namespace PeakService.Application.Peaks.NearbyPeaks;

public sealed record NearbyPeakResponse(
    Guid Id,
    string Name,
    int AltitudeMeters,
    double Latitude,
    double Longitude,
    string? CountryCode,
    string? Region,
    double DistanceMeters);
