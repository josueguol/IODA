using IODA.Core.Domain.Entities;
using IODA.Core.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnvironmentEntity = IODA.Core.Domain.Entities.Environment;

namespace IODA.Core.Infrastructure.Persistence.Configurations;

public class EnvironmentConfiguration : IEntityTypeConfiguration<EnvironmentEntity>
{
    public void Configure(EntityTypeBuilder<EnvironmentEntity> builder)
    {
        builder.ToTable("environments");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(e => e.PublicId)
            .HasConversion(ValueObjectConverters.IdentifierConverter)
            .HasColumnName("public_id")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Slug)
            .HasConversion(ValueObjectConverters.SlugConverter)
            .HasColumnName("slug")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(2000);

        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasOne(e => e.Project)
            .WithMany()
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.ProjectId, e.Slug })
            .IsUnique()
            .HasDatabaseName("ix_environments_project_slug");

        builder.Ignore(e => e.DomainEvents);
    }
}
