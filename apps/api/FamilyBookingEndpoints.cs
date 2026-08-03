using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NoorPath.FamilyBooking;
using NoorPath.FamilyBooking.Infrastructure;
using NoorPath.Traveller.Infrastructure;

public static class FamilyBookingEndpoints
{
    public static void MapFamilyBooking(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/family-parties").RequireAuthorization();
        api.MapGet("", ListParties);
        api.MapGet("/{partyId:guid}", GetParty);
        api.MapPost("", CreateParty);
        api.MapPost("/{partyId:guid}/members", AddMember);
        api.MapPost("/{partyId:guid}/members/{travellerId:guid}/remove", RemoveMember);
        api.MapPost("/{partyId:guid}/mahram-links", AddMahramLink);
        api.MapPost("/{partyId:guid}/mahram-links/{linkId:guid}/remove", RemoveMahramLink);
        api.MapPost("/{partyId:guid}/validate", ValidateParty);
    }

    private static string? AccountId(HttpContext http) => http.User.GetCurrentPrincipal()?.AccountId.Value;

    private static async Task<IResult> ListParties(HttpContext http, FamilyBookingDbContext db, CancellationToken ct)
    {
        var accountId = AccountId(http);
        if (accountId is null) return Results.Unauthorized();

        var parties = await db.Parties.AsNoTracking()
            .Where(x => x.AccountId == accountId && x.Status != FamilyPartyStatus.Archived)
            .OrderByDescending(x => x.UpdatedAtUtc)
            .Select(x => new { x.Id, x.Name, status = x.Status.ToString(), x.PolicyVersion, x.Version, x.UpdatedAtUtc })
            .ToArrayAsync(ct);

        return Results.Ok(new { parties });
    }

    private static async Task<IResult> GetParty(Guid partyId, HttpContext http, FamilyBookingDbContext db, CancellationToken ct)
    {
        var accountId = AccountId(http);
        if (accountId is null) return Results.Unauthorized();

        var party = await db.Parties.AsNoTracking().SingleOrDefaultAsync(x => x.Id == partyId && x.AccountId == accountId, ct);
        if (party is null) return Results.NotFound();

        var members = await db.Members.AsNoTracking()
            .Where(x => x.FamilyPartyId == partyId && x.AccountId == accountId && x.RemovedAtUtc == null)
            .OrderBy(x => x.AddedAtUtc)
            .Select(x => new { x.TravellerId, x.Version, x.AddedAtUtc })
            .ToArrayAsync(ct);

        var links = await db.MahramLinks.AsNoTracking()
            .Where(x => x.FamilyPartyId == partyId && x.AccountId == accountId && x.IsActive)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new
            {
                x.Id,
                x.ProtectedTravellerId,
                x.MahramTravellerId,
                relationshipType = x.RelationshipType.ToString(),
                x.Declaration,
                x.Version,
                x.UpdatedAtUtc,
            })
            .ToArrayAsync(ct);

        return Results.Ok(new
        {
            party = new { party.Id, party.Name, status = party.Status.ToString(), party.PolicyVersion, party.Version, party.UpdatedAtUtc },
            members,
            mahramLinks = links,
        });
    }

    public sealed record CreatePartyRequest(string Name);

    private static async Task<IResult> CreateParty(CreatePartyRequest request, HttpContext http, FamilyBookingDbContext db, TimeProvider clock, CancellationToken ct)
    {
        var accountId = AccountId(http);
        if (accountId is null) return Results.Unauthorized();

        var error = FamilyBookingPolicy.ValidatePartyName(request.Name);
        if (error is not null) return Results.BadRequest(new { code = "invalid_party_name", message = error });

        var now = clock.GetUtcNow();
        var party = new FamilyPartyRecord
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Name = request.Name.Trim(),
            Status = FamilyPartyStatus.Draft,
            PolicyVersion = FamilyBookingPolicy.CurrentVersion,
            Version = 0,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        db.Parties.Add(party);
        AddAudit(db, accountId, accountId, "family_party_created", "family_party", party.Id, new { party.Name }, now);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/v1/family-parties/{party.Id}", new { party.Id, party.Name, status = party.Status.ToString(), party.PolicyVersion, party.Version });
    }

    public sealed record AddMemberRequest(Guid TravellerId, int Version);

    private static async Task<IResult> AddMember(Guid partyId, AddMemberRequest request, HttpContext http, FamilyBookingDbContext db, TravellerDbContext travellers, TimeProvider clock, CancellationToken ct)
    {
        var accountId = AccountId(http);
        if (accountId is null) return Results.Unauthorized();

        var party = await db.Parties.SingleOrDefaultAsync(x => x.Id == partyId && x.AccountId == accountId, ct);
        if (party is null) return Results.NotFound();
        if (party.Version != request.Version) return Stale(party.Version);
        if (party.Status == FamilyPartyStatus.Archived) return Results.Conflict(new { code = "party_archived" });

        var travellerExists = await travellers.Travellers.AsNoTracking()
            .AnyAsync(x => x.Id == request.TravellerId && x.OwnerAccountId == accountId, ct);
        if (!travellerExists) return Results.NotFound();

        var existing = await db.Members
            .Where(x => x.FamilyPartyId == partyId && x.RemovedAtUtc == null)
            .Select(x => x.TravellerId)
            .ToArrayAsync(ct);
        var error = FamilyBookingPolicy.ValidateMembership(existing, request.TravellerId);
        if (error is not null) return Results.Conflict(new { code = "invalid_party_membership", message = error });

        var now = clock.GetUtcNow();
        db.Members.Add(new FamilyPartyMemberRecord
        {
            FamilyPartyId = party.Id,
            AccountId = accountId,
            TravellerId = request.TravellerId,
            Version = 0,
            AddedAtUtc = now,
        });
        TouchDraft(party, now);
        AddAudit(db, accountId, accountId, "family_member_added", "traveller", request.TravellerId, new { partyId }, now);

        return await Save(db, party);
    }

    public sealed record VersionRequest(int Version);

    private static async Task<IResult> RemoveMember(Guid partyId, Guid travellerId, VersionRequest request, HttpContext http, FamilyBookingDbContext db, TimeProvider clock, CancellationToken ct)
    {
        var accountId = AccountId(http);
        if (accountId is null) return Results.Unauthorized();

        var party = await db.Parties.SingleOrDefaultAsync(x => x.Id == partyId && x.AccountId == accountId, ct);
        if (party is null) return Results.NotFound();
        if (party.Version != request.Version) return Stale(party.Version);

        var member = await db.Members.SingleOrDefaultAsync(x => x.FamilyPartyId == partyId && x.TravellerId == travellerId && x.AccountId == accountId && x.RemovedAtUtc == null, ct);
        if (member is null) return Results.NotFound();

        var now = clock.GetUtcNow();
        member.RemovedAtUtc = now;
        member.Version++;
        var links = await db.MahramLinks.Where(x => x.FamilyPartyId == partyId && x.AccountId == accountId && x.IsActive && (x.ProtectedTravellerId == travellerId || x.MahramTravellerId == travellerId)).ToArrayAsync(ct);
        foreach (var link in links) { link.IsActive = false; link.Version++; link.UpdatedAtUtc = now; }
        TouchDraft(party, now);
        AddAudit(db, accountId, accountId, "family_member_removed", "traveller", travellerId, new { partyId, removedLinks = links.Length }, now);

        return await Save(db, party);
    }

    public sealed record AddMahramLinkRequest(Guid ProtectedTravellerId, Guid MahramTravellerId, MahramRelationshipType RelationshipType, string Declaration, int Version);

    private static async Task<IResult> AddMahramLink(Guid partyId, AddMahramLinkRequest request, HttpContext http, FamilyBookingDbContext db, TimeProvider clock, CancellationToken ct)
    {
        var accountId = AccountId(http);
        if (accountId is null) return Results.Unauthorized();

        var party = await db.Parties.SingleOrDefaultAsync(x => x.Id == partyId && x.AccountId == accountId, ct);
        if (party is null) return Results.NotFound();
        if (party.Version != request.Version) return Stale(party.Version);
        if (party.Status == FamilyPartyStatus.Archived) return Results.Conflict(new { code = "party_archived" });

        var memberIds = await db.Members.Where(x => x.FamilyPartyId == partyId && x.AccountId == accountId && x.RemovedAtUtc == null).Select(x => x.TravellerId).ToArrayAsync(ct);
        var activeLinks = await db.MahramLinks.Where(x => x.FamilyPartyId == partyId && x.AccountId == accountId && x.IsActive).Select(x => new ValueTuple<Guid, Guid>(x.ProtectedTravellerId, x.MahramTravellerId)).ToArrayAsync(ct);
        var error = FamilyBookingPolicy.ValidateMahramLink(request.ProtectedTravellerId, request.MahramTravellerId, request.Declaration, memberIds, activeLinks);
        if (error is not null) return Results.Conflict(new { code = "invalid_mahram_link", message = error });

        var now = clock.GetUtcNow();
        var link = new MahramLinkRecord
        {
            Id = Guid.NewGuid(),
            FamilyPartyId = partyId,
            AccountId = accountId,
            ProtectedTravellerId = request.ProtectedTravellerId,
            MahramTravellerId = request.MahramTravellerId,
            RelationshipType = request.RelationshipType,
            Declaration = request.Declaration.Trim(),
            IsActive = true,
            Version = 0,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        db.MahramLinks.Add(link);
        TouchDraft(party, now);
        AddAudit(db, accountId, accountId, "mahram_link_created", "mahram_link", link.Id, new { partyId, link.ProtectedTravellerId, link.MahramTravellerId, relationshipType = link.RelationshipType.ToString() }, now);

        var saved = await Save(db, party);
        return saved is null ? Results.Ok(new { link.Id, party.Version }) : saved;
    }

    private static async Task<IResult> RemoveMahramLink(Guid partyId, Guid linkId, VersionRequest request, HttpContext http, FamilyBookingDbContext db, TimeProvider clock, CancellationToken ct)
    {
        var accountId = AccountId(http);
        if (accountId is null) return Results.Unauthorized();

        var party = await db.Parties.SingleOrDefaultAsync(x => x.Id == partyId && x.AccountId == accountId, ct);
        if (party is null) return Results.NotFound();
        if (party.Version != request.Version) return Stale(party.Version);

        var link = await db.MahramLinks.SingleOrDefaultAsync(x => x.Id == linkId && x.FamilyPartyId == partyId && x.AccountId == accountId && x.IsActive, ct);
        if (link is null) return Results.NotFound();

        var now = clock.GetUtcNow();
        link.IsActive = false;
        link.Version++;
        link.UpdatedAtUtc = now;
        TouchDraft(party, now);
        AddAudit(db, accountId, accountId, "mahram_link_removed", "mahram_link", link.Id, new { partyId }, now);

        return await Save(db, party);
    }

    private static async Task<IResult> ValidateParty(Guid partyId, VersionRequest request, HttpContext http, FamilyBookingDbContext db, TimeProvider clock, CancellationToken ct)
    {
        var accountId = AccountId(http);
        if (accountId is null) return Results.Unauthorized();

        var party = await db.Parties.SingleOrDefaultAsync(x => x.Id == partyId && x.AccountId == accountId, ct);
        if (party is null) return Results.NotFound();
        if (party.Version != request.Version) return Stale(party.Version);
        if (party.Status == FamilyPartyStatus.Archived) return Results.Conflict(new { code = "party_archived" });

        var members = await db.Members.Where(x => x.FamilyPartyId == partyId && x.AccountId == accountId && x.RemovedAtUtc == null).Select(x => x.TravellerId).ToArrayAsync(ct);
        var links = await db.MahramLinks.Where(x => x.FamilyPartyId == partyId && x.AccountId == accountId && x.IsActive).ToArrayAsync(ct);
        var issues = new List<MahramValidationIssue>();
        if (members.Length == 0) issues.Add(new("empty_party", Guid.Empty, "Add at least one traveller before validating the family party."));
        foreach (var link in links.Where(x => !members.Contains(x.ProtectedTravellerId) || !members.Contains(x.MahramTravellerId)))
            issues.Add(new("link_member_missing", link.ProtectedTravellerId, "A Mahram link references a traveller who is no longer in the family party."));

        if (issues.Count > 0)
            return Results.Conflict(new MahramValidationResult(false, FamilyBookingPolicy.CurrentVersion, issues));

        var now = clock.GetUtcNow();
        party.Status = FamilyPartyStatus.Validated;
        party.PolicyVersion = FamilyBookingPolicy.CurrentVersion;
        party.Version++;
        party.UpdatedAtUtc = now;
        AddAudit(db, accountId, accountId, "family_party_validated", "family_party", party.Id, new { memberCount = members.Length, linkCount = links.Length, party.PolicyVersion }, now);

        try
        {
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { structurallyValid = true, party.PolicyVersion, party.Version, party.UpdatedAtUtc, disclaimer = "NoorPath records the customer declaration and structural checks; it does not provide a religious or legal ruling." });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new { code = "stale_family_party" });
        }
    }

    private static void TouchDraft(FamilyPartyRecord party, DateTimeOffset now)
    {
        party.Status = FamilyPartyStatus.Draft;
        party.PolicyVersion = FamilyBookingPolicy.CurrentVersion;
        party.Version++;
        party.UpdatedAtUtc = now;
    }

    private static void AddAudit(FamilyBookingDbContext db, string accountId, string actorId, string action, string subjectType, Guid subjectId, object detail, DateTimeOffset now) =>
        db.Audit.Add(new FamilyBookingAuditRecord
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            ActorId = actorId,
            Action = action,
            SubjectType = subjectType,
            SubjectId = subjectId,
            DetailJson = JsonSerializer.Serialize(detail),
            OccurredAtUtc = now,
        });

    private static IResult Stale(int currentVersion) => Results.Conflict(new { code = "stale_family_party", currentVersion });

    private static async Task<IResult> Save(FamilyBookingDbContext db, FamilyPartyRecord party)
    {
        try
        {
            await db.SaveChangesAsync();
            return Results.Ok(new { party.Version, party.UpdatedAtUtc });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new { code = "stale_family_party" });
        }
        catch (DbUpdateException)
        {
            return Results.Conflict(new { code = "family_booking_conflict" });
        }
    }
}
