using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace IODA.Publishing.Infrastructure.Persistence;

public class PublishingDbContextFactory : IDesignTimeDbContextFactory<PublishingDbContext>
{
    public PublishingDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Database=ioda_publishing;Username=postgres;Password=postgres;Include Error Detail=true";

        var optionsBuilder = new DbContextOptionsBuilder<PublishingDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.MigrationsAssembly(typeof(PublishingDbContext).Assembly.FullName);
        });

        return new PublishingDbContext(optionsBuilder.Options);
    }
}
