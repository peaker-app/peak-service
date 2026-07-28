using PeakService.Application.Abstractions;

namespace PeakService.Ingestion.UnitTests.TestData;

internal static class PeakSourceRecords
{
    public static PeakSourceRecord Valid(
        string wikidataId = "Q192580",
        string name = "Aneto",
        int altitudeMeters = 3404,
        double latitude = 42.6316,
        double longitude = 0.6577,
        string? countryCode = "ES",
        string? region = "Huesca",
        string? sourceRevision = null) =>
        new(
            wikidataId,
            name,
            altitudeMeters,
            ProminenceMeters: 2812,
            latitude,
            longitude,
            countryCode,
            region,
            sourceRevision);
}
