using Common.Application.Messaging;
using Common.Application.Pagination;
using Common.Domain.Results;
using PeakService.Application.Abstractions;
using PeakService.Application.Peaks.ListPeaks;

namespace PeakService.Application.Peaks.SearchPeaks;

internal sealed class SearchPeaksQueryHandler(IPeakCatalogReader reader)
    : IQueryHandler<SearchPeaksQuery, PagedResult<PeakListItemResponse>>
{
    public async Task<Result<PagedResult<PeakListItemResponse>>> Handle(
        SearchPeaksQuery query,
        CancellationToken cancellationToken)
    {
        Result validation = Validate(query);

        return validation.IsFailure
            ? Result.Failure<PagedResult<PeakListItemResponse>>(validation.Error)
            : await reader.SearchAsync(query.Query ?? string.Empty, query.Filter, query.Page, cancellationToken);
    }

    private static Result Validate(SearchPeaksQuery query)
    {
        Result page = query.Page.Validate();

        return page.IsFailure ? page : query.Filter.Validate();
    }
}
