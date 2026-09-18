using DevHub.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevHub.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="User"/> to <c>users</c> per docs/tech-specs/database-schema.md §2.
/// </summary>
/// <remarks>
/// Configuration lives here rather than in data annotations on the entity: annotations would put
/// an EF Core dependency in DevHub.Domain, which the architecture tests forbid outright.
/// </remarks>
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(user => user.Id);

        // uuid, application-generated (v7). No database default: the application must know the
        // id before the insert so it can raise events and return the resource without a
        // round-trip.
        builder.Property(user => user.Id)
            .ValueGeneratedNever();

        builder.Property(user => user.Email)
            .HasColumnType("citext")
            .IsRequired();

        // Unique by way of citext, so Ada@devhub.io and ada@devhub.io collide in the database
        // instead of becoming two accounts that argue about who owns the password.
        builder.HasIndex(user => user.Email)
            .IsUnique();

        builder.Property(user => user.DisplayName)
            .HasMaxLength(100)
            .IsRequired();

        // An S3 object key, nullable — a user without an avatar is normal, not an error.
        builder.Property(user => user.AvatarKey)
            .HasMaxLength(512);

        // text, not varchar(n): the hash format is an implementation detail of IPasswordHasher
        // (DEVHUB-013), and a length cap would turn a future algorithm change into a migration.
        builder.Property(user => user.PasswordHash)
            .IsRequired();

        // Column type comes from the global DateTimeOffset convention in DevHubDbContext.
        builder.Property(user => user.CreatedAt).IsRequired();
        builder.Property(user => user.UpdatedAt).IsRequired();
    }
}
