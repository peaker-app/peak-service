using PeakService.Domain.Peaks;

namespace PeakService.Domain.UnitTests.TestData;

internal static class PeakDrafts
{
    public static PeakDraft Valid(
        string wikidataId = "Q192580",
        string name = "Aneto",
        int altitudeMeters = 3404,
        int? prominenceMeters = 2812,
        string? countryCode = "ES",
        string? region = "Huesca",
        string? sourceRevision = null,
        Guid? rangeId = null,
        IReadOnlyList<PeakNameDraft>? alternativeNames = null,
        string? imageUrl = null,
        PeakImageAttribution? attribution = null) =>
        new(
            Source(
                wikidataId,
                name,
                altitudeMeters,
                prominenceMeters,
                countryCode,
                region,
                sourceRevision,
                imageUrl: imageUrl,
                attribution: attribution),
            rangeId,
            alternativeNames ?? []);

    public static PeakSourceData Source(
        string wikidataId = "Q192580",
        string name = "Aneto",
        int altitudeMeters = 3404,
        int? prominenceMeters = 2812,
        string? countryCode = "ES",
        string? region = "Huesca",
        string? sourceRevision = null,
        double latitude = 42.6316,
        double longitude = 0.6577,
        string? imageUrl = null,
        PeakImageAttribution? attribution = null) =>
        new(
            wikidataId,
            name,
            altitudeMeters,
            prominenceMeters,
            Coordinates.Create(latitude, longitude).Value,
            countryCode,
            region,
            sourceRevision,
            imageUrl)
        {
            Attribution = attribution ?? PeakImageAttribution.None
        };
}
