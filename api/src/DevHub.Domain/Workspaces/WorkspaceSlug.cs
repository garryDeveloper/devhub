using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using DevHub.Domain.Common;

namespace DevHub.Domain.Workspaces;

/// <summary>
/// The rules for a workspace slug: lowercase kebab, 3–50 characters. The slug is the workspace's
/// address in every URL (<c>/w/{slug}</c>), which is why it is immutable once created.
/// </summary>
/// <remarks>
/// Uniqueness is <b>not</b> checked here: "no other workspace has this slug" is a question about
/// every row in the table, which one aggregate cannot answer. The unique index answers it, and
/// the create handler (DEVHUB-024) turns the violation into a 409.
/// </remarks>
public static partial class WorkspaceSlug
{
    public const int MinLength = 3;
    public const int MaxLength = 50;

    /// <summary>
    /// Derives a slug from a workspace name: "Café Élite — Team 2" becomes "cafe-elite-team-2".
    /// Throws when nothing usable is left (a name made only of emoji or non-Latin script), so the
    /// caller must supply a slug explicitly.
    /// </summary>
    public static string FromName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        // FormD splits "é" into "e" + a combining accent; dropping the accents keeps "cafe"
        // instead of turning "Café" into "caf".
        var decomposed = name.Normalize(NormalizationForm.FormD);
        var withoutAccents = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                withoutAccents.Append(character);
            }
        }

        var slug = NonAlphanumericRun().Replace(withoutAccents.ToString().ToLowerInvariant(), "-").Trim('-');

        // Truncating can cut right after a separator ("my-very-long-…-"), so trim again.
        if (slug.Length > MaxLength)
        {
            slug = slug[..MaxLength].TrimEnd('-');
        }

        Validate(slug);
        return slug;
    }

    /// <summary>Checks an explicitly supplied slug. It is used as-is, never silently rewritten.</summary>
    public static void Validate(string slug)
    {
        ArgumentNullException.ThrowIfNull(slug);

        if (slug.Length is < MinLength or > MaxLength)
        {
            throw new DomainException($"A workspace slug must be between {MinLength} and {MaxLength} characters.");
        }

        if (!KebabCase().IsMatch(slug))
        {
            throw new DomainException(
                "A workspace slug may contain only lowercase letters, digits and single hyphens between them.");
        }
    }

    // Anything that is not a-z or 0-9 — including runs of several — collapses to one hyphen.
    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlphanumericRun();

    // "acme", "acme-2", "my-team". Not "-acme", "acme-", "my--team" or "Acme".
    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private static partial Regex KebabCase();
}
