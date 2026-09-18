namespace DevHub.Domain.Common;

/// <summary>
/// Marks an entity whose row carries <c>created_at</c> and <c>updated_at</c>. The persistence
/// layer stamps both on save, so no handler and no domain method ever sets them by hand.
/// </summary>
/// <remarks>
/// Read-only on purpose. CLAUDE.md §4 forbids public setters on aggregate state, and audit
/// timestamps are state. Implementations use <c>private set</c>; the DbContext writes through
/// EF's change tracker, which reaches the backing field regardless of setter accessibility.
/// <para>
/// Not every table is auditable — <c>issue_activities</c> and <c>refresh_tokens</c> carry only a
/// creation timestamp, and <c>workspace_members</c> uses <c>joined_at</c> — which is why this is
/// an interface rather than another base class in the hierarchy.
/// </para>
/// </remarks>
public interface IAuditable
{
    /// <summary>UTC. Set once, when the row is first inserted.</summary>
    DateTimeOffset CreatedAt { get; }

    /// <summary>UTC. Equal to <see cref="CreatedAt"/> until the first update.</summary>
    DateTimeOffset UpdatedAt { get; }
}
