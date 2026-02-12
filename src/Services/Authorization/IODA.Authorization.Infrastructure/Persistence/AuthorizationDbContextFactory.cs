using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace IODA.Authorization.Infrastructure.Persistence;

public class AuthorizationDbContextFactory : IDesignTimeDbContextFactory<AuthorizationDbContext>
{
    public AuthorizationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Database=ioda_authorization;Username=postgres;Password=postgres;Include Error Detail=true";

        var optionsBuilder = new DbContextOptionsBuilder<AuthorizationDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.MigrationsAssembly(typeof(AuthorizationDbContext).Assembly.FullName);
        });

        return new AuthorizationDbContext(optionsBuilder.Options);
    }
}
