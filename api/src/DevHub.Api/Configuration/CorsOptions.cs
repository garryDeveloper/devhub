namespace DevHub.Api.Configuration;

/// <summary>
/// Browser origins allowed to call the API. Per-environment: deployed environments supply their
/// own via <c>Cors__AllowedOrigins__0</c>; an empty list fails startup outside Development
/// (enforced in <c>ServiceCollectionExtensions.AddApiCors</c>, not by a data annotation, because
/// the rule depends on the hosting environment).
/// </summary>
public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; init; } = [];
}
