using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PeakService.Infrastructure.Persistence;

internal sealed class PeakDbContextFactory : IDesignTimeDbContextFactory<PeakDbContext>
{
    private const string DesignTimeConnectionString =
        "Server=localhost;Port=5433;Database=peaker_peaks;Username=peaker;Password=peaker";

    public PeakDbContext CreateDbContext(string[] args)
    {
        DbContextOptions<PeakDbContext> options = new DbContextOptionsBuilder<PeakDbContext>()
            .UseNpgsql(DesignTimeConnectionString, npgsql => npgsql.UseNetTopologySuite())
            .Options;

        return new PeakDbContext(options);
    }
}
