using IODA.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IODA.Core.Infrastructure.Persistence.Configurations;

public class ContentVersionConfiguration : IEntityTypeConfiguration<ContentVersion>
{
    public void Configure(EntityTypeBuilder<ContentVersion> builder)
    {
        builder.ToTable("content_versions");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Id)
            .ValueGeneratedNever();

        builder.Property(v => v.ContentId)
            .HasColumnName("content_id")
            .IsRequired();

        builder.Property(v => v.VersionNumber)
            .HasColumnName("version_number")
            .IsRequired();

        builder.Property(v => v.Title)
            .HasColumnName("title")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(v => v.Fields)
            .HasConversion(new Converters.JsonbDictionaryConverter())
            .HasColumnName("fields")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(v => v.Status)
            .HasColumnName("status")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(v => v.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(v => v.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.Property(v => v.Comment)
            .HasColumnName("comment")
            .HasMaxLength(1000);

        builder.HasOne(v => v.Content)
            .WithMany(c => c.Versions)
            .HasForeignKey(v => v.ContentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(v => new { v.ContentId, v.VersionNumber })
            .IsUnique()
            .HasDatabaseName("ix_content_versions_content_version");

        builder.Ignore(v => v.DomainEvents);
    }
}
