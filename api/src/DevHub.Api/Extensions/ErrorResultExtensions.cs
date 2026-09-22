using System.Globalization;
using System.Text.Json;
using DevHub.Application.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DevHub.Api.Extensions;

/// <summary>
/// Turns an Application <see cref="Error"/> into the RFC 7807 response of api-conventions.md §4.
/// The one place an <see cref="ErrorType"/> becomes a status code, so every controller agrees.
/// </summary>
public static class ErrorResultExtensions
{
    public const string ValidationProblemType = "https://devhub.dev/errors/validation";

    public static IActionResult ToProblem(this ControllerBase controller, Error error)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(error);

        if (error is { Type: ErrorType.Validation, FieldErrors: { } fieldErrors })
        {
            var modelState = new ModelStateDictionary();

            foreach (var (field, messages) in fieldErrors)
            {
                // The validator reports C# property names ("Password"); the client sent JSON
                // ones ("password"). errors.password is what the client can map back to a field.
                var key = JsonNamingPolicy.CamelCase.ConvertName(field);

                foreach (var message in messages)
                {
                    modelState.AddModelError(key, message);
                }
            }

            return controller.ValidationProblem(
                detail: "See the errors property.",
                type: ValidationProblemType,
                modelStateDictionary: modelState);
        }

        if (error.RetryAfter is { } retryAfter)
        {
            // Whole seconds, rounded up: rounding down would invite a retry that is refused again.
            controller.Response.Headers.RetryAfter =
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

        return controller.Problem(
            detail: error.Message,
            statusCode: status,
            title: title,
            // A stable URI per error code, so a client can branch on "auth.email_taken" without
            // parsing the human-readable detail.
            type: $"https://devhub.dev/errors/{error.Code}");
    }
}
