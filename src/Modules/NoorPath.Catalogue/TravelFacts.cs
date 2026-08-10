namespace NoorPath.Catalogue;

public sealed record FlightLegDraft(
    string AirlineName,
    string AirlineCode,
    string FlightNumber,
    string DepartureAirportName,
    string DepartureAirportCode,
    string ArrivalAirportName,
    string ArrivalAirportCode,
    FactConfirmationState ConfirmationState);

public sealed class PackageTravelFactsDraft
{
    public IReadOnlyList<FlightLegDraft> Legs { get; }

    public PackageTravelFactsDraft(IReadOnlyList<FlightLegDraft> legs)
    {
        var errors = Validate(legs);
        if (errors.Count != 0)
            throw new CatalogueDraftValidationException(errors);

        Legs = legs.Select(Normalize).ToArray();
    }

    public static Dictionary<string, string[]> Validate(IReadOnlyList<FlightLegDraft> legs)
    {
        var errors = new Dictionary<string, string[]>();

        if (legs.Count > 8)
            errors["legs"] = ["Use at most 8 flight legs for one package."];

        for (var index = 0; index < legs.Count; index++)
        {
            var leg = legs[index];
            var prefix = $"legs[{index}]";

            OptionalMax(errors, $"{prefix}.airlineName", leg.AirlineName, 120);
            OptionalMax(errors, $"{prefix}.airlineCode", leg.AirlineCode, 8);
            OptionalMax(errors, $"{prefix}.flightNumber", leg.FlightNumber, 16);
            OptionalMax(errors, $"{prefix}.departureAirportName", leg.DepartureAirportName, 160);
            OptionalMax(errors, $"{prefix}.departureAirportCode", leg.DepartureAirportCode, 8);
            OptionalMax(errors, $"{prefix}.arrivalAirportName", leg.ArrivalAirportName, 160);
            OptionalMax(errors, $"{prefix}.arrivalAirportCode", leg.ArrivalAirportCode, 8);

            if (leg.ConfirmationState != FactConfirmationState.Confirmed)
                continue;

            Required(errors, $"{prefix}.airlineName", leg.AirlineName, 120);
            Required(errors, $"{prefix}.flightNumber", leg.FlightNumber, 16);
            Required(errors, $"{prefix}.departureAirportName", leg.DepartureAirportName, 160);
            Required(errors, $"{prefix}.departureAirportCode", leg.DepartureAirportCode, 8);
            Required(errors, $"{prefix}.arrivalAirportName", leg.ArrivalAirportName, 160);
            Required(errors, $"{prefix}.arrivalAirportCode", leg.ArrivalAirportCode, 8);
        }

        return errors;
    }

    private static FlightLegDraft Normalize(FlightLegDraft value) => value with
    {
        AirlineName = value.AirlineName.Trim(),
        AirlineCode = value.AirlineCode.Trim().ToUpperInvariant(),
        FlightNumber = value.FlightNumber.Trim().ToUpperInvariant(),
        DepartureAirportName = value.DepartureAirportName.Trim(),
        DepartureAirportCode = value.DepartureAirportCode.Trim().ToUpperInvariant(),
        ArrivalAirportName = value.ArrivalAirportName.Trim(),
        ArrivalAirportCode = value.ArrivalAirportCode.Trim().ToUpperInvariant()
    };

    private static void Required(
        IDictionary<string, string[]> errors,
        string key,
        string value,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors[key] = ["This field is required when the flight leg is confirmed."];
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
