using DevHub.Application.Common;
using DevHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DevHub.Infrastructure.Identity;

/// <summary>
/// Deletes refresh tokens that expired more than <see cref="Retention"/> ago (DEVHUB-016,
/// database-schema.md §2). Runs shortly after startup, then once a day.
/// </summary>
/// <remarks>
/// Every refresh leaves a revoked row behind, so without this the table only grows. Revoked
/// rows need no rule of their own: every row expires, so every revoked row is purged once its
/// expiry is old enough.
/// <para>
/// Why keep dead rows 60 days at all: presenting an old token is how reuse is detected, and the
/// rows are the only record of which sessions a user had. Past expiry + 60 days neither matters
/// — a purged token is simply unknown, and still answers 401.
/// </para>
/// <para>
/// In-process rather than a scheduled Lambda (EPIC 18): one DELETE a day does not justify the
/// infrastructure. With several API instances each runs it; the DELETE is idempotent, so the
/// only cost is a few redundant statements.
/// </para>
/// </remarks>
public sealed partial class RefreshTokenCleanupService(
    IServiceScopeFactory scopeFactory,
    ITimeProvider clock,
    ILogger<RefreshTokenCleanupService> logger)
    : BackgroundService
{
    public static readonly TimeSpan Retention = TimeSpan.FromDays(60);

    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    // Out of the way of startup itself — and of migrations, when a fresh environment is still
    // creating the table.
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(1);

    /// <summary>One purge pass. Public so tests can run it without waiting for the timer.</summary>
    /// <returns>The number of rows deleted.</returns>
    public async Task<int> PurgeOnceAsync(CancellationToken cancellationToken)
    {
        var cutoff = clock.UtcNow - Retention;

        // BackgroundService is a singleton; DbContext is scoped. One scope per pass, so a pass
        // never reuses a context — or its change tracker — from the day before.
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DevHubDbContext>();

        return await dbContext.RefreshTokens
            .Where(token => token.ExpiresAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(InitialDelay, stoppingToken).ConfigureAwait(false);

            using var timer = new PeriodicTimer(Interval);

            do
            {
                await PurgeAndLogAsync(stoppingToken).ConfigureAwait(false);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host shutting down.
        }
    }

    private async Task PurgeAndLogAsync(CancellationToken stoppingToken)
    {
        try
        {
            var deleted = await PurgeOnceAsync(stoppingToken).ConfigureAwait(false);
            LogPurged(logger, deleted, Retention.Days);
        }
#pragma warning disable CA1031 // Housekeeping must never take the API down: since .NET 8 an
        // exception escaping ExecuteAsync stops the host. Log it and try again tomorrow.
        catch (Exception exception) when (exception is not OperationCanceledException)
#pragma warning restore CA1031
        {
            LogPurgeFailed(logger, exception);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Purged {Count} refresh token(s) expired more than {Days} days ago.")]
    private static partial void LogPurged(ILogger logger, int count, int days);

    [LoggerMessage(Level = LogLevel.Error, Message = "Refresh token purge failed; retrying at the next interval.")]
    private static partial void LogPurgeFailed(ILogger logger, Exception exception);
}
