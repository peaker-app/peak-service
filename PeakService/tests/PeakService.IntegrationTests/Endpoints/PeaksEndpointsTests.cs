using System.Net;
using Common.API.Responses;
using FluentAssertions;
using PeakService.Application.Peaks.GetPeakById;
using PeakService.Application.Peaks.ListPeaks;
using PeakService.Domain.MountainRanges;
using PeakService.Domain.Peaks;
using PeakService.IntegrationTests.TestData;
using Xunit;

namespace PeakService.IntegrationTests.Endpoints;

[Collection(PeakServiceCollection.Name)]
public sealed class PeaksEndpointsTests(PeakServiceApiFactory factory)
{
    [Fact]
    public async Task List_ReturnsPagedResultWithTotalCount()
    {
        await factory.ResetAsync();
        await factory.SeedAsync(
            TestPeaks.Create("Aneto", 42.63, 0.65, 3404),
            TestPeaks.Create("Posets", 42.66, 0.42, 3375),
            TestPeaks.Create("Monte Perdido", 42.68, 0.03, 3355));

        HttpResponseMessage response = await factory.CreateClient().GetAsync("/api/peaks?page=1&size=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PagedResponse<PeakListItemResponse>? body = await response.ReadAsync<PagedResponse<PeakListItemResponse>>();
        body!.TotalCount.Should().Be(3);
        body.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task List_WithSizeAboveMaximum_ReturnsBadRequest()
    {
        await factory.ResetAsync();

        HttpResponseMessage response = await factory.CreateClient().GetAsync("/api/peaks?size=101");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task List_WithCountryAndAltitudeFilter_ReturnsOnlyMatchingPeaks()
    {
        await factory.ResetAsync();
        await factory.SeedAsync(
            TestPeaks.Create("Aneto", 42.63, 0.65, 3404, countryCode: "ES"),
            TestPeaks.Create("Mont Blanc", 45.83, 6.86, 4808, countryCode: "FR"),
            TestPeaks.Create("Pica d'Estats", 42.67, 1.40, 3143, countryCode: "ES"));

        HttpResponseMessage response = await factory.CreateClient().GetAsync("/api/peaks?country=es&minAltitude=3200");

        PagedResponse<PeakListItemResponse>? body = await response.ReadAsync<PagedResponse<PeakListItemResponse>>();
        body!.Items.Should().ContainSingle(item => item.Name == "Aneto");
    }

    [Fact]
    public async Task GetById_ExistingPeak_ReturnsDetailWithRangeAndAlternativeNames()
    {
        await factory.ResetAsync();
        MountainRange range = MountainRange.Create("Alpes Peninos", null).Value;
        await factory.SeedRangeAsync(range);
        Peak peak = TestPeaks.Create(
            "Matterhorn", 45.976, 7.658, 4478, countryCode: "CH", region: "Valais", rangeId: range.Id,
            alternativeNames: [new PeakNameDraft("it", "Cervino", true)]);
        await factory.SeedAsync(peak);

        HttpResponseMessage response = await factory.CreateClient().GetAsync($"/api/peaks/{peak.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PeakDetailResponse? body = await response.ReadAsync<PeakDetailResponse>();
        body!.Name.Should().Be("Matterhorn");
        body.RangeName.Should().Be("Alpes Peninos");
        body.AlternativeNames.Should().ContainSingle(name => name.Name == "Cervino");
    }

    [Fact]
    public async Task GetById_MissingPeak_ReturnsNotFound()
    {
        await factory.ResetAsync();

        HttpResponseMessage response = await factory.CreateClient().GetAsync($"/api/peaks/{Guid.CreateVersion7()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
