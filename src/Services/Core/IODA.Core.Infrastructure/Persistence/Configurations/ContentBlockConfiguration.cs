using IODA.Core.Domain.Entities;
using IODA.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IODA.Core.Infrastructure.Persistence.Configurations;

public class ContentBlockConfiguration : IEntityTypeConfiguration<ContentBlock>
{
    public void Configure(EntityTypeBuilder<ContentBlock> builder)
    {
        builder.ToTable("content_blocks");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .ValueGeneratedNever();

        builder.Property(b => b.ContentId)
            .HasColumnName("content_id")
            .IsRequired();

        builder.Property(b => b.BlockType)
            .HasColumnName("block_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(b => b.Order)
            .HasColumnName("order")
            .IsRequired();

        builder.Property(b => b.Payload)
            .HasConversion(new JsonbDictionaryConverter())
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.HasIndex(b => new { b.ContentId, b.Order })
            .HasDatabaseName("ix_content_blocks_content_id_order");
    }
}
