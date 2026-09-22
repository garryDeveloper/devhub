namespace DevHub.Application.Common;

/// <summary>
/// Commits everything the current request changed, in one transaction. Repositories only stage
/// changes; nothing reaches the database until a handler calls this.
/// </summary>
public interface IUnitOfWork
{
    /// <exception cref="UniqueConstraintViolationException">
    /// A unique index rejected the write. Handlers that can race on a unique value catch this
    /// and turn it into a <see cref="ErrorType.Conflict"/>.
    /// </exception>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
