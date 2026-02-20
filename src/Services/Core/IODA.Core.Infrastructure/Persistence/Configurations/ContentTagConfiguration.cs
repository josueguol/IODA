using IODA.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IODA.Core.Infrastructure.Persistence.Configurations;

public class ContentTagConfiguration : IEntityTypeConfiguration<ContentTag>
{
    public void Configure(EntityTypeBuilder<ContentTag> builder)
    {
        builder.ToTable("content_tags");

        builder.HasKey(ct => new { ct.ContentId, ct.TagId });

        builder.Property(ct => ct.ContentId)
            .HasColumnName("content_id");

        builder.Property(ct => ct.TagId)
            .HasColumnName("tag_id");

        builder.HasOne(ct => ct.Content)
            .WithMany()
            .HasForeignKey(ct => ct.ContentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ct => ct.Tag)
            .WithMany()
            .HasForeignKey(ct => ct.TagId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(ct => ct.TagId)
            .HasDatabaseName("ix_content_tags_tag_id");
    }
}
