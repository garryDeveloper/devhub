using System.Reflection;
using System.Xml.Linq;
using DevHub.Domain.Common;

namespace DevHub.ArchitectureTests;

/// <summary>
/// DevHub.Domain must contain business rules and nothing else — no EF Core, no ASP.NET, no
/// AWS SDK. That is what makes the domain testable without a database and portable if the
/// persistence choice ever changes.
/// </summary>
public class DomainPurityRules
{
    /// <summary>
    /// Assemblies the BCL itself provides. Everything else is a framework dependency.
    /// </summary>
    private static readonly string[] AllowedAssemblyPrefixes =
    [
        "System",
        "netstandard",
        "mscorlib",
        "DevHub.Domain",
    ];

    [Fact]
    public void Domain_assembly_references_only_the_base_class_library()
    {
        var referenced = typeof(Entity).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .Where(name => !AllowedAssemblyPrefixes.Any(prefix =>
                name.Equals(prefix, StringComparison.Ordinal) ||
                name.StartsWith(prefix + ".", StringComparison.Ordinal)))
            .ToList();

        Assert.True(
            referenced.Count == 0,
            $"DevHub.Domain must reference only the BCL, but it references: {string.Join(", ", referenced)}");
    }

    /// <summary>
    /// The stricter rule, and the one the DEVHUB-002 acceptance criteria name explicitly: the
    /// project file itself carries no NuGet dependency. The assembly-level test above only
    /// catches a package whose types are actually used; this one catches it the moment the
    /// &lt;PackageReference&gt; is added.
    /// </summary>
    [Fact]
    public void Domain_project_file_declares_no_package_reference()
    {
        var projectFile = Path.Combine(SolutionRoot(), "src", "DevHub.Domain", "DevHub.Domain.csproj");
        Assert.True(File.Exists(projectFile), $"Expected to find the Domain project at {projectFile}.");

        // Parsed as XML rather than grepped: a comment mentioning PackageReference is not a
        // PackageReference, and a false positive here would teach the wrong lesson.
        var packageReferences = XDocument.Load(projectFile)
            .Descendants("PackageReference")
            .Select(element => element.Attribute("Include")?.Value ?? "(unnamed)")
            .ToList();

        Assert.True(
            packageReferences.Count == 0,
            "DevHub.Domain.csproj must have no <PackageReference>. Found: " + string.Join(", ", packageReferences));
    }

    /// <summary>Walks up from the test binaries until it finds DevHub.sln.</summary>
    private static string SolutionRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DevHub.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException($"Could not find DevHub.sln above {AppContext.BaseDirectory}.");
    }
}
