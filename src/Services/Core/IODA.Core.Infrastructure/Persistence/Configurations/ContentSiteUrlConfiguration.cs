using IODA.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IODA.Core.Infrastructure.Persistence.Configurations;

public class ContentSiteUrlConfiguration : IEntityTypeConfiguration<ContentSiteUrl>
{
    public void Configure(EntityTypeBuilder<ContentSiteUrl> builder)
    {
        builder.ToTable("content_site_urls");

        builder.HasKey(x => new { x.ContentId, x.SiteId });

        builder.Property(x => x.ContentId)
            .HasColumnName("content_id");

        builder.Property(x => x.SiteId)
            .HasColumnName("site_id");

        builder.Property(x => x.Path)
            .HasColumnName("path")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasOne(x => x.Content)
            .WithMany()
            .HasForeignKey(x => x.ContentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Site)
            .WithMany()
            .HasForeignKey(x => x.SiteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.SiteId)
            .HasDatabaseName("ix_content_site_urls_site_id");

        // Unicidad de ruta publicada por sitio.
        builder.HasIndex(x => new { x.SiteId, x.Path })
            .HasDatabaseName("ix_content_site_urls_site_path")
            .IsUnique();
    }
}
