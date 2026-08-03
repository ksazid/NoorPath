using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NoorPath.FamilyBooking;
using NoorPath.FamilyBooking.Infrastructure;
using NoorPath.Pricing.Infrastructure;

public static class FamilyQuoteEndpoints
{
    public static void MapFamilyQuoteBinding(this WebApplication app)
    {
        app.MapPost(
                "/api/v1/family-parties/{partyId:guid}/quotes/{quoteId:guid}/snapshot",
                BindQuote)
            .RequireAuthorization();
    }

    public sealed record BindFamilyQuoteRequest(int Version);

    private static async Task<IResult> BindQuote(
        Guid partyId,
        Guid quoteId,
        BindFamilyQuoteRequest request,
        HttpContext http,
        FamilyBookingDbContext family,
        PricingDbContext pricing,
        TimeProvider clock,
        ILogger<Program> log,
        CancellationToken cancellationToken)
    {
        var principal = http.User.GetCurrentPrincipal();
        if (principal is null)
            return Results.Unauthorized();

        var accountId = principal.AccountId.Value;
        var quote = await pricing.Quotes.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == quoteId && item.AccountId == accountId,
            cancellationToken);
        if (quote is null)
            return Results.NotFound();

        var party = await family.Parties.SingleOrDefaultAsync(
            item => item.Id == partyId && item.AccountId == accountId,
            cancellationToken);
        if (party is null)
            return Results.NotFound();
        if (party.Version != request.Version)
            return Results.Conflict(new { code = "stale_family_party", currentVersion = party.Version });
        if (party.Status != FamilyPartyStatus.Validated ||
            !string.Equals(party.PolicyVersion, FamilyBookingPolicy.CurrentVersion, StringComparison.Ordinal))
        {
            return Results.Conflict(new
            {
                code = "family_party_validation_required",
                message = "Validate the latest family-party composition before using it for a quote."
            });
        }

        var quoteTravellerIds = await pricing.QuoteTravellers.AsNoTracking()
            .Where(item => item.QuoteId == quoteId)
            .OrderBy(item => item.Position)
            .Select(item => item.TravellerId)
            .ToArrayAsync(cancellationToken);
        var partyTravellerIds = await family.Members.AsNoTracking()
            .Where(item =>
                item.FamilyPartyId == partyId &&
                item.AccountId == accountId &&
                item.RemovedAtUtc == null)
            .Select(item => item.TravellerId)
            .ToArrayAsync(cancellationToken);

        if (quoteTravellerIds.Length == 0 ||
            quoteTravellerIds.Length != partyTravellerIds.Length ||
            quoteTravellerIds.Except(partyTravellerIds).Any())
        {
            return Results.Conflict(new
            {
                code = "family_party_quote_mismatch",
                message = "The quote travellers must exactly match the validated family party."
            });
        }

        var links = await family.MahramLinks.AsNoTracking()
            .Where(item =>
                item.FamilyPartyId == partyId &&
                item.AccountId == accountId &&
                item.IsActive)
            .OrderBy(item => item.CreatedAtUtc)
            .Select(item => new
            {
                item.ProtectedTravellerId,
                item.MahramTravellerId,
                relationshipType = item.RelationshipType.ToString()
            })
            .ToArrayAsync(cancellationToken);
        if (links.Any(link =>
                !partyTravellerIds.Contains(link.ProtectedTravellerId) ||
                !partyTravellerIds.Contains(link.MahramTravellerId)))
        {
            return Results.Conflict(new { code = "family_party_validation_required" });
        }

        var existing = await family.QuoteSnapshots.SingleOrDefaultAsync(
            item => item.QuoteId == quoteId,
            cancellationToken);
        if (existing is not null)
        {
            return existing.AccountId == accountId &&
                   existing.FamilyPartyId == partyId &&
                   existing.PartyVersion == party.Version
                ? Results.Ok(new
                {
                    quoteId,
                    familyPartyId = partyId,
                    existing.PolicyVersion,
                    existing.PartyVersion,
                    snapshotCreatedAtUtc = existing.CreatedAtUtc
                })
                : Results.Conflict(new { code = "quote_family_snapshot_conflict" });
        }

        var now = clock.GetUtcNow();
        var payload = JsonSerializer.Serialize(new
        {
            familyPartyId = party.Id,
            partyName = party.Name,
            policyVersion = party.PolicyVersion,
            partyVersion = party.Version,
            travellerIds = quoteTravellerIds,
            mahramLinks = links
        });
        family.QuoteSnapshots.Add(new FamilyQuoteSnapshotRecord
        {
            Id = Guid.NewGuid(),
            QuoteId = quoteId,
            FamilyPartyId = party.Id,
            AccountId = accountId,
            PolicyVersion = party.PolicyVersion,
            PartyVersion = party.Version,
            PayloadJson = payload,
            CreatedAtUtc = now
        });
        family.Audit.Add(new FamilyBookingAuditRecord
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            ActorId = accountId,
            Action = "family_quote_snapshotted",
            SubjectType = "quote",
            SubjectId = quoteId,
            DetailJson = JsonSerializer.Serialize(new
            {
                familyPartyId = party.Id,
                partyVersion = party.Version,
                policyVersion = party.PolicyVersion,
                travellerCount = quoteTravellerIds.Length,
                mahramLinkCount = links.Length
            }),
            OccurredAtUtc = now
        });

        try
        {
            await family.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Results.Conflict(new { code = "quote_family_snapshot_conflict" });
        }

        log.LogInformation(
            "Family quote snapshot outcome=created quoteId={QuoteId} partyId={PartyId} partyVersion={PartyVersion} travellerCount={TravellerCount} correlationId={CorrelationId}",
            quoteId,
            party.Id,
            party.Version,
            quoteTravellerIds.Length,
            http.TraceIdentifier);

        return Results.Ok(new
        {
            quoteId,
            familyPartyId = party.Id,
            party.PolicyVersion,
            partyVersion = party.Version,
            snapshotCreatedAtUtc = now
        });
    }
}
