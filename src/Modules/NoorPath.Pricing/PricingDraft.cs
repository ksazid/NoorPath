namespace NoorPath.Pricing;

public enum PricingOccupancy
{
    Double,
    Triple,
    Quad
}

public sealed record OccupancyPriceDraft(
    PricingOccupancy Occupancy,
    decimal Amount);

public sealed record PricingDraftDetails(
    string Currency,
    IReadOnlyList<OccupancyPriceDraft> Occupancies,
    PaymentPlanDefinition? PaymentPlan = null);

public sealed class PricingDraft
{
    public PricingDraftDetails Details { get; }

    public PricingDraft(PricingDraftDetails details)
    {
        var errors = Validate(details);
        if (errors.Count != 0)
            throw new PricingDraftValidationException(errors);

        Details = details with
        {
            Currency = details.Currency.Trim().ToUpperInvariant(),
            Occupancies = details.Occupancies
                .OrderBy(x => x.Occupancy)
                .ToArray()
        };
    }

    public static Dictionary<string, string[]> Validate(PricingDraftDetails value)
    {
        var errors = new Dictionary<string, string[]>();
        var currency = value.Currency?.Trim() ?? string.Empty;

        if (currency.Length != 3 || !currency.All(char.IsAsciiLetter))
            errors["currency"] = ["Use a three-letter currency code such as INR."];

        if (value.Occupancies.Count is < 1 or > 3)
            errors["occupancies"] = ["Configure between one and three supported occupancies."];

        if (value.Occupancies.GroupBy(x => x.Occupancy).Any(group => group.Count() > 1))
            errors["occupancies"] = ["Each occupancy can be configured only once."];

        for (var index = 0; index < value.Occupancies.Count; index++)
        {
            var amount = value.Occupancies[index].Amount;
            if (amount <= 0)
                errors[$"occupancies[{index}].amount"] = ["Price must be greater than zero."];
            else if (decimal.Round(amount, 2, MidpointRounding.ToEven) != amount)
                errors[$"occupancies[{index}].amount"] = ["Price can use at most two decimal places."];
        }

        if (value.PaymentPlan is not null)
        {
            foreach (var error in PaymentPlanPolicy.Validate(value.PaymentPlan))
                errors[error.Key] = error.Value;
        }

        return errors;
    }
}

public sealed class PricingDraftValidationException(Dictionary<string, string[]> errors)
    : Exception("Pricing draft validation failed.")
{
    public Dictionary<string, string[]> Errors { get; } = errors;
}
