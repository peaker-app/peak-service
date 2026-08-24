namespace PeakService.Infrastructure.Persistence.ReadModels;

internal sealed record PeakSearchRow(
    Guid Id,
    string Name,
    int AltitudeMeters,
    int? ProminenceMeters,
    double Latitude,
    double Longitude,
    string? CountryCode,
    string? Region,
    string? ImageUrl,
    string? ImageAuthor,
    string? ImageLicense,
    long TotalCount);
