using DevHub.Application.Common;
using DevHub.Application.Workspaces;
using DevHub.Application.Workspaces.Access;
using DevHub.Domain.Workspaces;

namespace DevHub.Api.IntegrationTests.Unit;

/// <summary>DEVHUB-026: scope before role — no access is a 404, a low role is a 403.</summary>
public sealed class WorkspaceAccessExtensionsTests
{
    private static readonly Error NotFound = Error.NotFound("things.not_found", "The thing was not found.");

    private static readonly WorkspaceAccess Owner = new(Guid.CreateVersion7(), null, WorkspaceRole.Owner);
    private static readonly WorkspaceAccess Member = new(Guid.CreateVersion7(), null, WorkspaceRole.Member);

    [Fact]
    public void RequireMember_turns_no_access_into_the_resources_own_404()
    {
        var result = ((WorkspaceAccess?)null).RequireMember(NotFound);

        Assert.Same(NotFound, result.Error);
    }

    [Fact]
    public void RequireMember_accepts_any_role()
    {
        Assert.Same(Member, Member.RequireMember(NotFound).Value);
        Assert.Same(Owner, Owner.RequireMember(NotFound).Value);
    }

    [Fact]
    public void RequireOwner_turns_no_access_into_404_never_403()
    {
        var result = ((WorkspaceAccess?)null).RequireOwner(NotFound);

        Assert.Same(NotFound, result.Error);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public void RequireOwner_turns_a_member_into_403()
    {
        var result = Member.RequireOwner(NotFound);

        Assert.Same(WorkspaceErrors.OwnerRequired, result.Error);
        Assert.Equal(ErrorType.Forbidden, result.Error!.Type);
    }

    [Fact]
    public void RequireOwner_accepts_an_owner()
    {
        Assert.Same(Owner, Owner.RequireOwner(NotFound).Value);
    }
}
