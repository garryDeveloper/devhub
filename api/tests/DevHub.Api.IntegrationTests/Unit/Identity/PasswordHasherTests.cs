using DevHub.Application.Common;
using DevHub.Infrastructure.Identity;

namespace DevHub.Api.IntegrationTests.Unit.Identity;

/// <summary>
/// DEVHUB-013's unit tests for the <see cref="IPasswordHasher"/> adapter.
/// </summary>
/// <remarks>
/// Pure — no database, no HTTP — same temporary home as <see cref="Unit.UserTests"/>; see its
/// remarks for why.
/// </remarks>
public sealed class PasswordHasherTests
{
    private readonly IPasswordHasher _hasher = new PasswordHasher();

    [Fact]
    public void Verify_returns_true_for_the_password_it_was_hashed_from()
    {
        var hash = _hasher.Hash("correct horse battery staple");

        Assert.True(_hasher.Verify(hash, "correct horse battery staple"));
    }

    [Fact]
    public void Verify_returns_false_for_the_wrong_password()
    {
        var hash = _hasher.Hash("correct horse battery staple");

        Assert.False(_hasher.Verify(hash, "wrong password"));
    }

    [Fact]
    public void Hash_salts_every_call_so_the_same_password_hashes_differently()
    {
        // The behaviour a per-user salt exists for: two users who happen to pick the same
        // password must not end up with the same row in the database.
        var first = _hasher.Hash("correct horse battery staple");
        var second = _hasher.Hash("correct horse battery staple");

        Assert.NotEqual(first, second);
    }
}
