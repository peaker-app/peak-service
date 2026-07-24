using Common.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeakService.Domain.PeakIngestionRuns;

namespace PeakService.Infrastructure.Persistence.Configurations;

internal sealed class PeakIngestionRunConfiguration : EntityConfiguration<PeakIngestionRun>
{
    private const int StatusLength = 20;

    public override void Configure(EntityTypeBuilder<PeakIngestionRun> builder)
    {
        base.Configure(builder);

        builder.ToTable("peak_ingestion_runs");

        builder.Property(run => run.StartedAtUtc).HasColumnName("started_at_utc").IsRequired();
        builder.Property(run => run.FinishedAtUtc).HasColumnName("finished_at_utc");
        builder.Property(run => run.PeaksCreated).HasColumnName("peaks_created").IsRequired();
        builder.Property(run => run.PeaksUpdated).HasColumnName("peaks_updated").IsRequired();
        builder.Property(run => run.PeaksFailed).HasColumnName("peaks_failed").IsRequired();
        builder.Property(run => run.Error).HasColumnName("error");

        builder.Property(run => run.Status)
            .HasColumnName("status")
            .HasMaxLength(StatusLength)
            .HasConversion<string>()
            .IsRequired();

        builder.Ignore(run => run.IsRunning);

        builder.HasIndex(run => new { run.Status, run.StartedAtUtc })
            .HasDatabaseName("ix_peak_ingestion_runs_status_started");
    }
}
