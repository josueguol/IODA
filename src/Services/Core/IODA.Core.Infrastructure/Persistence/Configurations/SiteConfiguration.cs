using IODA.Core.Domain.Entities;
using IODA.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IODA.Core.Infrastructure.Persistence.Configurations;

public class SiteConfiguration : IEntityTypeConfiguration<Site>
{
    public void Configure(EntityTypeBuilder<Site> builder)
    {
        builder.ToTable("sites");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        builder.Property(s => s.PublicId)
            .HasConversion(ValueObjectConverters.IdentifierConverter)
            .HasColumnName("public_id")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(s => s.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(s => s.EnvironmentId)
            .HasColumnName("environment_id");

        builder.Property(s => s.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.Domain)
            .HasColumnName("domain")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(s => s.Subdomain)
            .HasColumnName("subdomain")
            .HasMaxLength(255);

        builder.Property(s => s.Subpath)
            .HasColumnName("subpath")
            .HasMaxLength(500);

        builder.Property(s => s.ThemeId)
            .HasColumnName("theme_id")
            .HasMaxLength(200);

        builder.Property(s => s.UrlTemplate)
            .HasColumnName("url_template")
            .HasMaxLength(500);

        builder.Property(s => s.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(s => s.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.HasOne(s => s.Project)
            .WithMany()
            .HasForeignKey(s => s.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Environment)
            .WithMany()
            .HasForeignKey(s => s.EnvironmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(s => new { s.ProjectId, s.PublicId })
            .IsUnique()
            .HasDatabaseName("ix_sites_project_public_id");

        builder.HasIndex(s => new { s.Domain, s.Subdomain, s.Subpath })
            .HasDatabaseName("ix_sites_domain_lookup");

        builder.Ignore(s => s.DomainEvents);
    }
}
