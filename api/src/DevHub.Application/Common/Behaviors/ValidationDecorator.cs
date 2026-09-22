using FluentValidation;

namespace DevHub.Application.Common.Behaviors;

/// <summary>
/// The "Validation" step of the pipeline in backend-architecture.md §4: runs every
/// <see cref="IValidator{T}"/> for the command and, if any fails, returns a
/// <see cref="ErrorType.Validation"/> result without ever calling the handler.
/// </summary>
/// <remarks>
/// A decorator, not a call at the top of each handler, so a new command cannot forget to
/// validate: writing the validator class is enough. And a decorator, not an MVC filter, so the
/// rules also hold when a handler is reached from a background job or a test rather than HTTP.
/// <para>
/// Validation failures are returned, not thrown — they are the most expected failure there is.
/// <see cref="IFailureResult{TSelf}"/> is what lets this generic class build a failed
/// <typeparamref name="TResponse"/>.
/// </para>
/// </remarks>
internal sealed class ValidationDecorator<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> inner,
    IEnumerable<IValidator<TCommand>> validators)
    : ICommandHandler<TCommand, TResponse>
    where TResponse : Result, IFailureResult<TResponse>
{
    public async Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken)
    {
        var failures = new List<FluentValidation.Results.ValidationFailure>();

        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            failures.AddRange(result.Errors);
        }

        if (failures.Count == 0)
        {
            return await inner.HandleAsync(command, cancellationToken).ConfigureAwait(false);
        }

        var fieldErrors = failures
            .GroupBy(failure => failure.PropertyName, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray(),
                StringComparer.Ordinal);

        return TResponse.FromError(Error.Validation(fieldErrors));
    }
}
