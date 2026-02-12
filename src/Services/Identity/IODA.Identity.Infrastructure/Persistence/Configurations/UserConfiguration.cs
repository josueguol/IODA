using IODA.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IODA.Identity.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(256)
            .IsRequired();
        builder.Property(u => u.NormalizedEmail)
            .HasColumnName("normalized_email")
            .HasMaxLength(256)
            .IsRequired();
        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(500)
            .IsRequired();
        builder.Property(u => u.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(200);
        builder.Property(u => u.IsActive)
            .HasColumnName("is_active")
            .IsRequired();
        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
        builder.Property(u => u.UpdatedAt)
            .HasColumnName("updated_at");
        builder.Property(u => u.LastLoginAt)
            .HasColumnName("last_login_at");

        builder.HasIndex(u => u.NormalizedEmail).IsUnique().HasDatabaseName("ix_users_normalized_email");
        builder.Ignore(u => u.DomainEvents);
    }
}
