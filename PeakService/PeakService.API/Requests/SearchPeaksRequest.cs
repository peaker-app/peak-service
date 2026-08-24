using Common.Application.Pagination;
using PeakService.Application.Peaks;
using PeakService.Application.Peaks.SearchPeaks;

namespace PeakService.API.Requests;

public sealed record SearchPeaksRequest
{
    public string? Q { get; init; }

    public int? Page { get; init; }

    public int? Size { get; init; }

    public string? Country { get; init; }

    public string? Region { get; init; }

    public int? MinAltitude { get; init; }

    public int? MaxAltitude { get; init; }

    public SearchPeaksQuery ToQuery() => new(
        Q ?? string.Empty,
        new PeakFilter(Country, Region, MinAltitude, MaxAltitude),
        new PageRequest(Page ?? PageRequest.MinPage, Size ?? PageRequest.DefaultSize));
}
