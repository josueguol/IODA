using IODA.Core.Domain.Entities;
using IODA.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IODA.Core.Infrastructure.Persistence.Configurations;

public class ContentConfiguration : IEntityTypeConfiguration<Content>
{
    public void Configure(EntityTypeBuilder<Content> builder)
    {
        builder.ToTable("contents");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedNever();

        builder.Property(c => c.PublicId)
            .HasConversion(ValueObjectConverters.IdentifierConverter)
            .HasColumnName("public_id")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(c => c.EnvironmentId)
            .HasColumnName("environment_id")
            .IsRequired();

        builder.Property(c => c.SiteId)
            .HasColumnName("site_id");

        builder.Property(c => c.ParentContentId)
            .HasColumnName("parent_content_id");

        builder.Property(c => c.SchemaId)
            .HasColumnName("schema_id")
            .IsRequired();

        builder.Property(c => c.Title)
            .HasColumnName("title")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(c => c.Slug)
            .HasConversion(ValueObjectConverters.SlugConverter)
            .HasColumnName("slug")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Status)
            .HasConversion(ValueObjectConverters.ContentStatusConverter)
            .HasColumnName("status")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.ContentType)
            .HasColumnName("content_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Fields)
            .HasConversion(new JsonbDictionaryConverter())
            .HasColumnName("fields")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(c => c.CurrentVersion)
            .HasColumnName("current_version")
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(c => c.PublishedAt)
            .HasColumnName("published_at");

        builder.Property(c => c.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.Property(c => c.UpdatedBy)
            .HasColumnName("updated_by");

        builder.Property(c => c.PublishedBy)
            .HasColumnName("published_by");

        builder.HasOne(c => c.Project)
            .WithMany()
            .HasForeignKey(c => c.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Environment)
            .WithMany()
            .HasForeignKey(c => c.EnvironmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Site)
            .WithMany()
            .HasForeignKey(c => c.SiteId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(c => c.Parent)
            .WithMany()
            .HasForeignKey(c => c.ParentContentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Schema)
            .WithMany()
            .HasForeignKey(c => c.SchemaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Versions)
            .WithOne(v => v.Content)
            .HasForeignKey(v => v.ContentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(c => c.Versions).HasField("_versions");

        builder.HasIndex(c => c.PublicId)
            .IsUnique()
            .HasDatabaseName("ix_contents_public_id");

        builder.HasIndex(c => new { c.ProjectId, c.EnvironmentId, c.Slug })
            .IsUnique()
            .HasDatabaseName("ix_contents_project_env_slug");

        builder.HasIndex(c => new { c.ProjectId, c.EnvironmentId, c.Status })
            .HasDatabaseName("ix_contents_project_env_status");

        builder.HasIndex(c => new { c.ProjectId, c.SiteId })
            .HasDatabaseName("ix_contents_project_site");

        builder.Ignore(c => c.DomainEvents);
    }
}
