using DevHub.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DevHub.Api.IntegrationTests;

/// <summary>
/// DEVHUB-006's acceptance criteria, asserted against a real PostgreSQL.
/// </summary>
/// <remarks>
/// These read the schema from <c>information_schema</c> rather than from EF's model. EF's model
/// is what EF <i>intended</i>; the catalog is what PostgreSQL actually built. A convention that
/// silently stops being applied — snake_case, timestamptz, citext — only shows up in the second.
/// </remarks>
public sealed class MigrationTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _postgres;

    public MigrationTests(PostgresFixture postgres) => _postgres = postgres;

    [Fact]
    public async Task Migrations_apply_to_an_empty_database_and_leave_none_pending()
    {
        await using var context = _postgres.CreateContext();

        var applied = await context.Database.GetAppliedMigrationsAsync();
        var pending = await context.Database.GetPendingMigrationsAsync();

        Assert.NotEmpty(applied);
        Assert.Empty(pending);
    }

    [Fact]
    public async Task Users_table_uses_snake_case_columns()
    {
        var columns = await QueryColumnNamesAsync("users");

        Assert.Equal(
            ["id", "email", "display_name", "avatar_key", "password_hash", "created_at", "updated_at"],
            columns);
    }

    [Fact]
    public async Task Email_is_citext_so_uniqueness_is_case_insensitive()
    {
        var type = await QueryScalarAsync<string>(
            "select udt_name from information_schema.columns where table_name = 'users' and column_name = 'email';");

        Assert.Equal("citext", type);
    }

    [Fact]
    public async Task Timestamps_are_timestamptz_not_naive_local_time()
    {
        var types = await QueryColumnTypesAsync("users", "created_at", "updated_at");

        Assert.All(types, type => Assert.Equal("timestamptz", type));
    }

    [Fact]
    public async Task Email_is_uniquely_indexed()
    {
        var isUnique = await QueryScalarAsync<bool>(
            """
            select i.indisunique
            from pg_index i
            join pg_class c on c.oid = i.indexrelid
            where c.relname = 'ix_users_email';
            """);

        Assert.True(isUnique);
    }

    /// <summary>
    /// The behaviour the index exists for: citext, not a lowercasing convention that every
    /// future write path has to remember.
    /// </summary>
    [Fact]
    public async Task Emails_differing_only_in_case_collide()
    {
        await using var context = _postgres.CreateContext();

        context.Users.Add(new User(Guid.CreateVersion7(), "Ada@devhub.io", "Ada", "hash"));
        await context.SaveChangesAsync();

        context.Users.Add(new User(Guid.CreateVersion7(), "ada@devhub.io", "Ada again", "hash"));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal("23505", ((PostgresException)exception.InnerException!).SqlState);
    }

    /// <summary>
    /// The acceptance criterion "saving an entity sets created_at/updated_at without the caller
    /// doing it", plus the rule that created_at is immutable once written.
    /// </summary>
    [Fact]
    public async Task Saving_stamps_audit_timestamps_and_created_at_never_changes()
    {
        var id = Guid.CreateVersion7();

        await using (var context = _postgres.CreateContext())
        {
            // Note what is NOT here: no CreatedAt, no UpdatedAt. The caller cannot set them.
            context.Users.Add(new User(id, "audit@devhub.io", "Audit", "hash"));
            await context.SaveChangesAsync();
        }

        await using (var context = _postgres.CreateContext())
        {
            var user = await context.Users.SingleAsync(u => u.Id == id);

            Assert.Equal(PostgresFixture.FixedClock.Instant, user.CreatedAt);
            Assert.Equal(PostgresFixture.FixedClock.Instant, user.UpdatedAt);
        }
    }

    private async Task<string[]> QueryColumnNamesAsync(string table)
    {
        await using var connection = new NpgsqlConnection(_postgres.ConnectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            "select column_name from information_schema.columns where table_name = @table order by ordinal_position;",
            connection);
        command.Parameters.AddWithValue("table", table);

        var names = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            names.Add(reader.GetString(0));
        }

        return [.. names];
    }

    private async Task<string[]> QueryColumnTypesAsync(string table, params string[] columns)
    {
        await using var connection = new NpgsqlConnection(_postgres.ConnectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            """
            select udt_name from information_schema.columns
            where table_name = @table and column_name = any(@columns);
            """,
            connection);
        command.Parameters.AddWithValue("table", table);
        command.Parameters.AddWithValue("columns", columns);

        var types = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            types.Add(reader.GetString(0));
        }

        return [.. types];
    }

    private async Task<T> QueryScalarAsync<T>(string sql)
    {
        await using var connection = new NpgsqlConnection(_postgres.ConnectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        var value = await command.ExecuteScalarAsync();

        Assert.NotNull(value);
        return (T)value!;
    }
}
