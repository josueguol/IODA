using IODA.Core.Domain.Entities;
using IODA.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IODA.Core.Infrastructure.Persistence.Configurations;

public class ContentSchemaConfiguration : IEntityTypeConfiguration<ContentSchema>
{
    public void Configure(EntityTypeBuilder<ContentSchema> builder)
    {
        builder.ToTable("content_schemas");

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

        builder.Property(s => s.SchemaName)
            .HasColumnName("schema_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.SchemaType)
            .HasColumnName("schema_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(s => s.ParentSchemaId)
            .HasColumnName("parent_schema_id");

        builder.HasOne(s => s.ParentSchema)
            .WithMany()
            .HasForeignKey(s => s.ParentSchemaId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.Property(s => s.SchemaVersion)
            .HasColumnName("schema_version")
            .IsRequired();

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

        builder.HasMany(s => s.Fields)
            .WithOne(f => f.Schema)
            .HasForeignKey(f => f.SchemaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.Fields).HasField("_fields");

        builder.HasIndex(s => s.PublicId)
            .IsUnique()
            .HasDatabaseName("ix_content_schemas_public_id");

        builder.HasIndex(s => new { s.ProjectId, s.SchemaType })
            .IsUnique()
            .HasDatabaseName("ix_content_schemas_project_type");

        builder.Ignore(s => s.DomainEvents);
    }
}
