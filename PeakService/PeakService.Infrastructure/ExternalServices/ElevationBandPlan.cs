using PeakService.Application.Abstractions;

namespace PeakService.Infrastructure.ExternalServices;

internal static class ElevationBandPlan
{
    public static IReadOnlyList<ElevationBand> Build(IngestionOptions options, PeakSourceCursor cursor) =>
        cursor.IsIncremental ? [ElevationBand.Unbounded] : FullSweep(options);

    public static List<ElevationBand> FullSweep(IngestionOptions options)
    {
        double minimum = options.MinElevationMeters;
        double maximum = Math.Max(options.MaxElevationMeters, minimum);

        List<ElevationBand> bands = [new(From: null, To: minimum)];

        for (double from = minimum; from < maximum; from += options.ElevationBandMeters)
        {
            bands.Add(new ElevationBand(from, Math.Min(from + options.ElevationBandMeters, maximum)));
        }

        bands.Add(new ElevationBand(maximum, To: null));

        return bands;
    }
}
