using PeakService.Domain.Peaks;

namespace PeakService.Application.Abstractions;

public sealed record PeakSourceRecord(
    string WikidataId,
    string Name,
    int AltitudeMeters,
    int? ProminenceMeters,
    double Latitude,
    double Longitude,
    string? CountryCode,
    string? Region,
    string? SourceRevision,
    string? ImageUrl = null)
{
    public IReadOnlyList<PeakNameDraft> AlternativeNames { get; init; } = [];
}
