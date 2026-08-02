using PeakService.Application.Peaks.GetPeakById;
using PeakService.Domain.Peaks;

namespace PeakService.Application.Peaks.Mappings;

internal static class PeakMappings
{
    public static PeakDetailResponse ToDetailResponse(this Peak peak, string? rangeName) => new(
        peak.Id,
        peak.Name,
        peak.AltitudeMeters,
        peak.ProminenceMeters,
        peak.Coordinates.Latitude,
        peak.Coordinates.Longitude,
        peak.CountryCode,
        peak.Region,
        peak.ImageUrl,
        peak.RangeId,
        rangeName,
        [.. peak.AlternativeNames.Select(name => new PeakNameResponse(name.LanguageCode, name.Name, name.IsOfficial))]);
}
