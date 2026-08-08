using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NoorPath.Booking.Infrastructure;
using Xunit;

namespace NoorPath.Commercial.Integration.Tests;

public sealed class OperatorDepartureManifestApiTests
{
    private static readonly Guid TravellerA = Guid.Parse("64000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task Foreign_departure_manifest_is_safe_not_found()
    {
        await using var app = await OperatorBookingAmendmentApi.CreateAsync(
            TestContext.Current.CancellationToken);
        using var client = app.CreateClientFor(OperatorBookingAmendmentApi.OperatorAccount);

        var response = await client.GetAsync(
            $"/api/v1/operator/departures/{Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff")}/manifest",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain("operator-b", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Operational_update_is_versioned_and_stale_write_adds_no_audit()
    {
        await using var app = await OperatorBookingAmendmentApi.CreateAsync(
            TestContext.Current.CancellationToken);
        using var client = app.CreateClientFor(OperatorBookingAmendmentApi.OperatorAccount);

        Guid departureId;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var bookings = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
            departureId = await bookings.Bookings.AsNoTracking()
                .Where(item => item.Id == OperatorBookingAmendmentApi.OwnedBookingId)
                .Select(item => item.DepartureId)
                .SingleAsync(TestContext.Current.CancellationToken);
        }

        var first = await client.PostAsJsonAsync(
            $"/api/v1/operator/departures/{departureId}/manifest/travellers/{TravellerA}/operations",
            new
            {
                note = "Confirm remaining ground-operation follow-up.",
                isAcknowledged = true,
                expectedVersion = 0
            },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var stale = await client.PostAsJsonAsync(
            $"/api/v1/operator/departures/{departureId}/manifest/travellers/{TravellerA}/operations",
            new
            {
                note = "Stale operator update.",
                isAcknowledged = false,
                expectedVersion = 0
            },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);

        await using var verifyScope = app.Services.CreateAsyncScope();
        var database = verifyScope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var operation = await database.Set<DepartureManifestTravellerRecord>().AsNoTracking()
            .SingleAsync(
                item => item.DepartureId == departureId && item.TravellerId == TravellerA,
                TestContext.Current.CancellationToken);
        var audit = await database.Set<DepartureManifestAuditRecord>().AsNoTracking()
            .SingleAsync(
                item => item.DepartureId == departureId && item.TravellerId == TravellerA,
                TestContext.Current.CancellationToken);

        Assert.Equal(1, operation.Version);
        Assert.True(operation.IsAcknowledged);
        Assert.Equal("Confirm remaining ground-operation follow-up.", operation.Note);
        Assert.Equal(0, audit.PreviousVersion);
        Assert.Equal(1, audit.ResultingVersion);
        Assert.False(audit.PreviousIsAcknowledged);
        Assert.True(audit.ResultingIsAcknowledged);
        Assert.Null(audit.PreviousNote);
        Assert.Equal(operation.Note, audit.ResultingNote);
    }

    [Fact]
    public async Task Operational_update_requires_explicit_note()
    {
        await using var app = await OperatorBookingAmendmentApi.CreateAsync(
            TestContext.Current.CancellationToken);
        using var client = app.CreateClientFor(OperatorBookingAmendmentApi.OperatorAccount);

        Guid departureId;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var bookings = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
            departureId = await bookings.Bookings.AsNoTracking()
                .Where(item => item.Id == OperatorBookingAmendmentApi.OwnedBookingId)
                .Select(item => item.DepartureId)
                .SingleAsync(TestContext.Current.CancellationToken);
        }

        var response = await client.PostAsJsonAsync(
            $"/api/v1/operator/departures/{departureId}/manifest/travellers/{TravellerA}/operations",
            new { note = "   ", isAcknowledged = true, expectedVersion = 0 },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var verifyScope = app.Services.CreateAsyncScope();
        var database = verifyScope.ServiceProvider.GetRequiredService<BookingDbContext>();
        Assert.Empty(await database.Set<DepartureManifestTravellerRecord>().ToArrayAsync(
            TestContext.Current.CancellationToken));
        Assert.Empty(await database.Set<DepartureManifestAuditRecord>().ToArrayAsync(
            TestContext.Current.CancellationToken));
    }
}
