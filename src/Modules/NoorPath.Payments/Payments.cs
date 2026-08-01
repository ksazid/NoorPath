namespace NoorPath.Payments;

public enum PaymentAttemptState
{
    Created,
    ProviderPending,
    RequiresAction,
    Succeeded,
    Failed,
    Cancelled
}

public enum ProviderEventOutcome
{
    Applied,
    Duplicate,
    IgnoredOutOfOrder,
    Rejected
}

public static class PaymentPolicy
{
    public static bool IsTerminal(PaymentAttemptState state) => state is
        PaymentAttemptState.Succeeded
        or PaymentAttemptState.Cancelled;

    public static bool CanTransition(
        PaymentAttemptState current,
        PaymentAttemptState next) => (current, next) switch
        {
            (PaymentAttemptState.Created, PaymentAttemptState.ProviderPending) => true,
            (PaymentAttemptState.Created, PaymentAttemptState.RequiresAction) => true,
            (PaymentAttemptState.Created, PaymentAttemptState.Failed) => true,
            (PaymentAttemptState.Created, PaymentAttemptState.Cancelled) => true,
            (PaymentAttemptState.ProviderPending, PaymentAttemptState.RequiresAction) => true,
            (PaymentAttemptState.ProviderPending, PaymentAttemptState.Succeeded) => true,
            (PaymentAttemptState.ProviderPending, PaymentAttemptState.Failed) => true,
            (PaymentAttemptState.ProviderPending, PaymentAttemptState.Cancelled) => true,
            (PaymentAttemptState.RequiresAction, PaymentAttemptState.ProviderPending) => true,
            (PaymentAttemptState.RequiresAction, PaymentAttemptState.Succeeded) => true,
            (PaymentAttemptState.RequiresAction, PaymentAttemptState.Failed) => true,
            (PaymentAttemptState.RequiresAction, PaymentAttemptState.Cancelled) => true,
            (PaymentAttemptState.Failed, PaymentAttemptState.ProviderPending) => true,
            (PaymentAttemptState.Failed, PaymentAttemptState.RequiresAction) => true,
            _ => current == next
        };

    public static ProviderEventOutcome EvaluateProviderEvent(
        PaymentAttemptState current,
        PaymentAttemptState requested)
    {
        if (current == requested)
            return ProviderEventOutcome.Duplicate;

        if (IsTerminal(current))
            return ProviderEventOutcome.IgnoredOutOfOrder;

        return CanTransition(current, requested)
            ? ProviderEventOutcome.Applied
            : ProviderEventOutcome.Rejected;
    }

    public static void ValidateAmount(string currency, decimal amount)
    {
        if (currency.Length != 3 || !currency.All(char.IsAsciiLetterUpper))
        {
            throw new ArgumentException(
                "Currency must be a three-letter uppercase code.",
                nameof(currency));
        }

        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount));
    }
}
