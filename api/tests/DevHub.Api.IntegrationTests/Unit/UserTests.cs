using DevHub.Domain.Users;

namespace DevHub.Api.IntegrationTests.Unit;

/// <summary>
/// DEVHUB-013's unit tests for <see cref="User.Create"/>: email normalization and its guard
/// clauses.
/// </summary>
/// <remarks>
/// Pure — no database, no HTTP — so this belongs in <c>DevHub.Domain.UnitTests</c> per
/// testing-strategy.md §2. That project doesn't exist yet; DEVHUB-011 creates it. It lives here,
/// alongside <see cref="Identity.PasswordHasherTests"/>, only until then and should move
/// verbatim once it does.
/// </remarks>
public sealed class UserTests
{
    [Fact]
    public void Create_normalizes_email_to_lowercase_and_trims_surrounding_whitespace()
    {
        var user = User.Create("  Ada@DevHub.io  ", "Ada", "hash");

        Assert.Equal("ada@devhub.io", user.Email);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_a_missing_email(string? email)
    {
        // ThrowIfNullOrWhiteSpace throws ArgumentNullException specifically for null and
        // ArgumentException for empty/whitespace — both are ArgumentException, which is the
        // contract this guard clause promises, so ThrowsAny is the correct assertion here,
        // not an exact-type Throws<ArgumentException>.
        Assert.ThrowsAny<ArgumentException>(() => User.Create(email!, "Ada", "hash"));
    }

    [Fact]
    public void Create_assigns_a_version7_id()
    {
        // Index locality (CLAUDE.md §4) is the whole point of preferring v7 — a test that only
        // checked "Id is not empty" would pass for a v4 id too.
        var user = User.Create("ada@devhub.io", "Ada", "hash");

        Assert.Equal(7, user.Id.Version);
    }
}
