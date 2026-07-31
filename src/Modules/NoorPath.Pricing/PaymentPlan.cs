namespace NoorPath.Pricing;

public sealed record PaymentPlanDefinition(
    decimal DepositPercent,
    int InstalmentDayOfMonth,
    int FinalPaymentDueDaysBeforeDeparture);

public sealed record QuoteInstalment(
    int Sequence,
    DateOnly DueDate,
    decimal Amount);

public sealed record QuoteFinancials(
    decimal Total,
    decimal DueNow,
    decimal Remaining,
    IReadOnlyList<QuoteInstalment> Instalments);

public static class PaymentPlanPolicy
{
    public static Dictionary<string, string[]> Validate(PaymentPlanDefinition value)
    {
        var errors = new Dictionary<string, string[]>();

        if (value.DepositPercent <= 0 || value.DepositPercent > 100)
            errors["paymentPlan.depositPercent"] = ["Deposit percentage must be greater than 0 and at most 100."];
        else if (decimal.Round(value.DepositPercent, 2, MidpointRounding.ToEven) != value.DepositPercent)
            errors["paymentPlan.depositPercent"] = ["Deposit percentage can use at most two decimal places."];

        if (value.InstalmentDayOfMonth is < 1 or > 28)
            errors["paymentPlan.instalmentDayOfMonth"] = ["Instalment day must be between 1 and 28."];

        if (value.FinalPaymentDueDaysBeforeDeparture is < 0 or > 180)
            errors["paymentPlan.finalPaymentDueDaysBeforeDeparture"] = ["Final payment deadline must be between 0 and 180 days before departure."];

        return errors;
    }
}

public static class QuoteScheduleCalculator
{
    public static QuoteFinancials Calculate(
        decimal total,
        DateOnly departureDate,
        DateTimeOffset createdAtUtc,
        PaymentPlanDefinition? paymentPlan)
    {
        if (total <= 0)
            throw new ArgumentOutOfRangeException(nameof(total), "Quote total must be greater than zero.");

        total = RoundMoney(total);
        if (paymentPlan is null)
            return new(total, total, 0m, Array.Empty<QuoteInstalment>());

        var errors = PaymentPlanPolicy.Validate(paymentPlan);
        if (errors.Count != 0)
            throw new ArgumentException("Payment plan is invalid.", nameof(paymentPlan));

        var quoteDate = DateOnly.FromDateTime(createdAtUtc.UtcDateTime);
        var finalDueDate = departureDate.AddDays(-paymentPlan.FinalPaymentDueDaysBeforeDeparture);
        if (finalDueDate <= quoteDate || paymentPlan.DepositPercent == 100m)
            return new(total, total, 0m, Array.Empty<QuoteInstalment>());

        var dueNow = RoundMoney(total * paymentPlan.DepositPercent / 100m);
        var remaining = RoundMoney(total - dueNow);
        if (remaining <= 0m)
            return new(total, total, 0m, Array.Empty<QuoteInstalment>());

        var dueDates = BuildDueDates(
            quoteDate,
            finalDueDate,
            paymentPlan.InstalmentDayOfMonth);
        var count = dueDates.Count;
        var regularAmount = RoundMoney(remaining / count);
        var instalments = new List<QuoteInstalment>(count);
        var allocated = 0m;

        for (var index = 0; index < count; index++)
        {
            var amount = index == count - 1
                ? RoundMoney(remaining - allocated)
                : regularAmount;
            allocated = RoundMoney(allocated + amount);
            instalments.Add(new(index + 1, dueDates[index], amount));
        }

        return new(total, dueNow, remaining, instalments);
    }

    private static IReadOnlyList<DateOnly> BuildDueDates(
        DateOnly quoteDate,
        DateOnly finalDueDate,
        int dayOfMonth)
    {
        var dates = new List<DateOnly>();
        var year = quoteDate.Year;
        var month = quoteDate.Month;

        while (true)
        {
            var candidate = new DateOnly(year, month, dayOfMonth);
            if (candidate > quoteDate && candidate <= finalDueDate)
                dates.Add(candidate);

            if (candidate >= finalDueDate)
                break;

            month++;
            if (month == 13)
            {
                month = 1;
                year++;
            }
        }

        if (dates.Count == 0 || dates[^1] != finalDueDate)
            dates.Add(finalDueDate);

        return dates;
    }

    private static decimal RoundMoney(decimal value) =>
        decimal.Round(value, 2, MidpointRounding.ToEven);
}
