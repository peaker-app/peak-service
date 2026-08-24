using Common.Application.Messaging;
using Common.Application.Pagination;
using Common.Domain.Results;
using PeakService.Application.Abstractions;
using PeakService.Domain.Peaks;

namespace PeakService.Application.Peaks.NearbyPeaks;

internal sealed class NearbyPeaksQueryHandler(IPeakCatalogReader reader)
    : IQueryHandler<NearbyPeaksQuery, PagedResult<NearbyPeakResponse>>
{
    public async Task<Result<PagedResult<NearbyPeakResponse>>> Handle(
        NearbyPeaksQuery query,
        CancellationToken cancellationToken)
    {
        Result page = query.Page.Validate();
        if (page.IsFailure)
        {
            return Result.Failure<PagedResult<NearbyPeakResponse>>(page.Error);
        }

        if (query.RadiusMeters is <= 0 or > NearbySearchLimits.MaxRadiusMeters)
        {
            return Result.Failure<PagedResult<NearbyPeakResponse>>(PeakErrors.RadiusOutOfRange);
        }

        Result<Coordinates> origin = Coordinates.Create(query.Latitude, query.Longitude);

        return origin.IsFailure
            ? Result.Failure<PagedResult<NearbyPeakResponse>>(origin.Error)
            : await reader.NearbyAsync(origin.Value, query.RadiusMeters, query.Page, cancellationToken);
    }
}
