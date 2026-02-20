using IODA.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IODA.Core.Infrastructure.Persistence.Configurations;

public class ContentSiteConfiguration : IEntityTypeConfiguration<ContentSite>
{
    public void Configure(EntityTypeBuilder<ContentSite> builder)
    {
        builder.ToTable("content_sites");

        builder.HasKey(cs => new { cs.ContentId, cs.SiteId });

        builder.Property(cs => cs.ContentId)
            .HasColumnName("content_id");

        builder.Property(cs => cs.SiteId)
            .HasColumnName("site_id");

        builder.HasOne(cs => cs.Content)
            .WithMany()
            .HasForeignKey(cs => cs.ContentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cs => cs.Site)
            .WithMany()
            .HasForeignKey(cs => cs.SiteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(cs => cs.SiteId)
            .HasDatabaseName("ix_content_sites_site_id");
    }
}
