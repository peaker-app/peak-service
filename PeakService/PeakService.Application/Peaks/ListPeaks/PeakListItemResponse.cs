namespace PeakService.Application.Peaks.ListPeaks;

public sealed record PeakListItemResponse(
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
    string? ImageLicense);
