using DevHub.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevHub.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="RefreshToken"/> to <c>refresh_tokens</c> per database-schema.md §2.
/// </summary>
/// <remarks>
/// <c>created_by_ip</c> and <c>user_agent</c> are deliberately not mapped (decided in
/// DEVHUB-016): nothing reads them until session listing (post-MVP, auth-spec.md §8), and an IP
/// address is personal data we should not collect before there is a use for it. Adding two
/// nullable columns then is a trivial migration.
/// </remarks>
internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(token => token.Id);

        builder.Property(token => token.Id)
            .ValueGeneratedNever();

        // SHA-256 as lowercase hex: always exactly 64 characters.
        builder.Property(token => token.TokenHash)
            .HasMaxLength(64)
            .IsRequired();

        // Unique because refresh looks a token up by its hash (DEVHUB-016), and two rows with
        // the same hash would make "which session is this" ambiguous.
        builder.HasIndex(token => token.TokenHash)
            .IsUnique();

        builder.Property(token => token.ExpiresAt).IsRequired();
        builder.Property(token => token.CreatedAt).IsRequired();

        // Deleting a user ends every session they had.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // The rotation chain DEVHUB-016 walks for reuse detection. SetNull so purging old rows
        // (revoked or expired for 60+ days, per the schema spec) never trips over a link.
        builder.HasOne<RefreshToken>()
            .WithMany()
            .HasForeignKey(token => token.ReplacedByTokenId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(token => new { token.UserId, token.ExpiresAt });

        // Reuse detection revokes a whole family in one UPDATE ... WHERE family_id = @f.
        builder.Property(token => token.FamilyId).IsRequired();
        builder.HasIndex(token => token.FamilyId);

        // Optimistic concurrency on PostgreSQL's system column xmin — the id of the transaction
        // that last wrote the row. Two parallel refreshes with the same token both read it
        // active; both try UPDATE ... WHERE id = @id AND xmin = @read. The second waits on the
        // first's row lock, then finds xmin changed, matches nothing and fails, so exactly one
        // successor is ever issued. A shadow property: this is persistence plumbing, not
        // domain state, so the entity never sees it.
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
    }
}
