namespace DevHub.Application.Common;

/// <summary>
/// Handles a command: an intent to change state. A command loads an aggregate through a
/// repository, calls a domain method, saves, and returns a DTO or a <see cref="Result"/>.
/// </summary>
public interface ICommandHandler<in TCommand, TResponse>
{
    Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken);
}

/// <summary>A command with no return value beyond success or failure.</summary>
public interface ICommandHandler<in TCommand> : ICommandHandler<TCommand, Result>
{
}
