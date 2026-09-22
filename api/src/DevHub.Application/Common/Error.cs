namespace DevHub.Application.Common;

/// <summary>
/// An expected failure, described well enough for the API layer to turn it into an RFC 7807
/// ProblemDetails without guessing the status code.
/// </summary>
/// <param name="Code">Stable, machine-readable, e.g. "issue.not_found".</param>
/// <param name="Message">Human-readable, safe to show a user. Never a stack trace.</param>
/// <param name="Type">Decides the HTTP status code.</param>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    /// <summary>
    /// Field-level validation failures, keyed by property name as the validator reports it
    /// (<c>Password</c>). The API layer decides the wire casing; this layer knows nothing about
    /// JSON. Only set when <see cref="Type"/> is <see cref="ErrorType.Validation"/>.
    /// </summary>
    public IReadOnlyDictionary<string, string[]>? FieldErrors { get; init; }

    /// <summary>How long the client should wait. Only set for <see cref="ErrorType.TooManyRequests"/>.</summary>
    public TimeSpan? RetryAfter { get; init; }

    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    public static Error Validation(IReadOnlyDictionary<string, string[]> fieldErrors) =>
        new("validation", "One or more validation errors occurred.", ErrorType.Validation) { FieldErrors = fieldErrors };

    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);

    public static Error TooManyRequests(string code, string message, TimeSpan retryAfter) =>
        new(code, message, ErrorType.TooManyRequests) { RetryAfter = retryAfter };
}

public enum ErrorType
{
    /// <summary>404. Also used when a resource exists but the caller may not see it — never 403.</summary>
    NotFound,

    /// <summary>400.</summary>
    Validation,

    /// <summary>409.</summary>
    Conflict,

    /// <summary>401. The caller could not be authenticated (bad credentials, bad token).</summary>
    Unauthorized,

    /// <summary>429, with a Retry-After header. Throttling a specific account, e.g. login lockout.</summary>
    TooManyRequests,
}
