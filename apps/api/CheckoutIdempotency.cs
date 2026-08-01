using System.Security.Cryptography;
using System.Text;

internal static class CheckoutIdempotency
{
    public const string HeaderName = "Idempotency-Key";

    public static bool TryRead(
        HttpContext http,
        out string? key,
        out IResult? error)
    {
        key = http.Request.Headers[HeaderName].ToString().Trim();
        error = null;

        if (key.Length is < 8 or > 100
            || key.Any(character => character is < ' ' or > '~'))
        {
            key = null;
            error = Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Review the idempotency key",
                detail: "Provide an Idempotency-Key containing 8 to 100 ASCII characters.",
                extensions: ProblemExtensions(http, "invalid_idempotency_key"));
            return false;
        }

        return true;
    }

    public static string Hash(string value) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    public static Dictionary<string, object?> ProblemExtensions(
        HttpContext http,
        string code) => new()
        {
            ["code"] = code,
            ["correlationId"] = http.TraceIdentifier
        };
}
