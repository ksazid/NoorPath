namespace NoorPath.Catalogue;

public enum FactConfirmationState
{
    Pending,
    Confirmed
}

public enum CatalogueDraftStatus
{
    Draft,
    ReadyForReview,
    Published
}

public enum PackageContentKind
{
    Inclusion,
    Exclusion
}

public sealed record AccommodationDraft(
    string HotelName,
    string Classification,
    string DistanceDisclosure,
    int Nights,
    FactConfirmationState ConfirmationState);

public sealed record TravelDraft(
    string RouteSummary,
    string Details,
    FactConfirmationState ConfirmationState);

public sealed record PackageDepartureDraftDetails(
    string PackageName,
    string Summary,
    AccommodationDraft Makkah,
    AccommodationDraft Madinah,
    TravelDraft Travel,
    string Origin,
    DateOnly DepartureDate,
    DateOnly ReturnDate,
    IReadOnlyList<string> Inclusions,
    IReadOnlyList<string> Exclusions);

public sealed class PackageDepartureDraft
{
    public PackageDepartureDraftDetails Details { get; }

    public PackageDepartureDraft(PackageDepartureDraftDetails details)
    {
        var errors = Validate(details);
        if (errors.Count != 0)
            throw new CatalogueDraftValidationException(errors);

        Details = details with
        {
            PackageName = details.PackageName.Trim(),
            Summary = details.Summary.Trim(),
            Makkah = Normalize(details.Makkah),
            Madinah = Normalize(details.Madinah),
            Travel = details.Travel with
            {
                RouteSummary = details.Travel.RouteSummary.Trim(),
                Details = details.Travel.Details.Trim()
            },
            Origin = details.Origin.Trim(),
            Inclusions = NormalizeItems(details.Inclusions),
            Exclusions = NormalizeItems(details.Exclusions)
        };
    }

    public static Dictionary<string, string[]> Validate(PackageDepartureDraftDetails value)
    {
        var errors = new Dictionary<string, string[]>();

        Required(errors, "packageName", value.PackageName, 120);
        Required(errors, "summary", value.Summary, 600);
        Required(errors, "origin", value.Origin, 120);
        Required(errors, "travel.routeSummary", value.Travel.RouteSummary, 200);
        OptionalMax(errors, "travel.details", value.Travel.Details, 600);

        ValidateAccommodation(errors, "makkah", value.Makkah);
        ValidateAccommodation(errors, "madinah", value.Madinah);

        if (value.ReturnDate <= value.DepartureDate)
            errors["returnDate"] = ["Return date must be after departure date."];

        if (value.Makkah.Nights + value.Madinah.Nights <= 0)
            errors["stays"] = ["At least one accommodation night is required."];

        ValidateItems(errors, "inclusions", value.Inclusions);
        ValidateItems(errors, "exclusions", value.Exclusions);

        return errors;
    }

    private static AccommodationDraft Normalize(AccommodationDraft value) => value with
    {
        HotelName = value.HotelName.Trim(),
        Classification = value.Classification.Trim(),
        DistanceDisclosure = value.DistanceDisclosure.Trim()
    };

    private static string[] NormalizeItems(IReadOnlyList<string> values) => values
        .Select(value => value.Trim())
        .Where(value => value.Length > 0)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    private static void ValidateAccommodation(
        IDictionary<string, string[]> errors,
        string prefix,
        AccommodationDraft value)
    {
        Required(errors, $"{prefix}.hotelName", value.HotelName, 160);
        OptionalMax(errors, $"{prefix}.classification", value.Classification, 80);
        OptionalMax(errors, $"{prefix}.distanceDisclosure", value.DistanceDisclosure, 120);

        if (value.Nights < 0)
            errors[$"{prefix}.nights"] = ["Nights cannot be negative."];
    }

    private static void ValidateItems(
        IDictionary<string, string[]> errors,
        string key,
        IReadOnlyList<string> values)
    {
        if (values.Count > 30 || values.Any(value => value.Trim().Length > 120))
            errors[key] = ["Use at most 30 items of 120 characters each."];
    }

    private static void Required(
        IDictionary<string, string[]> errors,
        string key,
        string value,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors[key] = ["This field is required."];
        else if (value.Trim().Length > maxLength)
            errors[key] = [$"Must be {maxLength} characters or fewer."];
    }

    private static void OptionalMax(
        IDictionary<string, string[]> errors,
        string key,
        string value,
        int maxLength)
    {
        if (value.Trim().Length > maxLength)
            errors[key] = [$"Must be {maxLength} characters or fewer."];
    }
}

public sealed class CatalogueDraftValidationException(Dictionary<string, string[]> errors)
    : Exception("Catalogue draft validation failed.")
{
    public Dictionary<string, string[]> Errors { get; } = errors;
}
