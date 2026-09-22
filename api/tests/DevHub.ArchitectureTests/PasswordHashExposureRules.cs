using System.Reflection;
using DevHub.Application.Common;

namespace DevHub.ArchitectureTests;

/// <summary>
/// DEVHUB-013's acceptance criterion: "No DTO ... can expose PasswordHash". No DTO exists yet —
/// <c>User</c>'s is DEVHUB-014/015's job — so this is a forward guard: it passes trivially today
/// and fails the day a DTO grows a <c>PasswordHash</c> member, instead of relying on review to
/// catch it.
/// </summary>
public class PasswordHashExposureRules
{
    private static readonly Assembly ApplicationAssembly = typeof(ICurrentUser).Assembly;
    private static readonly Assembly ApiAssembly = typeof(Program).Assembly;

    [Fact]
    public void No_Dto_type_exposes_a_PasswordHash_member()
    {
        var offenders = new[] { ApplicationAssembly, ApiAssembly }
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.Name.EndsWith("Dto", StringComparison.Ordinal))
            .Where(type =>
                type.GetProperty("PasswordHash", BindingFlags.Public | BindingFlags.Instance) is not null
                || type.GetField("PasswordHash", BindingFlags.Public | BindingFlags.Instance) is not null)
            .Select(type => type.FullName)
            .ToList();

        Assert.True(offenders.Count == 0, $"These DTOs expose PasswordHash: {string.Join(", ", offenders)}");
    }
}
