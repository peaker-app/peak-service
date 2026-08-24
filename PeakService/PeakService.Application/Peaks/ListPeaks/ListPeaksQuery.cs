using Common.Application.Messaging;
using Common.Application.Pagination;

namespace PeakService.Application.Peaks.ListPeaks;

public sealed record ListPeaksQuery(PeakFilter Filter, PageRequest Page)
    : IQuery<PagedResult<PeakListItemResponse>>;
