using DevHub.Domain.Common;
using DevHub.Domain.Users;

namespace DevHub.Api.IntegrationTests.Unit;

/// <summary>
/// DEVHUB-016's rules on <see cref="RefreshToken"/>: families, activity and rotation.
/// </summary>
/// <remarks>
/// Pure, so it belongs in <c>DevHub.Domain.UnitTests</c> once DEVHUB-011 creates it — same
/// arrangement as <see cref="UserTests"/>.
/// </remarks>
public sealed class RefreshTokenTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);

    [Fact]
    public void Issue_starts_a_new_family_named_after_the_first_token()
    {
        var token = RefreshToken.Issue(Guid.NewGuid(), "hash-0", Now, Lifetime);

        Assert.Equal(token.Id, token.FamilyId);
        Assert.Equal(Now + Lifetime, token.ExpiresAt);
        Assert.True(token.IsActive(Now));
    }

    [Fact]
    public void A_token_is_inactive_from_the_instant_it_expires()
    {
        var token = RefreshToken.Issue(Guid.NewGuid(), "hash-0", Now, Lifetime);

        Assert.True(token.IsActive(Now + Lifetime - TimeSpan.FromTicks(1)));
        Assert.False(token.IsActive(Now + Lifetime));
    }

    [Fact]
    public void Rotate_revokes_the_token_and_links_a_successor_in_the_same_family()
    {
        var userId = Guid.NewGuid();
        var original = RefreshToken.Issue(userId, "hash-0", Now, Lifetime);
        var later = Now + TimeSpan.FromMinutes(15);

        var successor = original.Rotate("hash-1", later, Lifetime);

        Assert.Equal(later, original.RevokedAt);
        Assert.Equal(successor.Id, original.ReplacedByTokenId);
        Assert.False(original.IsActive(later));

        Assert.NotEqual(original.Id, successor.Id);
        Assert.Equal(original.FamilyId, successor.FamilyId);
        Assert.Equal(userId, successor.UserId);
        Assert.Equal("hash-1", successor.TokenHash);
        Assert.Equal(later + Lifetime, successor.ExpiresAt);
        Assert.True(successor.IsActive(later));
    }

    [Fact]
    public void A_rotated_token_cannot_be_rotated_again()
    {
        var original = RefreshToken.Issue(Guid.NewGuid(), "hash-0", Now, Lifetime);
        original.Rotate("hash-1", Now, Lifetime);

        Assert.Throws<DomainException>(() => original.Rotate("hash-2", Now, Lifetime));
    }

    [Fact]
    public void An_expired_token_cannot_be_rotated()
    {
        var token = RefreshToken.Issue(Guid.NewGuid(), "hash-0", Now, Lifetime);

        Assert.Throws<DomainException>(() => token.Rotate("hash-1", Now + Lifetime, Lifetime));
    }
}
