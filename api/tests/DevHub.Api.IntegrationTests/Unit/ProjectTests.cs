using DevHub.Domain.Common;
using DevHub.Domain.Projects;

namespace DevHub.Api.IntegrationTests.Unit;

/// <summary>
/// DEVHUB-030's invariants on the <see cref="Project"/> aggregate: key format and immutability,
/// archive/unarchive, member rules and the issue sequence counter.
/// </summary>
/// <remarks>
/// Pure, so it belongs in <c>DevHub.Domain.UnitTests</c> once DEVHUB-011 creates it — same
/// arrangement as <see cref="WorkspaceTests"/>.
/// </remarks>
public sealed class ProjectTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);

    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _creatorId = Guid.NewGuid();

    [Fact]
    public void Create_yields_exactly_one_member_and_it_is_the_creator()
    {
        var project = Project.Create(_workspaceId, "DevHub", "DEV", _creatorId, Now);

        var member = Assert.Single(project.Members);
        Assert.Equal(_creatorId, member.UserId);
        Assert.Equal(ProjectRole.Member, member.Role);
        Assert.Equal(project.Id, member.ProjectId);
        Assert.Equal(Now, member.AddedAt);
        Assert.Equal(_workspaceId, project.WorkspaceId);
    }

    [Fact]
    public void Create_assigns_version7_ids_to_the_project_and_its_member()
    {
        var project = Project.Create(_workspaceId, "DevHub", "DEV", _creatorId, Now);

        Assert.Equal(7, project.Id.Version);
        Assert.Equal(7, project.Members.Single().Id.Version);
    }

    [Fact]
    public void Create_starts_with_no_issues_and_is_not_archived()
    {
        var project = Project.Create(_workspaceId, "DevHub", "DEV", _creatorId, Now);

        Assert.Equal(0, project.IssueSequence);
        Assert.Null(project.ArchivedAt);
    }

    [Fact]
    public void Create_rejects_an_empty_workspace()
    {
        Assert.Throws<DomainException>(() => Project.Create(Guid.Empty, "DevHub", "DEV", _creatorId, Now));
    }

    // ---- Key: format and immutability -------------------------------------------------------

    [Theory]
    [InlineData("DEV")]
    [InlineData("A1")]
    [InlineData("ABCDEFGHIJ")]
    public void A_valid_key_is_accepted_as_given(string key)
    {
        Assert.Equal(key, Project.Create(_workspaceId, "DevHub", key, _creatorId, Now).Key);
    }

    [Theory]
    [InlineData("dev")]
    [InlineData("1EV")]
    [InlineData("A")]
    [InlineData("ABCDEFGHIJK")]
    [InlineData("DEV-1")]
    [InlineData("DEV 1")]
    public void An_invalid_key_is_rejected_not_rewritten(string key)
    {
        Assert.Throws<DomainException>(() => Project.Create(_workspaceId, "DevHub", key, _creatorId, Now));
    }

    [Fact]
    public void Nothing_in_the_aggregate_can_change_the_key()
    {
        var project = Project.Create(_workspaceId, "DevHub", "DEV", _creatorId, Now);

        project.Rename("DevHub Platform");
        project.UpdateDescription("A platform.");
        project.SetAppearance("#112233", "rocket");

        Assert.Equal("DEV", project.Key);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("dev")]
    [InlineData("1EV")]
    [InlineData("A")]
    [InlineData("ABCDEFGHIJK")]
    public void ProjectKey_IsValid_rejects_every_key_Validate_rejects(string? key)
    {
        Assert.False(ProjectKey.IsValid(key));
    }

    [Theory]
    [InlineData("DEV")]
    [InlineData("A1")]
    public void ProjectKey_IsValid_accepts_what_Validate_accepts(string key)
    {
        ProjectKey.Validate(key);

        Assert.True(ProjectKey.IsValid(key));
    }

    // ---- Name / description / appearance -----------------------------------------------------

    [Fact]
    public void Name_is_trimmed_on_create_and_on_rename()
    {
        var project = Project.Create(_workspaceId, "  DevHub  ", "DEV", _creatorId, Now);
        Assert.Equal("DevHub", project.Name);

        project.Rename("  DevHub Platform ");
        Assert.Equal("DevHub Platform", project.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_missing_name_is_rejected(string? name)
    {
        Assert.ThrowsAny<ArgumentException>(() => Project.Create(_workspaceId, name!, "DEV", _creatorId, Now));
    }

    [Fact]
    public void A_name_longer_than_80_characters_is_rejected()
    {
        var project = Project.Create(_workspaceId, "DevHub", "DEV", _creatorId, Now);

        Assert.Throws<DomainException>(() => Project.Create(_workspaceId, new string('a', 81), "DEV", _creatorId, Now));
        Assert.Throws<DomainException>(() => project.Rename(new string('a', 81)));
    }

    [Fact]
    public void UpdateDescription_accepts_null_to_clear_it()
    {
        var project = Project.Create(_workspaceId, "DevHub", "DEV", _creatorId, Now);
        project.UpdateDescription("Something");

        project.UpdateDescription(null);

        Assert.Null(project.Description);
    }

    [Fact]
    public void A_description_longer_than_2000_characters_is_rejected()
    {
        var project = Project.Create(_workspaceId, "DevHub", "DEV", _creatorId, Now);

        Assert.Throws<DomainException>(() => project.UpdateDescription(new string('a', 2001)));
    }

    [Fact]
    public void SetAppearance_accepts_nulls_to_clear_both()
    {
        var project = Project.Create(_workspaceId, "DevHub", "DEV", _creatorId, Now);
        project.SetAppearance("#112233", "rocket");

        project.SetAppearance(null, null);

        Assert.Null(project.Color);
        Assert.Null(project.Icon);
    }

    // ---- Archive / unarchive ------------------------------------------------------------------

    [Fact]
    public void Archiving_sets_ArchivedAt_and_can_be_restored()
    {
        var project = Project.Create(_workspaceId, "DevHub", "DEV", _creatorId, Now);

        project.Archive(Now);
        Assert.Equal(Now, project.ArchivedAt);

        project.Unarchive();
        Assert.Null(project.ArchivedAt);
    }

    [Fact]
    public void Archiving_an_already_archived_project_throws()
    {
        var project = Project.Create(_workspaceId, "DevHub", "DEV", _creatorId, Now);
        project.Archive(Now);

        Assert.Throws<DomainException>(() => project.Archive(Now));
    }

    [Fact]
    public void Unarchiving_an_active_project_throws()
    {
        var project = Project.Create(_workspaceId, "DevHub", "DEV", _creatorId, Now);

        Assert.Throws<DomainException>(() => project.Unarchive());
    }

    // ---- Issue sequence ------------------------------------------------------------------------

    [Fact]
    public void NextIssueNumber_starts_at_one_and_never_goes_back()
    {
        var project = Project.Create(_workspaceId, "DevHub", "DEV", _creatorId, Now);

        Assert.Equal(1, project.NextIssueNumber());
        Assert.Equal(2, project.NextIssueNumber());
        Assert.Equal(2, project.IssueSequence);
    }

    // ---- Members ---------------------------------------------------------------------------

    [Fact]
    public void Adding_the_same_user_twice_is_rejected()
    {
        var project = Project.Create(_workspaceId, "DevHub", "DEV", _creatorId, Now);
        var userId = Guid.NewGuid();
        project.AddMember(userId, Now);

        Assert.Throws<DomainException>(() => project.AddMember(userId, Now));
        Assert.Equal(2, project.Members.Count);
    }

    [Fact]
    public void Adding_the_creator_again_is_rejected()
    {
        var project = Project.Create(_workspaceId, "DevHub", "DEV", _creatorId, Now);

        Assert.Throws<DomainException>(() => project.AddMember(_creatorId, Now));
    }

    [Fact]
    public void An_empty_user_cannot_be_added()
    {
        var project = Project.Create(_workspaceId, "DevHub", "DEV", _creatorId, Now);

        Assert.Throws<DomainException>(() => project.AddMember(Guid.Empty, Now));
    }

    [Fact]
    public void A_member_can_be_removed()
    {
        var project = Project.Create(_workspaceId, "DevHub", "DEV", _creatorId, Now);
        var member = project.AddMember(Guid.NewGuid(), Now);

        project.RemoveMember(member.Id);

        Assert.DoesNotContain(member, project.Members);
    }

    [Fact]
    public void Removing_a_member_of_another_project_throws()
    {
        var project = Project.Create(_workspaceId, "DevHub", "DEV", _creatorId, Now);
        var stranger = Project.Create(_workspaceId, "Other", "OTH", Guid.NewGuid(), Now).Members.Single();

        Assert.Throws<DomainException>(() => project.RemoveMember(stranger.Id));
    }

    [Fact]
    public void Members_cannot_be_modified_from_outside_the_aggregate()
    {
        var project = Project.Create(_workspaceId, "DevHub", "DEV", _creatorId, Now);

        // A read-only view, not the list itself: casting back to a mutable collection must not
        // be a way to bypass the aggregate's own rules.
        Assert.False(project.Members is List<ProjectMember>);
        Assert.True(((ICollection<ProjectMember>)project.Members).IsReadOnly);
    }
}
