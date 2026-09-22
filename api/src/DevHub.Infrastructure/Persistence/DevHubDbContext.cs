using System.Reflection;
using DevHub.Application.Common;
using DevHub.Domain.Common;
using DevHub.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace DevHub.Infrastructure.Persistence;

/// <summary>
/// The single unit of work over the DevHub database.
/// </summary>
/// <remarks>
/// Conventions are configured once, globally, in <see cref="ConfigureConventions"/>. Setting
/// them per entity would hold only until the first time someone forgets — and that omission
/// surfaces as a corrective migration, not as a compile error.
/// </remarks>
public sealed class DevHubDbContext : DbContext
{
    private readonly ITimeProvider _timeProvider;
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    public DevHubDbContext(
        DbContextOptions<DevHubDbContext> options,
        ITimeProvider timeProvider,
        IDomainEventDispatcher domainEventDispatcher)
        : base(options)
    {
        _timeProvider = timeProvider;
        _domainEventDispatcher = domainEventDispatcher;
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // citext gives case-insensitive uniqueness in the database itself, rather than by
        // lowercasing on the way in and trusting every future write path to remember.
        // Declared here so it lands in the migration as `CREATE EXTENSION` — it is part of the
        // schema, not something to run by hand on each environment.
        modelBuilder.HasPostgresExtension("citext");

        // One IEntityTypeConfiguration per entity, discovered by scanning. A new entity is
        // mapped by adding a file, never by growing this method.
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        base.OnModelCreating(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Every timestamp is timestamptz, stored UTC (database-schema.md §1). DateTimeOffset is
        // the type used throughout the domain precisely because Npgsql maps it to timestamptz,
        // while a plain DateTime invites `timestamp without time zone` and an entire class of
        // "the deploy happened an hour ago... or was it two?" bugs.
        configurationBuilder.Properties<DateTimeOffset>().HaveColumnType("timestamptz");

        base.ConfigureConventions(configurationBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampAuditTimestamps();

        // Collected and cleared BEFORE the save, dispatched after it. An event dispatched
        // before the commit can announce something that is then rolled back; leaving the
        // events on the entities means the next SaveChanges dispatches them a second time.
        var domainEvents = ChangeTracker.Entries<AggregateRoot>()
            .SelectMany(entry => entry.Entity.DomainEvents)
            .ToList();

        foreach (var entry in ChangeTracker.Entries<AggregateRoot>())
        {
            entry.Entity.ClearDomainEvents();
        }

        var affectedRows = await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (domainEvents.Count > 0)
        {
            await _domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken).ConfigureAwait(false);
        }

        return affectedRows;
    }

    /// <summary>
    /// Sets <c>created_at</c> and <c>updated_at</c> so no handler and no domain method has to.
    /// </summary>
    /// <remarks>
    /// Written through the change tracker rather than through a property setter: <see
    /// cref="IAuditable"/> exposes getters only (CLAUDE.md §4 forbids public setters on
    /// aggregate state), and EF reaches the backing field regardless of accessibility.
    /// <c>nameof</c> keeps it from being a magic string that survives a rename.
    /// </remarks>
    private void StampAuditTimestamps()
    {
        var now = _timeProvider.UtcNow;

        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Property(nameof(IAuditable.CreatedAt)).CurrentValue = now;
                    entry.Property(nameof(IAuditable.UpdatedAt)).CurrentValue = now;
                    break;

                case EntityState.Modified:
                    // CreatedAt is immutable. Marking it unmodified means a caller that tampers
                    // with it cannot rewrite history through an UPDATE.
                    entry.Property(nameof(IAuditable.CreatedAt)).IsModified = false;
                    entry.Property(nameof(IAuditable.UpdatedAt)).CurrentValue = now;
                    break;
            }
        }
    }
}
