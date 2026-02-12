using IODA.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IODA.Identity.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.UserId)
            .HasColumnName("user_id")
            .IsRequired();
        builder.Property(r => r.Token)
            .HasColumnName("token")
            .HasMaxLength(500)
            .IsRequired();
        builder.Property(r => r.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();
        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
        builder.Property(r => r.RevokedAt)
            .HasColumnName("revoked_at");

        builder.HasIndex(r => r.Token).HasDatabaseName("ix_refresh_tokens_token");
        builder.HasIndex(r => r.UserId).HasDatabaseName("ix_refresh_tokens_user_id");
        builder.Ignore(r => r.IsRevoked);
        builder.Ignore(r => r.IsExpired);
        builder.Ignore(r => r.IsValid);
        builder.Ignore(r => r.DomainEvents);
    }
}
