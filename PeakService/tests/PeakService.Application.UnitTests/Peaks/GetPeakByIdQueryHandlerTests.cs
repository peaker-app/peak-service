using Common.Domain.Results;
using FluentAssertions;
using NSubstitute;
using PeakService.Application.Peaks.GetPeakById;
using PeakService.Application.UnitTests.TestData;
using PeakService.Domain.MountainRanges;
using PeakService.Domain.Peaks;
using Xunit;

namespace PeakService.Application.UnitTests.Peaks;

public sealed class GetPeakByIdQueryHandlerTests
{
    private readonly IPeakRepository _peakRepository = Substitute.For<IPeakRepository>();
    private readonly IMountainRangeRepository _mountainRangeRepository = Substitute.For<IMountainRangeRepository>();
    private readonly GetPeakByIdQueryHandler _handler;

    public GetPeakByIdQueryHandlerTests() =>
        _handler = new GetPeakByIdQueryHandler(_peakRepository, _mountainRangeRepository);

    [Fact]
    public async Task Handle_WithExistingPeakWithoutRange_ReturnsResponseWithoutRangeName()
    {
        Peak peak = PeakFactory.Create(alternativeNames: [new PeakNameDraft("fr", "Néthou", false)]);
        _peakRepository.GetByIdAsync(peak.Id, Arg.Any<CancellationToken>()).Returns(peak);

        Result<PeakDetailResponse> result = await _handler.Handle(
            new GetPeakByIdQuery(peak.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RangeName.Should().BeNull();
        result.Value.AlternativeNames.Should().ContainSingle(name => name.Name == "Néthou");
    }

    [Fact]
    public async Task Handle_WithExistingPeakWithRange_ResolvesRangeName()
    {
        MountainRange range = MountainRange.Create("Pirineos", null).Value;
        Peak peak = PeakFactory.Create(rangeId: range.Id);
        _peakRepository.GetByIdAsync(peak.Id, Arg.Any<CancellationToken>()).Returns(peak);
        _mountainRangeRepository.GetByIdAsync(range.Id, Arg.Any<CancellationToken>()).Returns(range);

        Result<PeakDetailResponse> result = await _handler.Handle(
            new GetPeakByIdQuery(peak.Id), CancellationToken.None);

        result.Value.RangeName.Should().Be("Pirineos");
    }

    [Fact]
    public async Task Handle_WithMissingPeak_ReturnsNotFound()
    {
        Guid peakId = Guid.CreateVersion7();
        _peakRepository.GetByIdAsync(peakId, Arg.Any<CancellationToken>()).Returns((Peak?)null);

        Result<PeakDetailResponse> result = await _handler.Handle(
            new GetPeakByIdQuery(peakId), CancellationToken.None);

        result.Error.Type.Should().Be(ErrorType.NotFound);
    }
}
