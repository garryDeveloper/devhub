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
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
}

public enum ErrorType
{
    /// <summary>404. Also used when a resource exists but the caller may not see it — never 403.</summary>
    NotFound,

    /// <summary>400.</summary>
    Validation,

    /// <summary>409.</summary>
    Conflict,
}
