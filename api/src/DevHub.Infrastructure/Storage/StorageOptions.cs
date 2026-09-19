using System.ComponentModel.DataAnnotations;

namespace DevHub.Infrastructure.Storage;

/// <summary>
/// S3-compatible storage configuration (storage-s3-spec.md §7-8). <see cref="AccessKey"/> and
/// <see cref="SecretKey"/> are for MinIO/local only — production reads the EC2 instance role
/// through the default credential chain and must never set them, so they are not
/// <c>[Required]</c> here. The Development-only requirement is enforced in
/// <c>DependencyInjection.AddConfigurationOptions</c>, the same way CorsOptions' environment
/// -conditional rule is: a data annotation cannot see the hosting environment.
/// </summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    [Required]
    public string BucketName { get; init; } = string.Empty;

    [Required]
    public string Region { get; init; } = "us-east-1";

    // MinIO endpoint override, e.g. http://localhost:9000. Null targets real AWS S3.
    public string? ServiceUrl { get; init; }

    // MinIO requires path-style requests (bucket.example.com vs example.com/bucket). Real S3
    // does not, so this stays false outside local development.
    public bool ForcePathStyle { get; init; }

    public string? AccessKey { get; init; }

    public string? SecretKey { get; init; }
}
