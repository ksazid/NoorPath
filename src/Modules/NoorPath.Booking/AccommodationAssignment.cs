namespace NoorPath.Booking;

public enum AccommodationStay
{
    Makkah,
    Madinah
}

public enum AccommodationRoomType
{
    Double,
    Triple,
    Quad
}

public sealed record AccommodationRoom(
    Guid RoomId,
    AccommodationStay Stay,
    AccommodationRoomType RoomType,
    string Label,
    int Version,
    bool IsLocked);

public sealed record AccommodationAssignment(
    Guid TravellerId,
    Guid RoomId,
    AccommodationStay Stay);

public static class AccommodationAssignmentPolicy
{
    public const int MaximumReasonLength = 500;

    public static int Capacity(AccommodationRoomType roomType) => roomType switch
    {
        AccommodationRoomType.Double => 2,
        AccommodationRoomType.Triple => 3,
        AccommodationRoomType.Quad => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(roomType))
    };

    public static void ValidateMutation(
        BookingState bookingState,
        AccommodationRoom room,
        Guid travellerId,
        IReadOnlyCollection<Guid> bookingTravellerIds,
        IReadOnlyCollection<AccommodationAssignment> assignments,
        string reason,
        int expectedRoomVersion)
    {
        if (bookingState != BookingState.Confirmed)
            throw new InvalidOperationException("Accommodation assignments require a confirmed booking.");

        if (room.IsLocked)
            throw new InvalidOperationException("Accommodation assignments are locked for this stay.");

        if (expectedRoomVersion != room.Version)
            throw new InvalidOperationException("The room assignment changed. Refresh before trying again.");

        if (travellerId == Guid.Empty || !bookingTravellerIds.Contains(travellerId))
            throw new ArgumentException("Traveller must belong to the booking.", nameof(travellerId));

        var trimmedReason = reason?.Trim() ?? string.Empty;
        if (trimmedReason.Length is 0 or > MaximumReasonLength)
        {
            throw new ArgumentException(
                $"Assignment reason must be between 1 and {MaximumReasonLength} characters.",
                nameof(reason));
        }

        var existingForStay = assignments
            .SingleOrDefault(item => item.Stay == room.Stay && item.TravellerId == travellerId);
        if (existingForStay is not null && existingForStay.RoomId == room.RoomId)
            throw new InvalidOperationException("Traveller is already assigned to this room.");

        var occupants = assignments.Count(item => item.RoomId == room.RoomId);
        var movingWithinStay = existingForStay is not null;
        if (!movingWithinStay && occupants >= Capacity(room.RoomType))
            throw new InvalidOperationException("Room capacity has been reached.");
    }

    public static void ValidateUnassign(
        BookingState bookingState,
        AccommodationRoom room,
        Guid travellerId,
        IReadOnlyCollection<AccommodationAssignment> assignments,
        string reason,
        int expectedRoomVersion)
    {
        if (bookingState != BookingState.Confirmed)
            throw new InvalidOperationException("Accommodation assignments require a confirmed booking.");

        if (room.IsLocked)
            throw new InvalidOperationException("Accommodation assignments are locked for this stay.");

        if (expectedRoomVersion != room.Version)
            throw new InvalidOperationException("The room assignment changed. Refresh before trying again.");

        var trimmedReason = reason?.Trim() ?? string.Empty;
        if (trimmedReason.Length is 0 or > MaximumReasonLength)
        {
            throw new ArgumentException(
                $"Assignment reason must be between 1 and {MaximumReasonLength} characters.",
                nameof(reason));
        }

        if (!assignments.Any(item => item.RoomId == room.RoomId && item.TravellerId == travellerId))
            throw new InvalidOperationException("Traveller is not assigned to this room.");
    }
}
