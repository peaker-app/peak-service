using Common.Application.Pagination;
using Common.Domain.Results;
using FluentAssertions;
using NSubstitute;
using PeakService.Application.Abstractions;
using PeakService.Application.Peaks;
using PeakService.Application.Peaks.ListPeaks;
using PeakService.Application.Peaks.SearchPeaks;
using PeakService.Domain.Peaks;
using Xunit;

namespace PeakService.Application.UnitTests.Peaks;

public sealed class SearchPeaksQueryHandlerTests
{
    private readonly IPeakCatalogReader _reader = Substitute.For<IPeakCatalogReader>();
    private readonly SearchPeaksQueryHandler _handler;

    public SearchPeaksQueryHandlerTests() => _handler = new SearchPeaksQueryHandler(_reader);

    [Fact]
    public async Task Handle_WithValidRequest_DelegatesToReader()
    {
        PagedResult<PeakListItemResponse> expected = new([], 1, 20, 0);
        _reader.SearchAsync("aneto", Arg.Any<PeakFilter>(), Arg.Any<PageRequest>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        Result<PagedResult<PeakListItemResponse>> result = await _handler.Handle(
            new SearchPeaksQuery("aneto", PeakFilter.None, new PageRequest(1, 20)), CancellationToken.None);

        result.Value.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task Handle_WithSizeAboveMaximum_ReturnsValidationError()
    {
        Result<PagedResult<PeakListItemResponse>> result = await _handler.Handle(
            new SearchPeaksQuery("aneto", PeakFilter.None, new PageRequest(1, PageRequest.MaxSize + 1)),
            CancellationToken.None);

        result.Error.Should().Be(PaginationErrors.SizeOutOfRange);
    }

    [Fact]
    public async Task Handle_WithMalformedCountryFilter_ReturnsCountryCodeInvalid()
    {
        PeakFilter filter = new(CountryCode: "ESP", null, null, null);

        Result<PagedResult<PeakListItemResponse>> result = await _handler.Handle(
            new SearchPeaksQuery("aneto", filter, new PageRequest(1, 20)), CancellationToken.None);

        result.Error.Should().Be(PeakErrors.CountryCodeInvalid);
    }
}
