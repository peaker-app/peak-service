using Common.Application.Messaging;
using Common.Application.Pagination;

namespace PeakService.Application.Peaks.NearbyPeaks;

public sealed record NearbyPeaksQuery(double Latitude, double Longitude, double RadiusMeters, PageRequest Page)
    : IQuery<PagedResult<NearbyPeakResponse>>;
