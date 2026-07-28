using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PeakService.Infrastructure.Persistence;

internal sealed class PeakDbContextFactory : IDesignTimeDbContextFactory<PeakDbContext>
{
    private const string ConnectionStringVariable = "ConnectionStrings__PeakDatabase";

    private const string ModelOnlyConnectionString =
        "Server=localhost;Port=5433;Database=peaker_peaks";

    public PeakDbContext CreateDbContext(string[] args)
    {
        DbContextOptions<PeakDbContext> options = new DbContextOptionsBuilder<PeakDbContext>()
            .UseNpgsql(ResolveConnectionString(), npgsql => npgsql.UseNetTopologySuite())
            .Options;

        return new PeakDbContext(options);
    }

    private static string ResolveConnectionString()
    {
        string? configured = Environment.GetEnvironmentVariable(ConnectionStringVariable);

        return string.IsNullOrWhiteSpace(configured)
            ? ModelOnlyConnectionString
            : configured;
    }
}
