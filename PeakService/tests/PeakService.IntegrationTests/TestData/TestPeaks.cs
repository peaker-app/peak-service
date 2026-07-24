using PeakService.Domain.Peaks;

namespace PeakService.IntegrationTests.TestData;

internal static class TestPeaks
{
    public static Peak Create(
        string name,
        double latitude,
        double longitude,
        int altitudeMeters = 3000,
        string? countryCode = "ES",
        string? region = "Huesca",
        Guid? rangeId = null,
        IReadOnlyList<PeakNameDraft>? alternativeNames = null) =>
        Peak.Create(new PeakDraft(
            new PeakSourceData(
                NewWikidataId(),
                name,
                altitudeMeters,
                ProminenceMeters: null,
                Coordinates.Create(latitude, longitude).Value,
                countryCode,
                region,
                SourceRevision: null),
            rangeId,
            alternativeNames ?? [])).Value;

    private static string NewWikidataId() => $"Q{Guid.NewGuid():N}"[..12];
}
