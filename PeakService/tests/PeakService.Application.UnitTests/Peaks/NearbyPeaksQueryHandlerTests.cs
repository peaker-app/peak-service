using Common.Application.Pagination;
using Common.Domain.Results;
using FluentAssertions;
using NSubstitute;
using PeakService.Application.Abstractions;
using PeakService.Application.Peaks.NearbyPeaks;
using PeakService.Domain.Peaks;
using Xunit;

namespace PeakService.Application.UnitTests.Peaks;

public sealed class NearbyPeaksQueryHandlerTests
{
    private readonly IPeakCatalogReader _reader = Substitute.For<IPeakCatalogReader>();
    private readonly NearbyPeaksQueryHandler _handler;

    public NearbyPeaksQueryHandlerTests() => _handler = new NearbyPeaksQueryHandler(_reader);

    [Fact]
    public async Task Handle_WithValidRequest_DelegatesToReader()
    {
        PagedResult<NearbyPeakResponse> expected = new([], 1, 20, 0);
        _reader.NearbyAsync(Arg.Any<Coordinates>(), 5000, Arg.Any<PageRequest>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        Result<PagedResult<NearbyPeakResponse>> result = await _handler.Handle(
            new NearbyPeaksQuery(42.63, 0.65, 5000, new PageRequest(1, 20)), CancellationToken.None);

        result.Value.Should().BeSameAs(expected);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(NearbyPeaksQueryHandler.MaxRadiusMeters + 1)]
    public async Task Handle_WithRadiusOutOfRange_ReturnsRadiusOutOfRange(double radiusMeters)
    {
        Result<PagedResult<NearbyPeakResponse>> result = await _handler.Handle(
            new NearbyPeaksQuery(42.63, 0.65, radiusMeters, new PageRequest(1, 20)), CancellationToken.None);

        result.Error.Should().Be(PeakErrors.RadiusOutOfRange);
    }

    [Fact]
    public async Task Handle_WithLatitudeOutOfRange_ReturnsLatitudeOutOfRange()
    {
        Result<PagedResult<NearbyPeakResponse>> result = await _handler.Handle(
            new NearbyPeaksQuery(91, 0.65, 5000, new PageRequest(1, 20)), CancellationToken.None);

        result.Error.Should().Be(PeakErrors.LatitudeOutOfRange);
    }

    [Fact]
    public async Task Handle_WithInvalidPage_ReturnsPageOutOfRange()
    {
        Result<PagedResult<NearbyPeakResponse>> result = await _handler.Handle(
            new NearbyPeaksQuery(42.63, 0.65, 5000, new PageRequest(0, 20)), CancellationToken.None);

        result.Error.Should().Be(PaginationErrors.PageOutOfRange);
    }
}
