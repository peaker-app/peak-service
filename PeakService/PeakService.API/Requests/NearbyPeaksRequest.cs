using PeakService.Domain.Peaks;
using Common.Application.Pagination;
using PeakService.Application.Peaks.NearbyPeaks;

namespace PeakService.API.Requests;

public sealed record NearbyPeaksRequest
{
    public double? Lat { get; init; }

    public double? Lon { get; init; }

    public double? Radius { get; init; }

    public int? Page { get; init; }

    public int? Size { get; init; }

    public NearbyPeaksQuery ToQuery() => new(
        Lat ?? double.NaN,
        Lon ?? double.NaN,
        Radius ?? NearbySearchLimits.DefaultRadiusMeters,
        new PageRequest(Page ?? PageRequest.MinPage, Size ?? PageRequest.DefaultSize));
}
