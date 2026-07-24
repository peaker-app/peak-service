using Common.Application.Pagination;
using Common.Domain.Results;
using FluentAssertions;
using NSubstitute;
using PeakService.Application.Abstractions;
using PeakService.Application.Peaks;
using PeakService.Application.Peaks.ListPeaks;
using PeakService.Domain.Peaks;
using Xunit;

namespace PeakService.Application.UnitTests.Peaks;

public sealed class ListPeaksQueryHandlerTests
{
    private readonly IPeakCatalogReader _reader = Substitute.For<IPeakCatalogReader>();
    private readonly ListPeaksQueryHandler _handler;

    public ListPeaksQueryHandlerTests() => _handler = new ListPeaksQueryHandler(_reader);

    [Fact]
    public async Task Handle_WithValidRequest_ReturnsPagedResultFromReader()
    {
        var expected = new PagedResult<PeakListItemResponse>([], 1, 20, 0);
        _reader.ListAsync(Arg.Any<PeakFilter>(), Arg.Any<PageRequest>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        Result<PagedResult<PeakListItemResponse>> result = await _handler.Handle(
            new ListPeaksQuery(PeakFilter.None, new PageRequest(1, 20)), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task Handle_WithSizeAboveMaximum_ReturnsValidationErrorWithoutQueryingReader()
    {
        Result<PagedResult<PeakListItemResponse>> result = await _handler.Handle(
            new ListPeaksQuery(PeakFilter.None, new PageRequest(1, PageRequest.MaxSize + 1)), CancellationToken.None);

        result.Error.Should().Be(PaginationErrors.SizeOutOfRange);
        await _reader.DidNotReceiveWithAnyArgs().ListAsync(default!, default!, default);
    }

    [Fact]
    public async Task Handle_WithPageBelowMinimum_ReturnsValidationError()
    {
        Result<PagedResult<PeakListItemResponse>> result = await _handler.Handle(
            new ListPeaksQuery(PeakFilter.None, new PageRequest(0, 20)), CancellationToken.None);

        result.Error.Should().Be(PaginationErrors.PageOutOfRange);
    }

    [Fact]
    public async Task Handle_WithInvertedAltitudeRange_ReturnsAltitudeRangeInvalid()
    {
        var filter = new PeakFilter(null, null, MinAltitudeMeters: 3000, MaxAltitudeMeters: 1000);

        Result<PagedResult<PeakListItemResponse>> result = await _handler.Handle(
            new ListPeaksQuery(filter, new PageRequest(1, 20)), CancellationToken.None);

        result.Error.Should().Be(PeakErrors.AltitudeRangeInvalid);
    }
}
