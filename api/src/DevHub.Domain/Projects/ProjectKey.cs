using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using DevHub.Domain.Common;

namespace DevHub.Domain.Projects;

/// <summary>
/// The rules for a project key: the short code users see constantly in issue keys
/// (<c>DEV-42</c>). Always supplied by the caller — unlike a workspace slug, there is no
/// derivation from the name — and immutable once the project exists (domain-model.md).
/// </summary>
/// <remarks>
/// Uniqueness is <b>not</b> checked here: "no other project in this workspace has it" is a
/// question about every row in the table, which one aggregate cannot answer. The unique index on
/// <c>(workspace_id, key)</c> answers it, and the create handler (DEVHUB-031) turns the
/// violation into a 409.
/// </remarks>
public static partial class ProjectKey
{
    public const int MaxLength = 10;

    /// <summary>The non-throwing <see cref="Validate"/>, for input validation (DEVHUB-031).</summary>
    public static bool IsValid([NotNullWhen(true)] string? key) => key is not null && Format().IsMatch(key);

    public static void Validate(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (!Format().IsMatch(key))
        {
            throw new DomainException(
                "A project key must start with a letter, followed by 1-9 more uppercase letters " +
                "or digits (e.g. 'DEV').");
        }
    }

    // One uppercase letter, then 1-9 uppercase letters or digits: 2-10 characters total.
    [GeneratedRegex("^[A-Z][A-Z0-9]{1,9}$")]
    private static partial Regex Format();
}
