using Microsoft.EntityFrameworkCore;
using NoorPath.Booking;
using NoorPath.Booking.Infrastructure;
using NoorPath.Documents;
using NoorPath.Documents.Infrastructure;
using NoorPath.Operators;
using NoorPath.Visa;
using NoorPath.Visa.Infrastructure;

public static class VisaEndpoints
{
    public static void MapVisa(this WebApplication app)
    {
        app.MapGet("/api/v1/bookings/{bookingId:guid}/visa", CustomerCases).RequireAuthorization();
        var operatorApi = app.MapGroup("/api/v1/operator/visa").RequireAuthorization();
        operatorApi.MapGet("", Queue);
        operatorApi.MapGet("/{caseId:guid}", Case);
        operatorApi.MapPost("/{caseId:guid}/transitions", Transition);
    }

    private static async Task<IResult> CustomerCases(Guid bookingId, HttpContext http, BookingDbContext bookings, VisaDbContext visa, TimeProvider clock, CancellationToken ct)
    {
        var principal = http.User.GetCurrentPrincipal(); if (principal is null) return Results.Unauthorized();
        var booking = await bookings.Bookings.SingleOrDefaultAsync(x => x.Id == bookingId && x.AccountId == principal.AccountId.Value && x.State == BookingState.Confirmed, ct); if (booking is null) return Results.NotFound();
        var travellers = await bookings.Travellers.Where(x => x.BookingId == bookingId).OrderBy(x => x.Position).ToArrayAsync(ct);
        await EnsureCases(booking, travellers, visa, clock, ct);
        var cases = await visa.Cases.AsNoTracking().Where(x => x.BookingId == bookingId).ToArrayAsync(ct);
        return Results.Ok(new { travellers = travellers.Select(t => { var item = cases.Single(x => x.TravellerId == t.TravellerId); return new { t.TravellerId, t.FullName, status = VisaPolicy.CustomerLabel(item.Status), code = item.Status.ToString(), item.UpdatedAtUtc, requiredAction = item.Status == VisaStatus.ActionRequired ? item.CustomerAction : null }; }) });
    }

    private static async Task<OperatorAccess?> Access(HttpContext http, IOperatorAccess access, CancellationToken ct) { var p = http.User.GetCurrentPrincipal(); return p is null ? null : await access.FindActiveMembershipAsync(p.AccountId, ct); }
    private static async Task<IResult> Queue(HttpContext http, IOperatorAccess access, BookingDbContext bookings, VisaDbContext visa, TimeProvider clock, CancellationToken ct)
    {
        var op = await Access(http, access, ct); if (op is null || !op.IsAllowed(OperatorPermissions.VisaProcessing)) return Results.Forbid();
        var confirmed = await bookings.Bookings.Where(x => x.OperatorId == op.OperatorId && x.State == BookingState.Confirmed).ToArrayAsync(ct);
        foreach (var booking in confirmed) await EnsureCases(booking, await bookings.Travellers.Where(x => x.BookingId == booking.Id).ToArrayAsync(ct), visa, clock, ct);
        var bookingIds = confirmed.Select(x => x.Id).ToArray();
        var items = await visa.Cases.AsNoTracking().Where(x => bookingIds.Contains(x.BookingId) && x.Status != VisaStatus.Approved && x.Status != VisaStatus.Rejected).OrderBy(x => x.UpdatedAtUtc).Select(x => new { caseId = x.Id, x.BookingId, x.TravellerId, status = x.Status.ToString(), x.UpdatedAtUtc, x.Version }).ToArrayAsync(ct);
        return Results.Ok(new { items });
    }

    private static async Task<(VisaCaseRecord?, OperatorAccess?)> Scoped(Guid id, HttpContext http, IOperatorAccess access, VisaDbContext visa, CancellationToken ct)
    { var op = await Access(http, access, ct); if (op is null || !op.IsAllowed(OperatorPermissions.VisaProcessing)) return (null, op); return (await visa.Cases.SingleOrDefaultAsync(x => x.Id == id && x.OperatorId == op.OperatorId, ct), op); }

    private static async Task<IResult> Case(Guid caseId, HttpContext http, IOperatorAccess access, VisaDbContext visa, CancellationToken ct)
    {
        var (item, op) = await Scoped(caseId, http, access, visa, ct); if (op is null || !op.IsAllowed(OperatorPermissions.VisaProcessing)) return Results.Forbid(); if (item is null) return Results.NotFound();
        var history = await visa.History.AsNoTracking().Where(x => x.CaseId == caseId).OrderByDescending(x => x.OccurredAtUtc).Select(x => new { previousStatus = x.PreviousStatus.ToString(), newStatus = x.NewStatus.ToString(), x.Reason, x.Version, x.OccurredAtUtc }).ToArrayAsync(ct);
        return Results.Ok(new { caseId = item.Id, item.BookingId, item.TravellerId, status = item.Status.ToString(), item.CustomerAction, item.Version, item.UpdatedAtUtc, allowedTransitions = VisaPolicy.AllowedNext(item.Status).Select(x => x.ToString()), history });
    }

    public sealed record TransitionRequest(VisaStatus Status, string? Reason, int Version);
    private static async Task<IResult> Transition(Guid caseId, TransitionRequest request, HttpContext http, IOperatorAccess access, VisaDbContext visa, DocumentsDbContext documents, TimeProvider clock, ILogger<Program> log, CancellationToken ct)
    {
        var (item, op) = await Scoped(caseId, http, access, visa, ct); if (op is null || !op.IsAllowed(OperatorPermissions.VisaProcessing)) return Results.Forbid(); if (item is null) return Results.NotFound();
        if (item.Version != request.Version) return Results.Conflict(new { code = "stale_visa_case", currentVersion = item.Version });
        try { VisaPolicy.Validate(item.Status, request.Status, request.Reason); } catch (ArgumentException e) { return Results.BadRequest(new { code = "reason_required", message = e.Message }); } catch (InvalidOperationException) { return Results.Conflict(new { code = "invalid_transition" }); }
        if (request.Status == VisaStatus.ReadyToSubmit)
        {
            var requirements = await documents.Requirements.Where(x => x.BookingId == item.BookingId && x.TravellerId == item.TravellerId).Select(x => x.Id).ToArrayAsync(ct);
            var ready = requirements.Length > 0 && await documents.Submissions.Where(x => requirements.Contains(x.RequirementId)).GroupBy(x => x.RequirementId).AllAsync(g => g.OrderByDescending(x => x.CreatedAtUtc).First().State == SubmissionState.Approved, ct);
            if (!ready) return Results.Conflict(new { code = "documents_not_ready" });
        }
        var previous = item.Status; item.Status = request.Status; item.CustomerAction = request.Status == VisaStatus.ActionRequired ? request.Reason!.Trim() : null; item.Version++; item.UpdatedAtUtc = clock.GetUtcNow();
        visa.History.Add(new() { Id = Guid.NewGuid(), CaseId = item.Id, PreviousStatus = previous, NewStatus = item.Status, ActorId = http.User.GetCurrentPrincipal()!.AccountId.Value, Reason = request.Reason?.Trim(), Version = item.Version, OccurredAtUtc = item.UpdatedAtUtc });
        try { await visa.SaveChangesAsync(ct); } catch (DbUpdateConcurrencyException) { return Results.Conflict(new { code = "stale_visa_case" }); }
        log.LogInformation("Visa transition outcome=success caseId={CaseId} bookingId={BookingId} from={From} to={To} correlationId={CorrelationId}", item.Id, item.BookingId, previous, item.Status, http.TraceIdentifier);
        return Results.Ok(new { status = item.Status.ToString(), item.Version, item.UpdatedAtUtc });
    }

    private static async Task EnsureCases(BookingRecord booking, IEnumerable<BookingTravellerRecord> travellers, VisaDbContext visa, TimeProvider clock, CancellationToken ct)
    {
        var existing = await visa.Cases.Where(x => x.BookingId == booking.Id).Select(x => x.TravellerId).ToArrayAsync(ct); var now = clock.GetUtcNow();
        foreach (var traveller in travellers.Where(x => !existing.Contains(x.TravellerId))) visa.Cases.Add(new() { Id = Guid.NewGuid(), BookingId = booking.Id, TravellerId = traveller.TravellerId, OperatorId = booking.OperatorId, Status = VisaStatus.NotStarted, Version = 0, CreatedAtUtc = now, UpdatedAtUtc = now });
        if (visa.ChangeTracker.HasChanges()) try { await visa.SaveChangesAsync(ct); } catch (DbUpdateException) { visa.ChangeTracker.Clear(); }
    }
}
