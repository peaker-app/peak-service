using Common.Application.Messaging;
using Common.Domain.Results;
using PeakService.Application.Peaks.Mappings;
using PeakService.Domain.MountainRanges;
using PeakService.Domain.Peaks;

namespace PeakService.Application.Peaks.GetPeakById;

internal sealed class GetPeakByIdQueryHandler(
    IPeakRepository peakRepository,
    IMountainRangeRepository mountainRangeRepository)
    : IQueryHandler<GetPeakByIdQuery, PeakDetailResponse>
{
    public async Task<Result<PeakDetailResponse>> Handle(
        GetPeakByIdQuery query,
        CancellationToken cancellationToken)
    {
        Peak? peak = await peakRepository.GetByIdAsync(query.PeakId, cancellationToken);
        if (peak is null)
        {
            return Result.Failure<PeakDetailResponse>(PeakErrors.NotFound(query.PeakId));
        }

        string? rangeName = await ResolveRangeNameAsync(peak.RangeId, cancellationToken);

        return peak.ToDetailResponse(rangeName);
    }

    private async Task<string?> ResolveRangeNameAsync(Guid? rangeId, CancellationToken cancellationToken)
    {
        if (rangeId is null)
        {
            return null;
        }

        MountainRange? range = await mountainRangeRepository.GetByIdAsync(rangeId.Value, cancellationToken);

        return range?.Name;
    }
}
