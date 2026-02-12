using IODA.Core.Domain.Entities;
using IODA.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IODA.Core.Infrastructure.Persistence.Configurations;

public class FieldDefinitionConfiguration : IEntityTypeConfiguration<FieldDefinition>
{
    public void Configure(EntityTypeBuilder<FieldDefinition> builder)
    {
        builder.ToTable("field_definitions");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
            .ValueGeneratedNever();

        builder.Property(f => f.SchemaId)
            .HasColumnName("schema_id")
            .IsRequired();

        builder.Property(f => f.FieldName)
            .HasColumnName("field_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(f => f.FieldType)
            .HasColumnName("field_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(f => f.IsRequired)
            .HasColumnName("is_required")
            .IsRequired();

        builder.Property(f => f.DefaultValue)
            .HasConversion(new JsonObjectConverter())
            .HasColumnName("default_value")
            .HasColumnType("jsonb");

        builder.Property(f => f.HelpText)
            .HasColumnName("help_text")
            .HasMaxLength(500);

        builder.Property(f => f.ValidationRules)
            .HasConversion(new NullableJsonbDictionaryConverter())
            .HasColumnName("validation_rules")
            .HasColumnType("jsonb");

        builder.Property(f => f.DisplayOrder)
            .HasColumnName("display_order")
            .IsRequired();

        builder.HasOne(f => f.Schema)
            .WithMany(s => s.Fields)
            .HasForeignKey(f => f.SchemaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(f => new { f.SchemaId, f.FieldName })
            .IsUnique()
            .HasDatabaseName("ix_field_definitions_schema_name");

        builder.Ignore(f => f.DomainEvents);
    }
}
