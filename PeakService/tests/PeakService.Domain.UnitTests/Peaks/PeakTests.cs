using Common.Domain.Results;
using FluentAssertions;
using PeakService.Domain.Peaks;
using PeakService.Domain.Peaks.Events;
using PeakService.Domain.UnitTests.TestData;
using Xunit;

namespace PeakService.Domain.UnitTests.Peaks;

public sealed class PeakTests
{
    private const string LicenseUrl = "https://creativecommons.org/licenses/by-sa/4.0";
    private const string CreditUrl = "https://commons.wikimedia.org/wiki/File:Aneto.jpg";

    [Fact]
    public void Create_WithValidDraft_SucceedsAndCopiesData()
    {
        PeakDraft draft = PeakDrafts.Valid();

        Result<Peak> result = Peak.Create(draft);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().NotBe(Guid.Empty);
        result.Value.Name.Should().Be(draft.Source.Name);
        result.Value.AltitudeMeters.Should().Be(draft.Source.AltitudeMeters);
        result.Value.Coordinates.Should().Be(draft.Source.Coordinates);
    }

    [Fact]
    public void Create_WithAlternativeNames_AddsThemAll()
    {
        PeakDraft draft = PeakDrafts.Valid(alternativeNames:
        [
            new PeakNameDraft("de", "Matterhorn", true),
            new PeakNameDraft("it", "Cervino", true)
        ]);

        Result<Peak> result = Peak.Create(draft);

        result.Value.AlternativeNames.Should().HaveCount(2);
        result.Value.AlternativeNames.Select(name => name.Name)
            .Should().BeEquivalentTo("Matterhorn", "Cervino");
    }

    [Fact]
    public void Create_WithValidDraft_RaisesPeakCreatedDomainEvent()
    {
        Result<Peak> result = Peak.Create(PeakDrafts.Valid());

        result.Value.DomainEvents.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new PeakCreatedDomainEvent(
                result.Value.Id, "Aneto", 3404, "ES"));
    }

    [Fact]
    public void Create_WithNegativeAltitude_Succeeds()
    {
        PeakDraft draft = PeakDrafts.Valid(altitudeMeters: -200);

        Result<Peak> result = Peak.Create(draft);

        result.IsSuccess.Should().BeTrue();
        result.Value.AltitudeMeters.Should().Be(-200);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyWikidataId_ReturnsWikidataIdInvalid(string wikidataId)
    {
        Result<Peak> result = Peak.Create(PeakDrafts.Valid(wikidataId: wikidataId));

        result.Error.Should().Be(PeakErrors.WikidataIdInvalid);
    }

    [Fact]
    public void Create_WithTooLongWikidataId_ReturnsWikidataIdInvalid()
    {
        Result<Peak> result = Peak.Create(PeakDrafts.Valid(wikidataId: new string('Q', Peak.MaxWikidataIdLength + 1)));

        result.Error.Should().Be(PeakErrors.WikidataIdInvalid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyName_ReturnsNameInvalid(string name)
    {
        Result<Peak> result = Peak.Create(PeakDrafts.Valid(name: name));

        result.Error.Should().Be(PeakErrors.NameInvalid);
    }

    [Fact]
    public void Create_WithTooLongName_ReturnsNameInvalid()
    {
        Result<Peak> result = Peak.Create(PeakDrafts.Valid(name: new string('A', Peak.MaxNameLength + 1)));

        result.Error.Should().Be(PeakErrors.NameInvalid);
    }

    [Fact]
    public void Create_WithTooLongRegion_ReturnsRegionTooLong()
    {
        Result<Peak> result = Peak.Create(PeakDrafts.Valid(region: new string('R', Peak.MaxRegionLength + 1)));

        result.Error.Should().Be(PeakErrors.RegionTooLong);
    }

    [Fact]
    public void Create_WithAnImage_StoresItsUrl()
    {
        Result<Peak> result = Peak.Create(
            PeakDrafts.Valid(imageUrl: "https://commons.wikimedia.org/wiki/Special:FilePath/Aneto.jpg"));

        result.Value.ImageUrl.Should().Be("https://commons.wikimedia.org/wiki/Special:FilePath/Aneto.jpg");
    }

    [Fact]
    public void Create_WithoutAnImage_LeavesTheUrlEmpty()
    {
        Result<Peak> result = Peak.Create(PeakDrafts.Valid());

        result.Value.ImageUrl.Should().BeNull();
    }

    [Fact]
    public void Create_WithTooLongImageUrl_ReturnsImageUrlTooLong()
    {
        Result<Peak> result = Peak.Create(
            PeakDrafts.Valid(imageUrl: new string('u', Peak.MaxImageUrlLength + 1)));

        result.Error.Should().Be(PeakErrors.ImageUrlTooLong);
    }

    [Fact]
    public void Create_WithSurroundingWhitespaceInTheText_StoresItTrimmed()
    {
        Result<Peak> result = Peak.Create(PeakDrafts.Valid(name: "  Aneto  ", region: "  Huesca  "));

        result.Value.Should().BeEquivalentTo(new { Name = "Aneto", Region = "Huesca" });
    }

    [Fact]
    public void Create_WithAControlCharacterInTheName_ReturnsNameInvalid()
    {
        Result<Peak> result = Peak.Create(PeakDrafts.Valid(name: "An" + (char)7 + "eto"));

        result.Error.Should().Be(PeakErrors.NameInvalid);
    }

    [Fact]
    public void Create_WithAControlCharacterInTheRegion_ReturnsRegionInvalid()
    {
        Result<Peak> result = Peak.Create(PeakDrafts.Valid(region: "Hues" + (char)0 + "ca"));

        result.Error.Should().Be(PeakErrors.RegionInvalid);
    }

    [Fact]
    public void Create_WithAnImageUrlThatIsNotHttps_ReturnsImageUrlInvalid()
    {
        Result<Peak> result = Peak.Create(PeakDrafts.Valid(imageUrl: "javascript:alert(1)"));

        result.Error.Should().Be(PeakErrors.ImageUrlInvalid);
    }

    [Fact]
    public void Create_WithAnAttributedImage_StoresTheCredit()
    {
        Result<Peak> result = Peak.Create(PeakDrafts.Valid(
            imageUrl: "https://commons.wikimedia.org/wiki/Special:FilePath/Aneto.jpg",
            attribution: new PeakImageAttribution("Ana Ruiz", "CC BY-SA 4.0", LicenseUrl, CreditUrl)));

        result.Value.Attribution.Should().Be(
            new PeakImageAttribution("Ana Ruiz", "CC BY-SA 4.0", LicenseUrl, CreditUrl));
    }

    [Fact]
    public void Create_WithAnAttributionButNoImage_DropsTheCredit()
    {
        Result<Peak> result = Peak.Create(PeakDrafts.Valid(
            attribution: new PeakImageAttribution("Ana Ruiz", "CC BY-SA 4.0", LicenseUrl, CreditUrl)));

        result.Value.Attribution.Should().Be(PeakImageAttribution.None);
    }

    [Fact]
    public void Create_WithAnAuthorLongerThanTheMaximum_ReturnsImageAttributionTooLong()
    {
        Result<Peak> result = Peak.Create(PeakDrafts.Valid(
            imageUrl: "https://commons.wikimedia.org/wiki/Special:FilePath/Aneto.jpg",
            attribution: new PeakImageAttribution(new string('a', Peak.MaxImageAuthorLength + 1), "CC0", null, null)));

        result.Error.Should().Be(PeakErrors.ImageAttributionTooLong);
    }

    [Fact]
    public void Create_WithAnAlternativeNameLongerThanTheMaximum_DropsIt()
    {
        Result<Peak> result = Peak.Create(PeakDrafts.Valid(alternativeNames:
        [
            new PeakNameDraft("es", new string('n', PeakName.MaxNameLength + 1), IsOfficial: false),
            new PeakNameDraft("fr", "Pic d'Aneto", IsOfficial: false)
        ]));

        result.Value.AlternativeNames.Should().ContainSingle().Which.Name.Should().Be("Pic d'Aneto");
    }
}
