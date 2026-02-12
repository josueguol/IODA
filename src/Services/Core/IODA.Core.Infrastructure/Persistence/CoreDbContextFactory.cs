using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace IODA.Core.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for EF Core migrations.
/// Used when running 'dotnet ef migrations add'.
/// Connection string can be overridden via environment variable ConnectionStrings__DefaultConnection.
/// </summary>
public class CoreDbContextFactory : IDesignTimeDbContextFactory<CoreDbContext>
{
    public CoreDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=ioda_core;Username=ioda;Password=ioda_dev_password";

        var optionsBuilder = new DbContextOptionsBuilder<CoreDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.MigrationsAssembly(typeof(CoreDbContext).Assembly.FullName);
        });

        return new CoreDbContext(optionsBuilder.Options);
    }
}
