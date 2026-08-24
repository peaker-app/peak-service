using Common.Domain.Abstractions;
using Common.Domain.Results;

namespace PeakService.Domain.Peaks;

public sealed class Coordinates : ValueObject
{
    public const double MinLatitude = -90;
    public const double MaxLatitude = 90;
    public const double MinLongitude = -180;
    public const double MaxLongitude = 180;

    private Coordinates(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public double Latitude { get; }

    public double Longitude { get; }

    public static Result<Coordinates> Create(double latitude, double longitude)
    {
        if (!double.IsFinite(latitude) || latitude is < MinLatitude or > MaxLatitude)
        {
            return PeakErrors.LatitudeOutOfRange;
        }

        return !double.IsFinite(longitude) || longitude is < MinLongitude or > MaxLongitude
            ? PeakErrors.LongitudeOutOfRange
            : new Coordinates(latitude, longitude);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Latitude;
        yield return Longitude;
    }
}
