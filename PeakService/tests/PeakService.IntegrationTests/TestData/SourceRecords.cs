using PeakService.Application.Abstractions;
using PeakService.Domain.Peaks;

namespace PeakService.IntegrationTests.TestData;

internal static class SourceRecords
{
    public static PeakSourceRecord Valid(
        string wikidataId,
        string name,
        int altitudeMeters,
        double latitude,
        double longitude,
        string? countryCode = "ES",
        string? region = "Huesca",
        string? sourceRevision = null,
        IReadOnlyList<PeakNameDraft>? alternativeNames = null) =>
        new(
            wikidataId,
            name,
            altitudeMeters,
            ProminenceMeters: null,
            latitude,
            longitude,
            countryCode,
            region,
            sourceRevision)
        {
            AlternativeNames = alternativeNames ?? []
        };
}
