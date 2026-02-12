using IODA.Core.Domain.Entities;
using IODA.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IODA.Core.Infrastructure.Persistence.Configurations;

public class MediaItemConfiguration : IEntityTypeConfiguration<MediaItem>
{
    public void Configure(EntityTypeBuilder<MediaItem> builder)
    {
        builder.ToTable("media_items");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .ValueGeneratedNever();

        builder.Property(m => m.PublicId)
            .HasConversion(ValueObjectConverters.IdentifierConverter)
            .HasColumnName("public_id")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(m => m.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(m => m.FileName)
            .HasColumnName("file_name")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(m => m.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(m => m.ContentType)
            .HasColumnName("content_type")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(m => m.SizeBytes)
            .HasColumnName("size_bytes")
            .IsRequired();

        builder.Property(m => m.StorageKey)
            .HasColumnName("storage_key")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(m => m.Version)
            .HasColumnName("version")
            .IsRequired();

        builder.Property(m => m.Metadata)
            .HasConversion(new NullableJsonbDictionaryConverter())
            .HasColumnName("metadata")
            .HasColumnType("jsonb");

        builder.Property(m => m.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(m => m.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.HasOne(m => m.Project)
            .WithMany()
            .HasForeignKey(m => m.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => new { m.ProjectId, m.StorageKey })
            .HasDatabaseName("ix_media_items_project_storage");

        builder.Ignore(m => m.DomainEvents);
    }
}
