using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Persistence;

/// <summary>
/// Used by <c>dotnet ef</c> at design time. The connection string only has to be parseable --
/// migrations are generated from the model, not from a live database.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<JustThingsDbContext>
{
    public JustThingsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("JUSTTHINGS_DB")
            ?? "Host=localhost;Database=justthings;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<JustThingsDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(JustThingsDbContext).Assembly.FullName))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new JustThingsDbContext(options);
    }
}
