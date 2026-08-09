using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NoorPath.Booking.Infrastructure;
using NoorPath.Documents.Infrastructure;
using NoorPath.Visa.Infrastructure;
using Xunit;

namespace NoorPath.Commercial.Integration.Tests;

public sealed class OperatorDepartureHandoverApiTests
{
    [Fact]
    public async Task Foreign_departure_handover_is_safe_not_found()
    {
        await using var app = await OperatorBookingAmendmentApi.CreateAsync(
            TestContext.Current.CancellationToken);
        await EnsureReadinessSchemasAsync(app, TestContext.Current.CancellationToken);
        using var client = app.CreateClientFor(OperatorBookingAmendmentApi.OperatorAccount);

        var response = await client.GetAsync(
            $"/api/v1/operator/departures/{Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff")}/handover",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain("operator-b", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Blocked_completion_adds_no_handover_or_audit_and_does_not_mutate_booking_snapshot()
    {
        await using var app = await OperatorBookingAmendmentApi.CreateAsync(
            TestContext.Current.CancellationToken);
        await EnsureReadinessSchemasAsync(app, TestContext.Current.CancellationToken);
        using var client = app.CreateClientFor(OperatorBookingAmendmentApi.OperatorAccount);

        Guid departureId;
        int bookingVersionBefore;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var bookings = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
            var booking = await bookings.Bookings.AsNoTracking()
                .SingleAsync(
                    item => item.Id == OperatorBookingAmendmentApi.OwnedBookingId,
                    TestContext.Current.CancellationToken);
            departureId = booking.DepartureId;
            bookingVersionBefore = booking.Version;
        }

        var response = await client.PostAsJsonAsync(
            $"/api/v1/operator/departures/{departureId}/handover/complete",
            new
            {
                finalNote = "Attempt final operational handover.",
                expectedVersion = 0
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains(
            "handover_blocked",
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken),
            StringComparison.Ordinal);

        await using var verifyScope = app.Services.CreateAsyncScope();
        var database = verifyScope.ServiceProvider.GetRequiredService<BookingDbContext>();
        Assert.Empty(await database.Set<DepartureHandoverRecord>().ToArrayAsync(
            TestContext.Current.CancellationToken));
        Assert.Empty(await database.Set<DepartureHandoverAuditRecord>().ToArrayAsync(
            TestContext.Current.CancellationToken));
        Assert.Equal(
            bookingVersionBefore,
            await database.Bookings.AsNoTracking()
                .Where(item => item.Id == OperatorBookingAmendmentApi.OwnedBookingId)
                .Select(item => item.Version)
                .SingleAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Completed_handover_replay_is_idempotent_and_does_not_append_second_audit()
    {
        await using var app = await OperatorBookingAmendmentApi.CreateAsync(
            TestContext.Current.CancellationToken);
        await EnsureReadinessSchemasAsync(app, TestContext.Current.CancellationToken);
        using var client = app.CreateClientFor(OperatorBookingAmendmentApi.OperatorAccount);

        Guid departureId;
        string operatorId;
        var completedAt = DateTimeOffset.UtcNow;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var bookings = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
            var booking = await bookings.Bookings.AsNoTracking()
                .SingleAsync(
                    item => item.Id == OperatorBookingAmendmentApi.OwnedBookingId,
                    TestContext.Current.CancellationToken);
            departureId = booking.DepartureId;
            operatorId = booking.OperatorId;

            bookings.Add(new DepartureHandoverRecord
            {
                Id = Guid.NewGuid(),
                DepartureId = departureId,
                OperatorId = operatorId,
                IsCompleted = true,
                FinalNote = "Departure handover already complete.",
                CompletedByAccountId = OperatorBookingAmendmentApi.OperatorAccount,
                CompletedAtUtc = completedAt,
                Version = 3,
                CreatedAtUtc = completedAt,
                UpdatedAtUtc = completedAt
            });
            bookings.Add(new DepartureHandoverAuditRecord
            {
                Id = Guid.NewGuid(),
                DepartureId = departureId,
                OperatorId = operatorId,
                ActorAccountId = OperatorBookingAmendmentApi.OperatorAccount,
                Action = "completed",
                Note = "Departure handover already complete.",
                PreviousVersion = 2,
                ResultingVersion = 3,
                TravellerCount = 2,
                BlockedCount = 0,
                CorrelationId = "seed-vs28",
                OccurredAtUtc = completedAt
            });
            await bookings.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var response = await client.PostAsJsonAsync(
            $"/api/v1/operator/departures/{departureId}/handover/complete",
            new
            {
                finalNote = "Replay should not change the closeout.",
                expectedVersion = 1
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(
            "\"idempotent\":true",
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken),
            StringComparison.OrdinalIgnoreCase);

        await using var verifyScope = app.Services.CreateAsyncScope();
        var database = verifyScope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var handover = await database.Set<DepartureHandoverRecord>().AsNoTracking()
            .SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(3, handover.Version);
        Assert.Equal("Departure handover already complete.", handover.FinalNote);
        Assert.Equal(
            1,
            await database.Set<DepartureHandoverAuditRecord>().CountAsync(
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Group_leader_assignment_is_versioned_audited_and_does_not_change_travellers()
    {
        await using var app = await OperatorBookingAmendmentApi.CreateAsync(
            TestContext.Current.CancellationToken);
        await EnsureReadinessSchemasAsync(app, TestContext.Current.CancellationToken);
        using var client = app.CreateClientFor(OperatorBookingAmendmentApi.OperatorAccount);

        Guid departureId;
        int travellerCountBefore;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var bookings = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
            var booking = await bookings.Bookings.AsNoTracking()
                .SingleAsync(item => item.Id == OperatorBookingAmendmentApi.OwnedBookingId, TestContext.Current.CancellationToken);
            departureId = booking.DepartureId;
            travellerCountBefore = await bookings.Travellers.CountAsync(TestContext.Current.CancellationToken);
        }

        var response = await client.PostAsJsonAsync(
            $"/api/v1/operator/departures/{departureId}/manifest/group-leader",
            new { name = "Amina Rahman", expectedVersion = 0 },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var verifyScope = app.Services.CreateAsyncScope();
        var database = verifyScope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var handover = await database.Set<DepartureHandoverRecord>().AsNoTracking()
            .SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("Amina Rahman", handover.GroupLeaderName);
        Assert.Equal(1, handover.Version);
        Assert.False(handover.IsCompleted);
        Assert.Equal(travellerCountBefore, await database.Travellers.CountAsync(TestContext.Current.CancellationToken));
        var audit = await database.Set<DepartureHandoverAuditRecord>().AsNoTracking()
            .SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("group_leader_updated", audit.Action);
        Assert.Equal(0, audit.PreviousVersion);
        Assert.Equal(1, audit.ResultingVersion);
    }

    [Fact]
    public async Task Group_leader_write_is_safe_not_found_for_foreign_departure()
    {
        await using var app = await OperatorBookingAmendmentApi.CreateAsync(
            TestContext.Current.CancellationToken);
        await EnsureReadinessSchemasAsync(app, TestContext.Current.CancellationToken);
        using var client = app.CreateClientFor(OperatorBookingAmendmentApi.OperatorAccount);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/operator/departures/{Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff")}/manifest/group-leader",
            new { name = "Foreign Leader", expectedVersion = 0 },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await using var verifyScope = app.Services.CreateAsyncScope();
        var database = verifyScope.ServiceProvider.GetRequiredService<BookingDbContext>();
        Assert.Empty(await database.Set<DepartureHandoverRecord>().ToArrayAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Completed_handover_rejects_group_leader_change()
    {
        await using var app = await OperatorBookingAmendmentApi.CreateAsync(
            TestContext.Current.CancellationToken);
        await EnsureReadinessSchemasAsync(app, TestContext.Current.CancellationToken);
        using var client = app.CreateClientFor(OperatorBookingAmendmentApi.OperatorAccount);

        Guid departureId;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var bookings = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
            var booking = await bookings.Bookings.AsNoTracking()
                .SingleAsync(item => item.Id == OperatorBookingAmendmentApi.OwnedBookingId, TestContext.Current.CancellationToken);
            departureId = booking.DepartureId;
            var now = DateTimeOffset.UtcNow;
            bookings.Add(new DepartureHandoverRecord
            {
                Id = Guid.NewGuid(),
                DepartureId = departureId,
                OperatorId = booking.OperatorId,
                IsCompleted = true,
                GroupLeaderName = "Original Leader",
                FinalNote = "Closeout complete.",
                CompletedByAccountId = OperatorBookingAmendmentApi.OperatorAccount,
                CompletedAtUtc = now,
                Version = 2,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
            await bookings.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var response = await client.PostAsJsonAsync(
            $"/api/v1/operator/departures/{departureId}/manifest/group-leader",
            new { name = "Changed Leader", expectedVersion = 2 },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("handover_completed", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);

        await using var verifyScope = app.Services.CreateAsyncScope();
        var database = verifyScope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var record = await database.Set<DepartureHandoverRecord>().AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("Original Leader", record.GroupLeaderName);
        Assert.Equal(2, record.Version);
    }

    private static async Task EnsureReadinessSchemasAsync(
        OperatorBookingAmendmentApi app,
        CancellationToken cancellationToken)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var documents = scope.ServiceProvider.GetRequiredService<DocumentsDbContext>();
        var visa = scope.ServiceProvider.GetRequiredService<VisaDbContext>();
        await documents.Database.MigrateAsync(cancellationToken);
        await visa.Database.MigrateAsync(cancellationToken);
    }
}
