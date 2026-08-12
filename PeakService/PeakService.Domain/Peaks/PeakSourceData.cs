namespace PeakService.Domain.Peaks;

public sealed record PeakSourceData(
    string WikidataId,
    string Name,
    int AltitudeMeters,
    int? ProminenceMeters,
    Coordinates Coordinates,
    string? CountryCode,
    string? Region,
    string? SourceRevision,
    string? ImageUrl = null)
{
    public PeakImageAttribution Attribution { get; init; } = PeakImageAttribution.None;
}
