using Common.Application.Messaging;
using Common.Application.Pagination;
using Common.Domain.Results;
using PeakService.Application.Abstractions;

namespace PeakService.Application.Peaks.ListPeaks;

internal sealed class ListPeaksQueryHandler(IPeakCatalogReader reader)
    : IQueryHandler<ListPeaksQuery, PagedResult<PeakListItemResponse>>
{
    public async Task<Result<PagedResult<PeakListItemResponse>>> Handle(
        ListPeaksQuery query,
        CancellationToken cancellationToken)
    {
        Result validation = Validate(query);

        return validation.IsFailure
            ? Result.Failure<PagedResult<PeakListItemResponse>>(validation.Error)
            : await reader.ListAsync(query.Filter, query.Page, cancellationToken);
    }

    private static Result Validate(ListPeaksQuery query)
    {
        Result page = query.Page.Validate();

        return page.IsFailure ? page : query.Filter.Validate();
    }
}
