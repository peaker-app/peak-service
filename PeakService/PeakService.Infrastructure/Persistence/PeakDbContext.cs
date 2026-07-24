using Common.Application.Abstractions;
using Common.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using PeakService.Domain.MountainRanges;
using PeakService.Domain.PeakIngestionRuns;
using PeakService.Domain.Peaks;

namespace PeakService.Infrastructure.Persistence;

public sealed class PeakDbContext(DbContextOptions<PeakDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Peak> Peaks => Set<Peak>();

    public DbSet<MountainRange> MountainRanges => Set<MountainRange>();

    public DbSet<PeakIngestionRun> PeakIngestionRuns => Set<PeakIngestionRun>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("postgis");
        modelBuilder.HasPostgresExtension("unaccent");
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PeakDbContext).Assembly);
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
    }
}
