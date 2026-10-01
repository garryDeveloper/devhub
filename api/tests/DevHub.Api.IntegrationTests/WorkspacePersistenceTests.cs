using DevHub.Domain.Users;
using DevHub.Domain.Workspaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DevHub.Api.IntegrationTests;

/// <summary>
/// DEVHUB-023's persistence half: the <c>workspaces</c>/<c>workspace_members</c> mapping and the
/// constraints the domain cannot guarantee on its own, against a real PostgreSQL.
/// </summary>
public sealed class WorkspacePersistenceTests : IClassFixture<PostgresFixture>
{
    private const string UniqueViolation = "23505";
    private const string CheckViolation = "23514";

    private static readonly DateTimeOffset Now = PostgresFixture.FixedClock.Instant;

    private readonly PostgresFixture _postgres;

    public WorkspacePersistenceTests(PostgresFixture postgres) => _postgres = postgres;

    [Fact]
    public async Task A_workspace_round_trips_with_its_owner_member()
    {
        var ownerId = await CreateUserAsync();
        var workspace = Workspace.Create("Round Trip", ownerId, Now);

        await using (var context = _postgres.CreateContext())
        {
            context.Workspaces.Add(workspace);
            await context.SaveChangesAsync();
        }

        await using (var context = _postgres.CreateContext())
        {
            var loaded = await context.Workspaces
                .Include(w => w.Members)
                .SingleAsync(w => w.Id == workspace.Id);

            Assert.Equal("Round Trip", loaded.Name);
            Assert.Equal(workspace.Slug, loaded.Slug);
            Assert.Equal(Now, loaded.CreatedAt);

            var owner = Assert.Single(loaded.Members);
            Assert.Equal(ownerId, owner.UserId);
            Assert.Equal(WorkspaceRole.Owner, owner.Role);
        }
    }

    /// <summary>
    /// The path DEVHUB-025 relies on: load the aggregate, add and remove through it, save. The
    /// new member must be INSERTed and the removed one DELETEd — nobody calls Add or Remove on a
    /// DbSet of members, because there isn't one.
    /// </summary>
    [Fact]
    public async Task Members_added_and_removed_through_a_loaded_workspace_are_persisted()
    {
        var ownerId = await CreateUserAsync();
        var firstUserId = await CreateUserAsync();
        var secondUserId = await CreateUserAsync();
        var workspace = Workspace.Create("Membership", ownerId, Now);
        var firstMember = workspace.AddMember(firstUserId, WorkspaceRole.Member, Now);

        await using (var context = _postgres.CreateContext())
        {
            context.Workspaces.Add(workspace);
            await context.SaveChangesAsync();
        }

        await using (var context = _postgres.CreateContext())
        {
            var loaded = await context.Workspaces.Include(w => w.Members).SingleAsync(w => w.Id == workspace.Id);

            loaded.AddMember(secondUserId, WorkspaceRole.Owner, Now);
            loaded.RemoveMember(firstMember.Id);

            await context.SaveChangesAsync();
        }

        await using (var context = _postgres.CreateContext())
        {
            var loaded = await context.Workspaces.Include(w => w.Members).SingleAsync(w => w.Id == workspace.Id);

            Assert.Equal(
                new[] { ownerId, secondUserId }.Order(),
                loaded.Members.Select(m => m.UserId).Order());
        }
    }

    [Fact]
    public async Task The_same_user_twice_in_one_workspace_is_rejected_by_the_unique_index()
    {
        var ownerId = await CreateUserAsync();
        var workspace = Workspace.Create("Duplicate Member", ownerId, Now);

        await using (var context = _postgres.CreateContext())
        {
            context.Workspaces.Add(workspace);
            await context.SaveChangesAsync();
        }

        // Bypasses the domain on purpose — this is what two concurrent "add member" requests
        // look like, each of which passed the in-memory check before either saved.
        var sqlState = await ExecuteExpectingFailureAsync(
            "insert into workspace_members (id, workspace_id, user_id, role, joined_at) values (@id, @workspace, @user, 'Member', now());",
            ("id", Guid.CreateVersion7()),
            ("workspace", workspace.Id),
            ("user", ownerId));

        Assert.Equal(UniqueViolation, sqlState);
    }

    [Fact]
    public async Task Slugs_are_unique_across_the_system()
    {
        var ownerId = await CreateUserAsync();

        await using var context = _postgres.CreateContext();

        context.Workspaces.Add(Workspace.Create("Taken", ownerId, Now, slug: "taken-slug"));
        await context.SaveChangesAsync();

        context.Workspaces.Add(Workspace.Create("Also Taken", ownerId, Now, slug: "taken-slug"));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        Assert.Equal(UniqueViolation, Assert.IsType<PostgresException>(exception.InnerException).SqlState);
    }

    [Fact]
    public async Task Roles_are_stored_as_text_and_constrained_to_the_mvp_roles()
    {
        var ownerId = await CreateUserAsync();
        var workspace = Workspace.Create("Roles", ownerId, Now);

        await using (var context = _postgres.CreateContext())
        {
            context.Workspaces.Add(workspace);
            await context.SaveChangesAsync();
        }

        var storedRole = await QueryScalarAsync(
            "select role from workspace_members where workspace_id = @workspace;",
            ("workspace", workspace.Id));
        Assert.Equal("Owner", storedRole);

        var sqlState = await ExecuteExpectingFailureAsync(
            "update workspace_members set role = 'Admin' where workspace_id = @workspace;",
            ("workspace", workspace.Id));
        Assert.Equal(CheckViolation, sqlState);
    }

    /// <summary>
    /// The index the ticket calls "not optional". Its existence is asserted from the catalog so
    /// that dropping it in a later migration fails a test rather than a latency graph.
    /// </summary>
    [Fact]
    public async Task Workspace_members_are_indexed_by_user_id()
    {
        var definition = await QueryScalarAsync(
            "select indexdef from pg_indexes where tablename = 'workspace_members' and indexname = 'ix_workspace_members_user_id';");

        Assert.Contains("(user_id)", definition);
    }

    private async Task<Guid> CreateUserAsync()
    {
        await using var context = _postgres.CreateContext();

        var user = User.Create($"{Guid.NewGuid():N}@devhub.io", "Workspace Tester", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user.Id;
    }

    private async Task<string> QueryScalarAsync(string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = new NpgsqlConnection(_postgres.ConnectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        var result = await command.ExecuteScalarAsync();
        return Assert.IsType<string>(result);
    }

    private async Task<string> ExecuteExpectingFailureAsync(string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = new NpgsqlConnection(_postgres.ConnectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        var exception = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
        return exception.SqlState;
    }
}
