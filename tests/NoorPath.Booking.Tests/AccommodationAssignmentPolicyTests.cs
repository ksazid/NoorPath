using NoorPath.Booking;
using Xunit;

namespace NoorPath.Booking.Tests;

public sealed class AccommodationAssignmentPolicyTests
{
    private static readonly Guid TravellerA = Guid.Parse("71000000-0000-0000-0000-000000000001");
    private static readonly Guid TravellerB = Guid.Parse("71000000-0000-0000-0000-000000000002");
    private static readonly Guid TravellerC = Guid.Parse("71000000-0000-0000-0000-000000000003");
    private static readonly Guid RoomA = Guid.Parse("72000000-0000-0000-0000-000000000001");
    private static readonly Guid RoomB = Guid.Parse("72000000-0000-0000-0000-000000000002");

    [Fact]
    public void Double_room_rejects_third_occupant()
    {
        var room = new AccommodationRoom(
            RoomA,
            AccommodationStay.Makkah,
            AccommodationRoomType.Double,
            "Makkah 201",
            1,
            false);
        var assignments = new[]
        {
            new AccommodationAssignment(TravellerA, RoomA, AccommodationStay.Makkah),
            new AccommodationAssignment(TravellerB, RoomA, AccommodationStay.Makkah)
        };

        var error = Assert.Throws<InvalidOperationException>(() =>
            AccommodationAssignmentPolicy.ValidateMutation(
                BookingState.Confirmed,
                room,
                TravellerC,
                [TravellerA, TravellerB, TravellerC],
                assignments,
                "Place the final traveller.",
                1));

        Assert.Contains("capacity", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Traveller_can_move_between_rooms_in_same_stay_without_becoming_duplicate()
    {
        var destination = new AccommodationRoom(
            RoomB,
            AccommodationStay.Makkah,
            AccommodationRoomType.Double,
            "Makkah 202",
            3,
            false);
        var assignments = new[]
        {
            new AccommodationAssignment(TravellerA, RoomA, AccommodationStay.Makkah)
        };

        AccommodationAssignmentPolicy.ValidateMutation(
            BookingState.Confirmed,
            destination,
            TravellerA,
            [TravellerA],
            assignments,
            "Move traveller beside family member.",
            3);
    }

    [Fact]
    public void Locked_room_rejects_changes()
    {
        var room = new AccommodationRoom(
            RoomA,
            AccommodationStay.Madinah,
            AccommodationRoomType.Triple,
            "Madinah 301",
            2,
            true);

        var error = Assert.Throws<InvalidOperationException>(() =>
            AccommodationAssignmentPolicy.ValidateMutation(
                BookingState.Confirmed,
                room,
                TravellerA,
                [TravellerA],
                [],
                "Late reassignment.",
                2));

        Assert.Contains("locked", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Stale_room_version_is_rejected()
    {
        var room = new AccommodationRoom(
            RoomA,
            AccommodationStay.Makkah,
            AccommodationRoomType.Quad,
            "Makkah 401",
            4,
            false);

        var error = Assert.Throws<InvalidOperationException>(() =>
            AccommodationAssignmentPolicy.ValidateMutation(
                BookingState.Confirmed,
                room,
                TravellerA,
                [TravellerA],
                [],
                "Assign traveller.",
                3));

        Assert.Contains("refresh", error.Message, StringComparison.OrdinalIgnoreCase);
    }
}
