namespace NoorPath.Traveller;

public sealed record TravellerProfileDetails(
    string FullName,
    DateOnly DateOfBirth);

public sealed class TravellerProfile
{
    public TravellerProfileDetails Details { get; }

    public TravellerProfile(TravellerProfileDetails details)
    {
        var errors = Validate(details);
        if (errors.Count != 0)
            throw new TravellerValidationException(errors);

        Details = details with { FullName = NormalizeName(details.FullName) };
    }

    public static Dictionary<string, string[]> Validate(TravellerProfileDetails value)
    {
        var errors = new Dictionary<string, string[]>();
        var fullName = NormalizeName(value.FullName ?? string.Empty);

        if (fullName.Length < 2 || fullName.Length > 120)
            errors["fullName"] = ["Full name must be between 2 and 120 characters."];

        if (value.DateOfBirth >= DateOnly.FromDateTime(DateTime.UtcNow))
            errors["dateOfBirth"] = ["Date of birth must be in the past."];

        return errors;
    }

    private static string NormalizeName(string value) =>
        string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}

public sealed class TravellerValidationException(Dictionary<string, string[]> errors)
    : Exception("Traveller validation failed.")
{
    public Dictionary<string, string[]> Errors { get; } = errors;
}
