using IODA.Authorization.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IODA.Authorization.Infrastructure.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.Name).HasColumnName("name").HasMaxLength(128).IsRequired();
        builder.Property(r => r.Description).HasColumnName("description").HasMaxLength(500).IsRequired();
        builder.HasIndex(r => r.Name).IsUnique().HasDatabaseName("ix_roles_name");
        builder.Ignore(r => r.DomainEvents);

        builder.HasMany<RolePermission>()
            .WithOne()
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(r => r.RolePermissions).HasField("_rolePermissions").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
