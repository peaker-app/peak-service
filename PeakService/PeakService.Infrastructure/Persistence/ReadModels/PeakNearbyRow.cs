namespace PeakService.Infrastructure.Persistence.ReadModels;

internal sealed record PeakNearbyRow(
    Guid Id,
    string Name,
    int AltitudeMeters,
    double Latitude,
    double Longitude,
    string? CountryCode,
    string? Region,
    string? ImageUrl,
    double DistanceMeters,
    long TotalCount);
