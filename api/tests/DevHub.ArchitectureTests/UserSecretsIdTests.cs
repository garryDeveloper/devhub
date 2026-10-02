using System.Reflection;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace DevHub.ArchitectureTests;

/// <summary>
/// DEVHUB-113: the API and the design-time factory (<c>dotnet ef</c>) must read the same
/// user-secrets store, or migrations can be applied to a different database than the one the API
/// uses. The id is declared in both .csproj files; this is what keeps them from drifting apart.
/// </summary>
public class UserSecretsIdTests
{
    [Fact]
    public void Api_and_Infrastructure_declare_the_same_user_secrets_id()
    {
        var api = UserSecretsIdOf(typeof(Program).Assembly);
        var infrastructure = UserSecretsIdOf(typeof(Infrastructure.DependencyInjection).Assembly);

        Assert.False(string.IsNullOrWhiteSpace(api), "DevHub.Api declares no UserSecretsId.");
        Assert.Equal(api, infrastructure);
    }

    private static string? UserSecretsIdOf(Assembly assembly) =>
        assembly.GetCustomAttribute<UserSecretsIdAttribute>()?.UserSecretsId;
}
