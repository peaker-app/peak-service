using Common.Application.Messaging;
using Common.Application.Pagination;
using PeakService.Application.Peaks.ListPeaks;

namespace PeakService.Application.Peaks.SearchPeaks;

public sealed record SearchPeaksQuery(string Query, PeakFilter Filter, PageRequest Page)
    : IQuery<PagedResult<PeakListItemResponse>>;
