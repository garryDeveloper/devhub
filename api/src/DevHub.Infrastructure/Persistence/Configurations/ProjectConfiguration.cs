using DevHub.Domain.Projects;
using DevHub.Domain.Workspaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevHub.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the <see cref="Project"/> aggregate — the <c>projects</c> row and its
/// <c>project_members</c> — per database-schema.md.
/// </summary>
internal sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        // The domain validates the key format too (ProjectKey); the CHECK is the database's own
        // guarantee against a row written outside the aggregate — a manual fix, a bad migration.
        builder.ToTable("projects", table => table.HasCheckConstraint(
            "ck_projects_key_format",
            "key ~ '^[A-Z][A-Z0-9]{1,9}$'"));

        builder.HasKey(project => project.Id);

        builder.Property(project => project.Id)
            .ValueGeneratedNever();

        builder.Property(project => project.WorkspaceId)
            .IsRequired();

        builder.Property(project => project.Name)
            .HasMaxLength(Project.NameMaxLength)
            .IsRequired();

        builder.Property(project => project.Key)
            .HasMaxLength(ProjectKey.MaxLength)
            .IsRequired();

        builder.Property(project => project.Description)
            .HasMaxLength(Project.DescriptionMaxLength);

        builder.Property(project => project.Color)
            .HasMaxLength(Project.ColorMaxLength);

        builder.Property(project => project.Icon)
            .HasMaxLength(Project.IconMaxLength);

        builder.Property(project => project.IssueSequence)
            .IsRequired();

        builder.Property(project => project.CreatedAt).IsRequired();
        builder.Property(project => project.UpdatedAt).IsRequired();

        // The domain validates uniqueness is impossible to check on one aggregate; this is what
        // actually enforces "two projects in the same workspace cannot share a key".
        builder.HasIndex(project => new { project.WorkspaceId, project.Key })
            .IsUnique();

        // Every list query (DEVHUB-031) filters out archived projects by default, so the index
        // should only cover the rows that query actually reads.
        builder.HasIndex(project => project.WorkspaceId)
            .HasFilter("archived_at IS NULL");

        // Cascade: a workspace never outlives its projects, and nothing recovers a project whose
        // workspace is gone.
        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(project => project.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Members are reached only through the aggregate. EF reads and writes the private
        // _members field directly, since the public Members property is a read-only view.
        builder.HasMany(project => project.Members)
            .WithOne()
            .HasForeignKey(member => member.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(project => project.Members)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
