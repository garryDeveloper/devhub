using DevHub.Domain.Users;
using DevHub.Domain.Workspaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevHub.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the <see cref="Workspace"/> aggregate — the <c>workspaces</c> row and its
/// <c>workspace_members</c> — per database-schema.md.
/// </summary>
internal sealed class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
{
    public void Configure(EntityTypeBuilder<Workspace> builder)
    {
        builder.HasKey(workspace => workspace.Id);

        builder.Property(workspace => workspace.Id)
            .ValueGeneratedNever();

        builder.Property(workspace => workspace.Name)
            .HasMaxLength(Workspace.NameMaxLength)
            .IsRequired();

        builder.Property(workspace => workspace.Slug)
            .HasMaxLength(WorkspaceSlug.MaxLength)
            .IsRequired();

        // The domain validates the format; only the database can guarantee "no other workspace
        // has it". DEVHUB-024 maps the violation to 409.
        builder.HasIndex(workspace => workspace.Slug)
            .IsUnique();

        // Restrict, not Cascade: deleting a user must not silently delete every workspace they
        // ever created — along with other people's projects and issues inside it.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(workspace => workspace.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(workspace => workspace.CreatedAt).IsRequired();
        builder.Property(workspace => workspace.UpdatedAt).IsRequired();

        // Members are reached only through the aggregate. EF reads and writes the private
        // _members field directly, since the public Members property is a read-only view.
        builder.HasMany(workspace => workspace.Members)
            .WithOne()
            .HasForeignKey(member => member.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(workspace => workspace.Members)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
