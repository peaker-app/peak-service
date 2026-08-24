using Common.Domain.Results;
using PeakService.Domain.Peaks;

namespace PeakService.Application.Peaks;

public sealed record PeakFilter(
    string? CountryCode,
    string? Region,
    int? MinAltitudeMeters,
    int? MaxAltitudeMeters)
{
    private const int CountryCodeLength = 2;

    public static readonly PeakFilter None = new(null, null, null, null);

    public Result Validate()
    {
        if (CountryCode is not null && CountryCode.Trim().Length != CountryCodeLength)
        {
            return Result.Failure(PeakErrors.CountryCodeInvalid);
        }

        return MinAltitudeMeters is { } min && MaxAltitudeMeters is { } max && min > max
            ? Result.Failure(PeakErrors.AltitudeRangeInvalid)
            : Result.Success();
    }
}
