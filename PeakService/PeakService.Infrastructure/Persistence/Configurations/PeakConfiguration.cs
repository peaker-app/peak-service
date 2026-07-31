using Common.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;
using PeakService.Domain.MountainRanges;
using PeakService.Domain.Peaks;

namespace PeakService.Infrastructure.Persistence.Configurations;

internal sealed class PeakConfiguration : EntityConfiguration<Peak>
{
    private const int CountryCodeLength = 2;
    private const string Wgs84GeographyPoint = "geography (Point,4326)";
    private const string PeakIdColumn = "peak_id";

    public override void Configure(EntityTypeBuilder<Peak> builder)
    {
        base.Configure(builder);

        builder.ToTable("peaks");

        ConfigureColumns(builder);
        ConfigureIndexesAndRelations(builder);
        ConfigureSearchVector(builder);
        ConfigureAlternativeNames(builder);
    }

    private static void ConfigureColumns(EntityTypeBuilder<Peak> builder)
    {
        builder.Property(peak => peak.WikidataId)
            .HasColumnName("wikidata_id").HasMaxLength(Peak.MaxWikidataIdLength).IsRequired();

        builder.Property(peak => peak.Name)
            .HasColumnName("name").HasMaxLength(Peak.MaxNameLength).IsRequired();

        builder.Property(peak => peak.AltitudeMeters).HasColumnName("altitude_m").IsRequired();
        builder.Property(peak => peak.ProminenceMeters).HasColumnName("prominence_m");

        builder.Property(peak => peak.Coordinates)
            .HasColumnName("location").HasColumnType(Wgs84GeographyPoint)
            .HasConversion<CoordinatesConverter>().IsRequired();

        builder.Property(peak => peak.CountryCode)
            .HasColumnName("country_code").HasMaxLength(CountryCodeLength).IsFixedLength();

        builder.Property(peak => peak.Region).HasColumnName("region").HasMaxLength(Peak.MaxRegionLength);

        builder.Property(peak => peak.SourceRevision)
            .HasColumnName("source_revision").HasMaxLength(Peak.MaxSourceRevisionLength);

        builder.Property(peak => peak.RangeId).HasColumnName("range_id");
    }

    private static void ConfigureIndexesAndRelations(EntityTypeBuilder<Peak> builder)
    {
        builder.HasIndex(peak => peak.WikidataId).IsUnique().HasDatabaseName("ux_peaks_wikidata");
        builder.HasIndex(peak => peak.AltitudeMeters).IsDescending().HasDatabaseName("ix_peaks_altitude");
        builder.HasIndex(peak => peak.CountryCode).HasDatabaseName("ix_peaks_country");
        builder.HasIndex(peak => peak.Coordinates).HasMethod("gist").HasDatabaseName("ix_peaks_location");

        builder.HasOne<MountainRange>()
            .WithMany()
            .HasForeignKey(peak => peak.RangeId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureSearchVector(EntityTypeBuilder<Peak> builder)
    {
        builder.Property<NpgsqlTsVector>("search_vector")
            .HasColumnName("search_vector")
            .HasComputedColumnSql("to_tsvector('simple', immutable_unaccent(name))", stored: true);

        builder.HasIndex("search_vector").HasMethod("gin").HasDatabaseName("ix_peaks_search");
    }

    private static void ConfigureAlternativeNames(EntityTypeBuilder<Peak> builder) =>
        builder.OwnsMany(peak => peak.AlternativeNames, name =>
        {
            name.ToTable("peak_names");
            name.WithOwner().HasForeignKey(PeakIdColumn);
            name.HasKey(entity => entity.Id);
            name.Property(entity => entity.Id).HasColumnName("id").ValueGeneratedNever();
            name.Property<Guid>(PeakIdColumn).HasColumnName(PeakIdColumn);
            name.Property(entity => entity.LanguageCode)
                .HasColumnName("language_code").HasMaxLength(PeakName.MaxLanguageCodeLength).IsRequired();
            name.Property(entity => entity.Name)
                .HasColumnName("name").HasMaxLength(PeakName.MaxNameLength).IsRequired();
            name.Property(entity => entity.IsOfficial).HasColumnName("is_official").IsRequired();
            name.HasIndex(PeakIdColumn, nameof(PeakName.LanguageCode), nameof(PeakName.Name))
                .IsUnique().HasDatabaseName("ux_peak_names_unique");
            name.HasIndex(PeakIdColumn).HasDatabaseName("ix_peak_names_peak");
        });
}
