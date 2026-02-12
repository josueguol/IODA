using IODA.Publishing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IODA.Publishing.Infrastructure.Persistence;

public class PublishingDbContext : DbContext
{
    public PublishingDbContext(DbContextOptions<PublishingDbContext> options)
        : base(options)
    {
    }

    public DbSet<PublicationRequest> PublicationRequests => Set<PublicationRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PublishingDbContext).Assembly);
    }
}
