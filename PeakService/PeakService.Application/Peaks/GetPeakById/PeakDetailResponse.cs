namespace PeakService.Application.Peaks.GetPeakById;

public sealed record PeakDetailResponse(
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
    string? ImageLicenseUrl,
    string? ImageCreditUrl,
    Guid? RangeId,
    string? RangeName,
    IReadOnlyList<PeakNameResponse> AlternativeNames);
