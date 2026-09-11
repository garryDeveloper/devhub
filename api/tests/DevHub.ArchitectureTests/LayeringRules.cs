using System.Reflection;
using DevHub.Application.Common;
using DevHub.Domain.Common;
using NetArchTest.Rules;

namespace DevHub.ArchitectureTests;

/// <summary>
/// The dependency direction from docs/tech-specs/backend-architecture.md §2:
/// <code>
/// Api            → Application → Domain
/// Infrastructure → Application + Domain
/// </code>
/// These are rules a compiler cannot enforce — nothing stops someone adding a project
/// reference — so they are enforced here and run in CI.
/// </summary>
public class LayeringRules
{
    private static readonly Assembly DomainAssembly = typeof(Entity).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(ICurrentUser).Assembly;

    private const string ApplicationNamespace = "DevHub.Application";
    private const string InfrastructureNamespace = "DevHub.Infrastructure";
    private const string ApiNamespace = "DevHub.Api";

    [Fact]
    public void Domain_should_not_depend_on_any_other_layer()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace, ApiNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result, "DevHub.Domain is the innermost layer and must depend on nothing"));
    }

    [Fact]
    public void Application_should_not_depend_on_infrastructure_or_api()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNamespace, ApiNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result, "DevHub.Application defines the ports; Infrastructure implements them, never the other way round"));
    }

    /// <summary>
    /// A "ShouldNot have dependency" rule passes both when nothing violates it and when the
    /// engine cannot see the assembly at all. This test tells the two apart, so the rules above
    /// can never go quietly vacuous after a rename or a retarget.
    /// </summary>
    [Theory]
    [InlineData("DevHub.Domain")]
    [InlineData("DevHub.Application")]
    [InlineData("DevHub.Infrastructure")]
    [InlineData("DevHub.Api")]
    public void Rule_engine_can_see_every_layer(string assemblyName)
    {
        var assembly = Assembly.Load(assemblyName);
        var typeCount = Types.InAssembly(assembly).GetTypes().Count();

        Assert.True(typeCount > 0, $"NetArchTest found no types in {assemblyName}; the layering rules above would pass for the wrong reason.");
    }

    private static string Describe(TestResult result, string rule)
    {
        var offenders = result.FailingTypeNames ?? [];
        return $"{rule}. Offending types: {string.Join(", ", offenders)}";
    }
}
