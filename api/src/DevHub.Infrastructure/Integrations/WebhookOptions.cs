using System.ComponentModel.DataAnnotations;

namespace DevHub.Infrastructure.Integrations;

/// <summary>
/// Shared secret used to validate inbound webhook signatures (webhooks-spec.md §3, §7). One
/// configured secret is the local-development shape; per-project secrets stored encrypted in the
/// database are a later ticket, not this one.
/// </summary>
public sealed class WebhookOptions
{
    public const string SectionName = "Webhooks";

    [Required]
    [MinLength(32)]
    public string DefaultSecret { get; init; } = string.Empty;
}
