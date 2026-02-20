using IODA.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IODA.Core.Infrastructure.Persistence.Configurations;

public class ContentHierarchyConfiguration : IEntityTypeConfiguration<ContentHierarchy>
{
    public void Configure(EntityTypeBuilder<ContentHierarchy> builder)
    {
        builder.ToTable("content_hierarchies");

        builder.HasKey(ch => new { ch.ContentId, ch.HierarchyId });

        builder.Property(ch => ch.ContentId)
            .HasColumnName("content_id");

        builder.Property(ch => ch.HierarchyId)
            .HasColumnName("hierarchy_id");

        builder.HasOne(ch => ch.Content)
            .WithMany()
            .HasForeignKey(ch => ch.ContentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ch => ch.Hierarchy)
            .WithMany()
            .HasForeignKey(ch => ch.HierarchyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(ch => ch.HierarchyId)
            .HasDatabaseName("ix_content_hierarchies_hierarchy_id");
    }
}
