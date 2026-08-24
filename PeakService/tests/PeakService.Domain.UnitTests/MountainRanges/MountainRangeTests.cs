using Common.Domain.Results;
using FluentAssertions;
using PeakService.Domain.MountainRanges;
using Xunit;

namespace PeakService.Domain.UnitTests.MountainRanges;

public sealed class MountainRangeTests
{
    [Fact]
    public void Create_WithValidName_Succeeds()
    {
        Result<MountainRange> result = MountainRange.Create("Pirineos", null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Pirineos");
        result.Value.ParentRangeId.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyName_ReturnsNameInvalid(string name)
    {
        Result<MountainRange> result = MountainRange.Create(name, null);

        result.Error.Should().Be(MountainRangeErrors.NameInvalid);
    }

    [Fact]
    public void Create_WithTooLongName_ReturnsNameInvalid()
    {
        Result<MountainRange> result = MountainRange.Create(new string('R', MountainRange.MaxNameLength + 1), null);

        result.Error.Should().Be(MountainRangeErrors.NameInvalid);
    }
}
