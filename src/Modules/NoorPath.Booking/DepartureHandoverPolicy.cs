namespace NoorPath.Booking;

public static class DepartureHandoverPolicy
{
    public const int MaxNoteLength = 500;

    public static bool CanComplete(int travellerCount, int blockedCount) =>
        travellerCount > 0 && blockedCount == 0;

    public static string? ValidateCompletion(
        int travellerCount,
        int blockedCount,
        string? note,
        int expectedVersion,
        int currentVersion,
        bool isCompleted)
    {
        if (isCompleted)
            return null;
        if (travellerCount <= 0)
            return "handover_empty";
        if (blockedCount > 0)
            return "handover_blocked";
        if (expectedVersion != currentVersion)
            return "handover_stale";
        if (string.IsNullOrWhiteSpace(note) || note.Trim().Length > MaxNoteLength)
            return "handover_note_invalid";
        return null;
    }
}
