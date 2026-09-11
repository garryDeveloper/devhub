namespace DevHub.Application.Common;

/// <summary>
/// Handles a query: a read that never changes state. Queries bypass repositories and project
/// straight from the DbContext with AsNoTracking() into a DTO — see
/// docs/tech-specs/backend-architecture.md §4.
/// </summary>
public interface IQueryHandler<in TQuery, TResponse>
{
    Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
