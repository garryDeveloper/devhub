using DevHub.Application.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DevHub.Infrastructure.Persistence;

/// <summary>
/// <see cref="IUnitOfWork"/> over <see cref="DevHubDbContext"/>, plus the one translation the
/// Application layer needs from the provider: a unique-index violation.
/// </summary>
/// <remarks>
/// The translation lives here and not in <see cref="DevHubDbContext.SaveChangesAsync"/> so that
/// code talking to the DbContext directly (the migration tests, future seeders) still sees the
/// provider's own <see cref="DbUpdateException"/>.
/// </remarks>
internal sealed class EfUnitOfWork(DevHubDbContext dbContext) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgres)
        {
            throw new UniqueConstraintViolationException(postgres.ConstraintName, exception);
        }
    }
}
