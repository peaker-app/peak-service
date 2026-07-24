using Common.Domain.Results;
using FluentAssertions;
using PeakService.Domain.Peaks;
using Xunit;

namespace PeakService.Domain.UnitTests.Peaks;

public sealed class CoordinatesTests
{
    [Theory]
    [InlineData(42.6316, 0.6577)]
    [InlineData(-90, -180)]
    [InlineData(90, 180)]
    [InlineData(0, 0)]
    public void Create_WithCoordinatesInRange_Succeeds(double latitude, double longitude)
    {
        Result<Coordinates> result = Coordinates.Create(latitude, longitude);

        result.IsSuccess.Should().BeTrue();
        result.Value.Latitude.Should().Be(latitude);
        result.Value.Longitude.Should().Be(longitude);
    }

    [Theory]
    [InlineData(-90.0001)]
    [InlineData(90.0001)]
    public void Create_WithLatitudeOutOfRange_ReturnsLatitudeOutOfRange(double latitude)
    {
        Result<Coordinates> result = Coordinates.Create(latitude, 0);

        result.Error.Should().Be(PeakErrors.LatitudeOutOfRange);
    }

    [Theory]
    [InlineData(-180.0001)]
    [InlineData(180.0001)]
    public void Create_WithLongitudeOutOfRange_ReturnsLongitudeOutOfRange(double longitude)
    {
        Result<Coordinates> result = Coordinates.Create(0, longitude);

        result.Error.Should().Be(PeakErrors.LongitudeOutOfRange);
    }

    [Fact]
    public void Create_WithNonFiniteLatitude_ReturnsLatitudeOutOfRange()
    {
        Result<Coordinates> result = Coordinates.Create(double.NaN, 0);

        result.Error.Should().Be(PeakErrors.LatitudeOutOfRange);
    }
}
