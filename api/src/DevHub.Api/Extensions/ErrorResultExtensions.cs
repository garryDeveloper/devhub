using System.Globalization;
using System.Text.Json;
using DevHub.Application.Common;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace DevHub.Api.Extensions;

/// <summary>
/// Turns an Application <see cref="Error"/> into the RFC 7807 response of api-conventions.md §4.
/// The one place an <see cref="ErrorType"/> becomes a status code, so every endpoint agrees.
/// </summary>
public static class ErrorResultExtensions
{
    /// <summary>Every DevHub problem <c>type</c> is this plus the error code.</summary>
    public const string ProblemTypeBase = "https://devhub.dev/errors/";

    public const string ValidationProblemType = ProblemTypeBase + "validation";

    /// <param name="httpContext">
    /// Needed only for <c>Retry-After</c>: a <see cref="ProblemHttpResult"/> writes a body, not
    /// headers, so the header goes on the response before the result executes.
    /// </param>
    public static ProblemHttpResult ToProblem(this Error error, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(httpContext);

        if (error is { Type: ErrorType.Validation, FieldErrors: { } fieldErrors })
        {
            // The validator reports C# property names ("Password"); the client sent JSON ones
            // ("password"). errors.password is what the client can map back to a field.
            var errors = fieldErrors.ToDictionary(
                entry => JsonNamingPolicy.CamelCase.ConvertName(entry.Key),
                entry => entry.Value);

            // HttpValidationProblemDetails is a ProblemDetails with an "errors" member; the
            // ProblemDetails writer serializes the runtime type, so "errors" reaches the wire.
            return TypedResults.Problem(new HttpValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
                Detail = "See the errors property.",
                Type = ValidationProblemType,
            });
        }

        if (error.RetryAfter is { } retryAfter)
        {
            // Whole seconds, rounded up: rounding down would invite a retry that is refused again.
            httpContext.Response.Headers.RetryAfter =
                ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        }

        var (status, title) = error.Type switch
        {
            ErrorType.Validation => (StatusCodes.Status400BadRequest, "The request is invalid."),
            ErrorType.Unauthorized => (StatusCodes.Status401Unauthorized, "Authentication failed."),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "The resource was not found."),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "The request conflicts with the current state."),
            ErrorType.TooManyRequests => (StatusCodes.Status429TooManyRequests, "Too many requests."),
            _ => throw new NotSupportedException($"Unhandled {nameof(ErrorType)}: {error.Type}."),
        };

        return TypedResults.Problem(
            detail: error.Message,
            statusCode: status,
            title: title,
            // A stable URI per error code, so a client can branch on "auth.email_taken" without
            // parsing the human-readable detail.
            type: ProblemTypeBase + error.Code);
    }
}
