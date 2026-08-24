using Common.Domain.Results;
using FluentAssertions;
using PeakService.Domain.Peaks;
using PeakService.Domain.Peaks.Events;
using PeakService.Domain.UnitTests.TestData;
using Xunit;

namespace PeakService.Domain.UnitTests.Peaks;

public sealed class PeakUpdateFromSourceTests
{
    private static readonly PeakNameDraft Cervino = new("it", "Cervino", true);
    private static readonly PeakNameDraft Cervin = new("fr", "Cervin", true);

    [Fact]
    public void UpdateFromSource_WithIdenticalData_ReturnsUnchanged()
    {
        Peak peak = Existing();

        Result<PeakUpdateOutcome> result = peak.UpdateFromSource(PeakDrafts.Source(), []);

        result.Value.Should().Be(PeakUpdateOutcome.Unchanged);
    }

    [Fact]
    public void UpdateFromSource_WithIdenticalData_RaisesNoDomainEvent()
    {
        Peak peak = Existing();

        peak.UpdateFromSource(PeakDrafts.Source(), []);

        peak.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void UpdateFromSource_WithChangedAltitude_UpdatesTheAltitude()
    {
        Peak peak = Existing();

        Result<PeakUpdateOutcome> result = peak.UpdateFromSource(PeakDrafts.Source(altitudeMeters: 3410), []);

        result.Value.Should().Be(PeakUpdateOutcome.Updated);
        peak.AltitudeMeters.Should().Be(3410);
    }

    [Fact]
    public void UpdateFromSource_WithChangedAltitude_RaisesUpdatedButNotRenamed()
    {
        Peak peak = Existing();

        peak.UpdateFromSource(PeakDrafts.Source(altitudeMeters: 3410), []);

        peak.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PeakUpdatedDomainEvent>();
    }

    [Fact]
    public void UpdateFromSource_WithChangedName_RaisesUpdatedAndRenamed()
    {
        Peak peak = Existing();

        peak.UpdateFromSource(PeakDrafts.Source(name: "Pico de Aneto"), []);

        peak.DomainEvents.Select(domainEvent => domainEvent.GetType())
            .Should().BeEquivalentTo([typeof(PeakUpdatedDomainEvent), typeof(PeakRenamedDomainEvent)]);
    }

    [Fact]
    public void UpdateFromSource_WithChangedName_CarriesTheNewNameInTheRenamedEvent()
    {
        Peak peak = Existing();

        peak.UpdateFromSource(PeakDrafts.Source(name: "Pico de Aneto"), []);

        peak.DomainEvents.OfType<PeakRenamedDomainEvent>().Single()
            .Should().BeEquivalentTo(new PeakRenamedDomainEvent(peak.Id, "Pico de Aneto", 3404, "ES"));
    }

    [Fact]
    public void UpdateFromSource_WithChangedCoordinates_ReturnsUpdated()
    {
        Peak peak = Existing();

        Result<PeakUpdateOutcome> result = peak.UpdateFromSource(PeakDrafts.Source(latitude: 42.7), []);

        result.Value.Should().Be(PeakUpdateOutcome.Updated);
    }

    [Fact]
    public void UpdateFromSource_WithNewSourceRevision_ReturnsUpdated()
    {
        Peak peak = Existing();

        Result<PeakUpdateOutcome> result = peak.UpdateFromSource(
            PeakDrafts.Source(sourceRevision: "2026-07-24T10:00:00Z"), []);

        result.Value.Should().Be(PeakUpdateOutcome.Updated);
        peak.SourceRevision.Should().Be("2026-07-24T10:00:00Z");
    }

    [Fact]
    public void UpdateFromSource_WithNewAlternativeName_AddsIt()
    {
        Peak peak = Existing();

        Result<PeakUpdateOutcome> result = peak.UpdateFromSource(PeakDrafts.Source(), [Cervino]);

        result.Value.Should().Be(PeakUpdateOutcome.Updated);
        peak.AlternativeNames.Should().ContainSingle().Which.Name.Should().Be("Cervino");
    }

    [Fact]
    public void UpdateFromSource_WithTheSameAlternativeNames_ReturnsUnchanged()
    {
        Peak peak = Existing([Cervino, Cervin]);

        Result<PeakUpdateOutcome> result = peak.UpdateFromSource(PeakDrafts.Source(), [Cervino, Cervin]);

        result.Value.Should().Be(PeakUpdateOutcome.Unchanged);
        peak.AlternativeNames.Should().HaveCount(2);
    }

    [Fact]
    public void UpdateFromSource_WithoutAnAlternativeNameThatExisted_RemovesIt()
    {
        Peak peak = Existing([Cervino, Cervin]);

        Result<PeakUpdateOutcome> result = peak.UpdateFromSource(PeakDrafts.Source(), [Cervino]);

        result.Value.Should().Be(PeakUpdateOutcome.Updated);
        peak.AlternativeNames.Select(name => name.Name).Should().BeEquivalentTo(["Cervino"]);
    }

    [Fact]
    public void UpdateFromSource_DoesNotTouchRangeId()
    {
        Guid rangeId = Guid.CreateVersion7();
        Peak peak = Peak.Create(PeakDrafts.Valid(rangeId: rangeId)).Value;

        peak.UpdateFromSource(PeakDrafts.Source(name: "Pico de Aneto"), []);

        peak.RangeId.Should().Be(rangeId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateFromSource_WithEmptyName_ReturnsNameInvalid(string name)
    {
        Peak peak = Existing();

        Result<PeakUpdateOutcome> result = peak.UpdateFromSource(PeakDrafts.Source(name: name), []);

        result.Error.Should().Be(PeakErrors.NameInvalid);
    }

    [Fact]
    public void UpdateFromSource_WithTooLongSourceRevision_ReturnsSourceRevisionTooLong()
    {
        Peak peak = Existing();

        Result<PeakUpdateOutcome> result = peak.UpdateFromSource(
            PeakDrafts.Source(sourceRevision: new string('R', Peak.MaxSourceRevisionLength + 1)), []);

        result.Error.Should().Be(PeakErrors.SourceRevisionTooLong);
    }

    [Fact]
    public void UpdateFromSource_WithANewImage_ReturnsUpdatedAndStoresTheUrl()
    {
        Peak peak = Existing();

        Result<PeakUpdateOutcome> result = peak.UpdateFromSource(
            PeakDrafts.Source(imageUrl: "https://commons.wikimedia.org/wiki/Special:FilePath/Aneto.jpg"), []);

        result.Value.Should().Be(PeakUpdateOutcome.Updated);
        peak.ImageUrl.Should().Be("https://commons.wikimedia.org/wiki/Special:FilePath/Aneto.jpg");
    }

    [Fact]
    public void UpdateFromSource_WithTheSameImage_ReturnsUnchanged()
    {
        Peak peak = Peak.Create(PeakDrafts.Valid(imageUrl: "https://commons.wikimedia.org/photo.jpg")).Value;
        peak.ClearDomainEvents();

        Result<PeakUpdateOutcome> result = peak.UpdateFromSource(
            PeakDrafts.Source(imageUrl: "https://commons.wikimedia.org/photo.jpg"), []);

        result.Value.Should().Be(PeakUpdateOutcome.Unchanged);
    }

    [Fact]
    public void UpdateFromSource_WithTooLongImageUrl_ReturnsImageUrlTooLong()
    {
        Peak peak = Existing();

        Result<PeakUpdateOutcome> result = peak.UpdateFromSource(
            PeakDrafts.Source(imageUrl: new string('u', Peak.MaxImageUrlLength + 1)), []);

        result.Error.Should().Be(PeakErrors.ImageUrlTooLong);
    }

    [Fact]
    public void UpdateFromSource_WithInvalidData_LeavesThePeakUntouched()
    {
        Peak peak = Existing([Cervino]);

        peak.UpdateFromSource(PeakDrafts.Source(name: string.Empty, altitudeMeters: 9999), []);

        peak.Name.Should().Be("Aneto");
        peak.AltitudeMeters.Should().Be(3404);
        peak.AlternativeNames.Should().ContainSingle();
    }

    private static Peak Existing(IReadOnlyList<PeakNameDraft>? alternativeNames = null)
    {
        Peak peak = Peak.Create(PeakDrafts.Valid(alternativeNames: alternativeNames)).Value;
        peak.ClearDomainEvents();

        return peak;
    }
}
