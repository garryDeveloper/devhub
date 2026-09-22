using System.Reflection;
using DevHub.Application.Common;

namespace DevHub.ArchitectureTests;

/// <summary>
/// DEVHUB-013's acceptance criterion: "No DTO ... can expose PasswordHash". Guards
/// <c>UserDto</c> (DEVHUB-014) and every DTO after it: the day one grows a <c>PasswordHash</c>
/// member this fails, instead of relying on review to catch it.
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
