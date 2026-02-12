using IODA.Authorization.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IODA.Authorization.Infrastructure.Persistence.Configurations;

public class AccessRuleConfiguration : IEntityTypeConfiguration<AccessRule>
{
    public void Configure(EntityTypeBuilder<AccessRule> builder)
    {
        builder.ToTable("access_rules");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(a => a.RoleId).HasColumnName("role_id").IsRequired();
        builder.Property(a => a.ProjectId).HasColumnName("project_id");
        builder.Property(a => a.EnvironmentId).HasColumnName("environment_id");
        builder.Property(a => a.SchemaId).HasColumnName("schema_id");
        builder.Property(a => a.ContentStatus).HasColumnName("content_status").HasMaxLength(64);
        builder.HasIndex(a => new { a.UserId, a.RoleId, a.ProjectId, a.EnvironmentId, a.SchemaId, a.ContentStatus })
            .HasDatabaseName("ix_access_rules_user_role_scope");
        builder.Ignore(a => a.DomainEvents);
    }
}
