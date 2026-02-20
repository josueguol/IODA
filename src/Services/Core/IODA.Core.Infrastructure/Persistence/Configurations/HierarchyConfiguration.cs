using IODA.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IODA.Core.Infrastructure.Persistence.Configurations;

public class HierarchyConfiguration : IEntityTypeConfiguration<Hierarchy>
{
    public void Configure(EntityTypeBuilder<Hierarchy> builder)
    {
        builder.ToTable("hierarchies");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Id)
            .ValueGeneratedNever();

        builder.Property(h => h.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(h => h.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(h => h.Slug)
            .HasColumnName("slug")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(h => h.Description)
            .HasColumnName("description")
            .HasMaxLength(2000);

        builder.Property(h => h.ImageUrl)
            .HasColumnName("image_url")
            .HasMaxLength(500);

        builder.Property(h => h.ParentHierarchyId)
            .HasColumnName("parent_hierarchy_id");

        builder.HasOne(h => h.Project)
            .WithMany()
            .HasForeignKey(h => h.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.Parent)
            .WithMany()
            .HasForeignKey(h => h.ParentHierarchyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(h => new { h.ProjectId, h.Slug })
            .IsUnique()
            .HasDatabaseName("ix_hierarchies_project_slug");
        builder.HasIndex(h => h.ParentHierarchyId)
            .HasDatabaseName("ix_hierarchies_parent_id");
    }
}
