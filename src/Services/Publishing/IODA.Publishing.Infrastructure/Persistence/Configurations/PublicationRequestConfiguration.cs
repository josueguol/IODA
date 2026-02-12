using IODA.Publishing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IODA.Publishing.Infrastructure.Persistence.Configurations;

public class PublicationRequestConfiguration : IEntityTypeConfiguration<PublicationRequest>
{
    public void Configure(EntityTypeBuilder<PublicationRequest> builder)
    {
        builder.ToTable("publication_requests");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.ContentId).HasColumnName("content_id").IsRequired();
        builder.Property(p => p.ProjectId).HasColumnName("project_id").IsRequired();
        builder.Property(p => p.EnvironmentId).HasColumnName("environment_id").IsRequired();
        builder.Property(p => p.RequestedBy).HasColumnName("requested_by").IsRequired();
        builder.Property(p => p.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(p => p.RequestedAt).HasColumnName("requested_at").IsRequired();
        builder.Property(p => p.ResolvedAt).HasColumnName("resolved_at");
        builder.Property(p => p.ResolvedBy).HasColumnName("resolved_by");
        builder.Property(p => p.RejectionReason).HasColumnName("rejection_reason").HasMaxLength(2000);
        builder.Property(p => p.ValidationErrors).HasColumnName("validation_errors").HasMaxLength(4000);
        builder.HasIndex(p => new { p.ContentId, p.Status }).HasDatabaseName("ix_publication_requests_content_status");
        builder.Ignore(p => p.DomainEvents);
    }
}
