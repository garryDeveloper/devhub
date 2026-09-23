namespace DevHub.Application.Common;

/// <summary>
/// A row changed between being read and being saved: another request got there first.
/// </summary>
/// <remarks>
/// The provider-neutral form of EF Core's <c>DbUpdateConcurrencyException</c>, for the same
/// reason as <see cref="UniqueConstraintViolationException"/>: a handler can react to it without
/// referencing EF Core. Raised only for entities mapped with a concurrency token (DEVHUB-016
/// maps PostgreSQL's <c>xmin</c> on <c>refresh_tokens</c>). Nothing was written — the whole
/// SaveChanges rolled back.
/// </remarks>
public sealed class ConcurrencyConflictException(Exception innerException)
    : Exception("The row was changed by another request after it was read.", innerException);
