using DevHub.Application.Common;
using DevHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace DevHub.Api.IntegrationTests.Harness;

/// <summary>
/// The real API — every middleware, filter, handler and the real migrations — in memory, over a
/// throwaway PostgreSQL 16 container. One per test class (<c>IClassFixture</c>).
/// </summary>
/// <remarks>
/// The first slice of DEVHUB-011's harness, built by DEVHUB-014 because it is the first ticket
/// with an endpoint to test. Respawn, seed builders and the auth helper are still DEVHUB-011's;
/// until then tests isolate themselves by using a unique email each.
/// <para>
/// Runs as environment <c>Testing</c>, never <c>Development</c>: Development loads user-secrets,
/// so a developer's local <c>ConnectionStrings:Default</c> or <c>Jwt:Secret</c> would silently
/// override the ones below — and the tests would pass or fail against their own database.
/// </para>
/// </remarks>
public class DevHubApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>Known to the tests so they can forge tokens (expired, tampered) on purpose.</summary>
    public const string JwtSecret = "integration-tests-only-secret-not-for-any-real-environment";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

    /// <summary>The API's clock. Tests move it instead of sleeping (DEVHUB-011 technical notes).</summary>
    public TestClock Clock { get; } = new();

    /// <summary>Tests send every request from one in-memory "IP", so the real limit of 10 would trip.</summary>
    protected virtual int AuthPermitLimit => 10_000;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DevHubDbContext>().Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    /// <summary>A DbContext on the same database the API uses, for asserting what was persisted.</summary>
    public async Task<T> QueryDbAsync<T>(Func<DevHubDbContext, Task<T>> query)
    {
        await using var scope = Services.CreateAsyncScope();
        return await query(scope.ServiceProvider.GetRequiredService<DevHubDbContext>());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // The connection string is read eagerly in Program.cs (AddInfrastructure fails fast on a
        // missing one), before configuration added via ConfigureAppConfiguration is visible.
        // UseSetting lands early enough.
        builder.UseSetting("ConnectionStrings:Default", _postgres.GetConnectionString());

        // Everything else is read through IOptions<T>, after the host is built, so an ordinary
        // in-memory source — added last, so it wins — is enough.
        builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = JwtSecret,
                ["Cors:AllowedOrigins:0"] = "http://localhost",
                ["Storage:BucketName"] = "devhub-integration-tests",
                ["Webhooks:DefaultSecret"] = "integration-tests-only-webhook-secret-0123456789",
                ["RateLimiting:AuthPermitLimit"] = AuthPermitLimit.ToString(System.Globalization.CultureInfo.InvariantCulture),
            }));

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<ITimeProvider>(Clock);

            // Adds GET /api/test/whoami: DevHub has no authenticated endpoint yet (GET /api/me is
            // DEVHUB-019), and "a token is accepted by a protected endpoint" needs one.
            services.AddTransient<IStartupFilter, ProtectedTestEndpoint>();
        });
    }
}

/// <summary>Real time until a test moves it.</summary>
public sealed class TestClock : ITimeProvider
{
    private TimeSpan _offset;

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow + _offset;

    public void Advance(TimeSpan by) => _offset += by;
}
