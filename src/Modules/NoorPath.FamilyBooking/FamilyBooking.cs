namespace NoorPath.FamilyBooking;

public enum FamilyPartyStatus
{
    Draft,
    Validated,
    Archived,
}

public enum MahramRelationshipType
{
    Father,
    Son,
    Brother,
    Husband,
    PaternalUncle,
    MaternalUncle,
    Nephew,
    Grandfather,
    Grandson,
    OtherDeclaredRelationship,
}

public sealed record FamilyParty(
    Guid Id,
    string AccountId,
    string Name,
    FamilyPartyStatus Status,
    int Version,
    string PolicyVersion,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record FamilyPartyMember(
    Guid FamilyPartyId,
    string AccountId,
    Guid TravellerId,
    int Version,
    DateTimeOffset AddedAtUtc,
    DateTimeOffset? RemovedAtUtc = null);

public sealed record MahramLink(
    Guid Id,
    Guid FamilyPartyId,
    string AccountId,
    Guid ProtectedTravellerId,
    Guid MahramTravellerId,
    MahramRelationshipType RelationshipType,
    string Declaration,
    bool IsActive,
    int Version,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record MahramValidationIssue(
    string Code,
    Guid TravellerId,
    string CustomerMessage);

public sealed record MahramValidationResult(
    bool IsValid,
    string PolicyVersion,
    IReadOnlyList<MahramValidationIssue> Issues);

public static class FamilyBookingPolicy
{
    public const string CurrentVersion = "2026-08-v1";
    public const int MaximumPartySize = 20;
    public const int MaximumDeclarationLength = 500;
    public const int MaximumPartyNameLength = 100;

    public static string? ValidatePartyName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "A family party name is required.";
        }

        return name.Trim().Length > MaximumPartyNameLength
            ? $"The family party name cannot exceed {MaximumPartyNameLength} characters."
            : null;
    }

    public static string? ValidateMembership(
        IReadOnlyCollection<Guid> existingTravellerIds,
        Guid travellerId)
    {
        if (travellerId == Guid.Empty)
        {
            return "A valid traveller is required.";
        }

        if (existingTravellerIds.Contains(travellerId))
        {
            return "The traveller is already in this family party.";
        }

        return existingTravellerIds.Count >= MaximumPartySize
            ? $"A family party cannot contain more than {MaximumPartySize} travellers."
            : null;
    }

    public static string? ValidateMahramLink(
        Guid protectedTravellerId,
        Guid mahramTravellerId,
        string? declaration,
        IReadOnlyCollection<Guid> partyTravellerIds,
        IReadOnlyCollection<(Guid ProtectedTravellerId, Guid MahramTravellerId)> activeLinks)
    {
        if (protectedTravellerId == Guid.Empty || mahramTravellerId == Guid.Empty)
        {
            return "Both travellers are required for a Mahram link.";
        }

        if (protectedTravellerId == mahramTravellerId)
        {
            return "A traveller cannot be linked to themselves as Mahram.";
        }

        if (!partyTravellerIds.Contains(protectedTravellerId) || !partyTravellerIds.Contains(mahramTravellerId))
        {
            return "Both travellers must belong to the same family party.";
        }

        if (activeLinks.Contains((protectedTravellerId, mahramTravellerId)))
        {
            return "This active Mahram link already exists.";
        }

        if (string.IsNullOrWhiteSpace(declaration))
        {
            return "A customer declaration is required.";
        }

        return declaration.Trim().Length > MaximumDeclarationLength
            ? $"The declaration cannot exceed {MaximumDeclarationLength} characters."
            : null;
    }
}
