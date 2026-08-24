using Common.Application.Pagination;
using PeakService.Application.Peaks;
using PeakService.Application.Peaks.ListPeaks;
using PeakService.Application.Peaks.NearbyPeaks;
using PeakService.Domain.Peaks;

namespace PeakService.Application.Abstractions;

public interface IPeakCatalogReader
{
    Task<PagedResult<PeakListItemResponse>> ListAsync(
        PeakFilter filter,
        PageRequest page,
        CancellationToken cancellationToken);

    Task<PagedResult<PeakListItemResponse>> SearchAsync(
        string query,
        PeakFilter filter,
        PageRequest page,
        CancellationToken cancellationToken);

    Task<PagedResult<NearbyPeakResponse>> NearbyAsync(
        Coordinates origin,
        double radiusMeters,
        PageRequest page,
        CancellationToken cancellationToken);
}
