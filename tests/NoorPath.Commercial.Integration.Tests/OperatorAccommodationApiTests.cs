using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NoorPath.Booking;
using NoorPath.Booking.Infrastructure;
using Xunit;

namespace NoorPath.Commercial.Integration.Tests;

public sealed class OperatorAccommodationApiTests
{
    private static readonly Guid TravellerA = Guid.Parse("64000000-0000-0000-0000-000000000001");
    private static readonly Guid TravellerB = Guid.Parse("64000000-0000-0000-0000-000000000002");

    [Fact]
    public async Task Foreign_operator_accommodation_is_safe_not_found()
    {
        await using var app = await OperatorBookingAmendmentApi.CreateAsync(
            TestContext.Current.CancellationToken);
        using var client = app.CreateClientFor(OperatorBookingAmendmentApi.OperatorAccount);

        var response = await client.GetAsync(
            $"/api/v1/operator/bookings/{OperatorBookingAmendmentApi.ForeignBookingId}/accommodation",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain("operator-b", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("NP-VS25-FOREIGN", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Assignment_and_reassignment_preserve_booking_commercial_snapshot_and_append_audit()
    {
        await using var app = await OperatorBookingAmendmentApi.CreateAsync(
            TestContext.Current.CancellationToken);
        using var client = app.CreateClientFor(OperatorBookingAmendmentApi.OperatorAccount);

        BookingOccupancy occupancyBefore;
        decimal totalBefore;
        int bookingVersionBefore;
        await using (var beforeScope = app.Services.CreateAsyncScope())
        {
            var bookings = beforeScope.ServiceProvider.GetRequiredService<BookingDbContext>();
            var booking = await bookings.Bookings.AsNoTracking().SingleAsync(
                item => item.Id == OperatorBookingAmendmentApi.OwnedBookingId,
                TestContext.Current.CancellationToken);
            occupancyBefore = booking.Occupancy;
            totalBefore = booking.Total;
            bookingVersionBefore = booking.Version;
        }

        var first = await CreateRoomAsync(client, "makkah", "double", "Makkah 201");
        var second = await CreateRoomAsync(client, "makkah", "double", "Makkah 202");

        var assign = await client.PostAsJsonAsync(
            $"/api/v1/operator/bookings/{OperatorBookingAmendmentApi.OwnedBookingId}/accommodation/rooms/{first.RoomId}/assign",
            new
            {
                travellerId = TravellerA,
                reason = "Keep family allocation together.",
                expectedRoomVersion = first.Version,
                expectedPreviousRoomVersion = (int?)null
            },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, assign.StatusCode);

        var move = await client.PostAsJsonAsync(
            $"/api/v1/operator/bookings/{OperatorBookingAmendmentApi.OwnedBookingId}/accommodation/rooms/{second.RoomId}/assign",
            new
            {
                travellerId = TravellerA,
                reason = "Move beside the second family member.",
                expectedRoomVersion = second.Version,
                expectedPreviousRoomVersion = first.Version + 1
            },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, move.StatusCode);

        await using var scope = app.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var bookingAfter = await database.Bookings.AsNoTracking().SingleAsync(
            item => item.Id == OperatorBookingAmendmentApi.OwnedBookingId,
            TestContext.Current.CancellationToken);
        var assignments = await database.Set<AccommodationAssignmentRecord>().AsNoTracking()
            .Where(item => item.BookingId == bookingAfter.Id && item.Stay == AccommodationStay.Makkah)
            .ToArrayAsync(TestContext.Current.CancellationToken);
        var audits = await database.Set<AccommodationAssignmentAuditRecord>().AsNoTracking()
            .Where(item => item.BookingId == bookingAfter.Id)
            .OrderBy(item => item.OccurredAtUtc)
            .ToArrayAsync(TestContext.Current.CancellationToken);

        var assignment = Assert.Single(assignments);
        Assert.Equal(second.RoomId, assignment.RoomId);
        Assert.Equal(TravellerA, assignment.TravellerId);
        Assert.Equal(2, audits.Length);
        Assert.Equal("assigned", audits[0].Action);
        Assert.Equal("reassigned", audits[1].Action);
        Assert.Equal(occupancyBefore, bookingAfter.Occupancy);
        Assert.Equal(totalBefore, bookingAfter.Total);
        Assert.Equal(bookingVersionBefore, bookingAfter.Version);
    }

    [Fact]
    public async Task Stale_room_version_is_rejected_without_extra_assignment_or_audit()
    {
        await using var app = await OperatorBookingAmendmentApi.CreateAsync(
            TestContext.Current.CancellationToken);
        using var client = app.CreateClientFor(OperatorBookingAmendmentApi.OperatorAccount);
        var room = await CreateRoomAsync(client, "makkah", "double", "Makkah 301");

        var first = await client.PostAsJsonAsync(
            $"/api/v1/operator/bookings/{OperatorBookingAmendmentApi.OwnedBookingId}/accommodation/rooms/{room.RoomId}/assign",
            new
            {
                travellerId = TravellerA,
                reason = "Initial room assignment.",
                expectedRoomVersion = room.Version,
                expectedPreviousRoomVersion = (int?)null
            },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var stale = await client.PostAsJsonAsync(
            $"/api/v1/operator/bookings/{OperatorBookingAmendmentApi.OwnedBookingId}/accommodation/rooms/{room.RoomId}/assign",
            new
            {
                travellerId = TravellerB,
                reason = "Attempt from a stale screen.",
                expectedRoomVersion = room.Version,
                expectedPreviousRoomVersion = (int?)null
            },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);

        await using var scope = app.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        Assert.Equal(
            1,
            await database.Set<AccommodationAssignmentRecord>().CountAsync(
                item => item.BookingId == OperatorBookingAmendmentApi.OwnedBookingId,
                TestContext.Current.CancellationToken));
        Assert.Equal(
            1,
            await database.Set<AccommodationAssignmentAuditRecord>().CountAsync(
                item => item.BookingId == OperatorBookingAmendmentApi.OwnedBookingId,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Locked_room_rejects_assignment()
    {
        await using var app = await OperatorBookingAmendmentApi.CreateAsync(
            TestContext.Current.CancellationToken);
        using var client = app.CreateClientFor(OperatorBookingAmendmentApi.OperatorAccount);
        var room = await CreateRoomAsync(client, "madinah", "double", "Madinah 401");

        var locked = await client.PostAsJsonAsync(
            $"/api/v1/operator/bookings/{OperatorBookingAmendmentApi.OwnedBookingId}/accommodation/rooms/{room.RoomId}/lock",
            new
            {
                locked = true,
                reason = "Final operational rooming list approved.",
                expectedRoomVersion = room.Version
            },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, locked.StatusCode);

        var assign = await client.PostAsJsonAsync(
            $"/api/v1/operator/bookings/{OperatorBookingAmendmentApi.OwnedBookingId}/accommodation/rooms/{room.RoomId}/assign",
            new
            {
                travellerId = TravellerA,
                reason = "Late room change.",
                expectedRoomVersion = room.Version + 1,
                expectedPreviousRoomVersion = (int?)null
            },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, assign.StatusCode);
        Assert.Contains(
            "locked",
            await assign.Content.ReadAsStringAsync(TestContext.Current.CancellationToken),
            StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<(Guid RoomId, int Version)> CreateRoomAsync(
        HttpClient client,
        string stay,
        string roomType,
        string label)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/v1/operator/bookings/{OperatorBookingAmendmentApi.OwnedBookingId}/accommodation/rooms",
            new { stay, roomType, label },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        return (
            body.RootElement.GetProperty("roomId").GetGuid(),
            body.RootElement.GetProperty("version").GetInt32());
    }
}
