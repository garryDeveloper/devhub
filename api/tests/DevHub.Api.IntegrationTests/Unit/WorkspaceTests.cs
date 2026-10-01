using DevHub.Domain.Common;
using DevHub.Domain.Workspaces;

namespace DevHub.Api.IntegrationTests.Unit;

/// <summary>
/// DEVHUB-023's invariants on the <see cref="Workspace"/> aggregate: the owner created with it,
/// the last-owner rules, duplicate members and the name/slug rules.
/// </summary>
/// <remarks>
/// Pure, so it belongs in <c>DevHub.Domain.UnitTests</c> once DEVHUB-011 creates it — same
/// arrangement as <see cref="UserTests"/>.
/// </remarks>
public sealed class WorkspaceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 24, 12, 0, 0, TimeSpan.Zero);

    private readonly Guid _ownerId = Guid.NewGuid();

    [Fact]
    public void Create_yields_exactly_one_member_and_it_is_the_owner()
    {
        var workspace = Workspace.Create("Acme", _ownerId, Now);

        var member = Assert.Single(workspace.Members);
        Assert.Equal(_ownerId, member.UserId);
        Assert.Equal(WorkspaceRole.Owner, member.Role);
        Assert.Equal(workspace.Id, member.WorkspaceId);
        Assert.Equal(Now, member.JoinedAt);
        Assert.Equal(_ownerId, workspace.OwnerId);
    }

    [Fact]
    public void Create_assigns_version7_ids_to_the_workspace_and_its_owner()
    {
        var workspace = Workspace.Create("Acme", _ownerId, Now);

        Assert.Equal(7, workspace.Id.Version);
        Assert.Equal(7, workspace.Members.Single().Id.Version);
    }

    [Fact]
    public void Create_rejects_an_empty_owner()
    {
        Assert.Throws<DomainException>(() => Workspace.Create("Acme", Guid.Empty, Now));
    }

    [Fact]
    public void Removing_the_last_owner_throws()
    {
        var workspace = Workspace.Create("Acme", _ownerId, Now);
        var owner = workspace.Members.Single();

        Assert.Throws<DomainException>(() => workspace.RemoveMember(owner.Id));
        Assert.Single(workspace.Members);
    }

    [Fact]
    public void Removing_an_owner_while_a_second_owner_remains_is_allowed()
    {
        var workspace = Workspace.Create("Acme", _ownerId, Now);
        var firstOwner = workspace.Members.Single();
        var secondOwner = workspace.AddMember(Guid.NewGuid(), WorkspaceRole.Owner, Now);

        workspace.RemoveMember(firstOwner.Id);

        Assert.Equal(secondOwner, Assert.Single(workspace.Members));
    }

    [Fact]
    public void Demoting_the_last_owner_throws()
    {
        var workspace = Workspace.Create("Acme", _ownerId, Now);
        var owner = workspace.Members.Single();
        workspace.AddMember(Guid.NewGuid(), WorkspaceRole.Member, Now);

        // A plain member does not count: the rule is about owners, not about members in general.
        Assert.Throws<DomainException>(() => workspace.ChangeMemberRole(owner.Id, WorkspaceRole.Member));
        Assert.Equal(WorkspaceRole.Owner, owner.Role);
    }

    [Fact]
    public void Demoting_an_owner_while_a_second_owner_remains_is_allowed()
    {
        var workspace = Workspace.Create("Acme", _ownerId, Now);
        var firstOwner = workspace.Members.Single();
        workspace.AddMember(Guid.NewGuid(), WorkspaceRole.Owner, Now);

        workspace.ChangeMemberRole(firstOwner.Id, WorkspaceRole.Member);

        Assert.Equal(WorkspaceRole.Member, firstOwner.Role);
    }

    [Fact]
    public void Setting_the_last_owner_to_owner_again_is_a_no_op_not_an_error()
    {
        var workspace = Workspace.Create("Acme", _ownerId, Now);
        var owner = workspace.Members.Single();

        workspace.ChangeMemberRole(owner.Id, WorkspaceRole.Owner);

        Assert.Equal(WorkspaceRole.Owner, owner.Role);
    }

    [Fact]
    public void A_member_can_be_promoted_and_removed()
    {
        var workspace = Workspace.Create("Acme", _ownerId, Now);
        var member = workspace.AddMember(Guid.NewGuid(), WorkspaceRole.Member, Now);

        workspace.ChangeMemberRole(member.Id, WorkspaceRole.Owner);
        Assert.Equal(WorkspaceRole.Owner, member.Role);

        workspace.RemoveMember(member.Id);
        Assert.DoesNotContain(member, workspace.Members);
    }

    [Fact]
    public void Adding_the_same_user_twice_is_rejected()
    {
        var workspace = Workspace.Create("Acme", _ownerId, Now);
        var userId = Guid.NewGuid();
        workspace.AddMember(userId, WorkspaceRole.Member, Now);

        Assert.Throws<DomainException>(() => workspace.AddMember(userId, WorkspaceRole.Owner, Now));
        Assert.Equal(2, workspace.Members.Count);
    }

    [Fact]
    public void Adding_the_creator_again_is_rejected()
    {
        var workspace = Workspace.Create("Acme", _ownerId, Now);

        Assert.Throws<DomainException>(() => workspace.AddMember(_ownerId, WorkspaceRole.Member, Now));
    }

    [Fact]
    public void An_undefined_role_is_rejected()
    {
        var workspace = Workspace.Create("Acme", _ownerId, Now);

        Assert.Throws<DomainException>(() => workspace.AddMember(Guid.NewGuid(), (WorkspaceRole)42, Now));
    }

    [Fact]
    public void Changing_or_removing_a_member_of_another_workspace_throws()
    {
        var workspace = Workspace.Create("Acme", _ownerId, Now);
        var stranger = Workspace.Create("Other", Guid.NewGuid(), Now).Members.Single();

        Assert.Throws<DomainException>(() => workspace.ChangeMemberRole(stranger.Id, WorkspaceRole.Member));
        Assert.Throws<DomainException>(() => workspace.RemoveMember(stranger.Id));
    }

    [Fact]
    public void Members_cannot_be_modified_from_outside_the_aggregate()
    {
        var workspace = Workspace.Create("Acme", _ownerId, Now);

        // A read-only view, not the list itself: casting back to a mutable collection must not
        // be a way around the last-owner rule.
        Assert.False(workspace.Members is List<WorkspaceMember>);
        Assert.True(((ICollection<WorkspaceMember>)workspace.Members).IsReadOnly);
    }

    [Fact]
    public void Name_is_trimmed_on_create_and_on_rename()
    {
        var workspace = Workspace.Create("  Acme  ", _ownerId, Now);
        Assert.Equal("Acme", workspace.Name);

        workspace.Rename("  Acme Corp ");
        Assert.Equal("Acme Corp", workspace.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_missing_name_is_rejected(string? name)
    {
        Assert.ThrowsAny<ArgumentException>(() => Workspace.Create(name!, _ownerId, Now));
    }

    [Fact]
    public void A_name_longer_than_80_characters_is_rejected()
    {
        var workspace = Workspace.Create("Acme", _ownerId, Now);

        Assert.Throws<DomainException>(() => Workspace.Create(new string('a', 81), _ownerId, Now));
        Assert.Throws<DomainException>(() => workspace.Rename(new string('a', 81)));
    }

    [Fact]
    public void Rename_does_not_change_the_slug()
    {
        var workspace = Workspace.Create("Acme", _ownerId, Now);

        workspace.Rename("Something Else Entirely");

        Assert.Equal("acme", workspace.Slug);
    }

    [Theory]
    [InlineData("Acme", "acme")]
    [InlineData("My Team", "my-team")]
    [InlineData("  DevHub -- Platform  ", "devhub-platform")]
    [InlineData("Café Élite", "cafe-elite")]
    [InlineData("Team #2 (Backend)", "team-2-backend")]
    public void The_slug_is_derived_from_the_name_as_lowercase_kebab(string name, string expected)
    {
        Assert.Equal(expected, Workspace.Create(name, _ownerId, Now).Slug);
    }

    [Fact]
    public void A_derived_slug_is_truncated_to_50_characters_without_a_trailing_hyphen()
    {
        // 49 letters, a space, then more: the cut at 50 lands right on the hyphen.
        var name = new string('a', 49) + " bcdef";

        var slug = Workspace.Create(name, _ownerId, Now).Slug;

        Assert.Equal(new string('a', 49), slug);
    }

    [Theory]
    [InlineData("AB")]
    [InlineData("🚀🚀🚀")]
    [InlineData("東京チーム")]
    public void A_name_that_yields_a_slug_under_3_characters_needs_an_explicit_slug(string name)
    {
        Assert.Throws<DomainException>(() => Workspace.Create(name, _ownerId, Now));

        Assert.Equal("tokyo-team", Workspace.Create(name, _ownerId, Now, slug: "tokyo-team").Slug);
    }

    [Fact]
    public void An_explicit_slug_is_used_as_given()
    {
        var workspace = Workspace.Create("Acme Corporation", _ownerId, Now, slug: "acme");

        Assert.Equal("acme", workspace.Slug);
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("Acme")]
    [InlineData("acme corp")]
    [InlineData("-acme")]
    [InlineData("acme-")]
    [InlineData("acme--corp")]
    [InlineData("acme_corp")]
    public void An_invalid_explicit_slug_is_rejected_not_rewritten(string slug)
    {
        Assert.Throws<DomainException>(() => Workspace.Create("Acme", _ownerId, Now, slug));
    }

    [Fact]
    public void An_explicit_slug_longer_than_50_characters_is_rejected()
    {
        Assert.Throws<DomainException>(() => Workspace.Create("Acme", _ownerId, Now, new string('a', 51)));
    }
}
