using System.Net;
using Common.API.Responses;
using FluentAssertions;
using PeakService.Application.Peaks.ListPeaks;
using PeakService.Domain.Peaks;
using PeakService.IntegrationTests.TestData;
using Xunit;

namespace PeakService.IntegrationTests.Endpoints;

[Collection(PeakServiceCollection.Name)]
public sealed class SearchEndpointTests(PeakServiceApiFactory factory)
{
    [Theory]
    [InlineData("Aneto")]
    [InlineData("aneto")]
    [InlineData("ANETO")]
    [InlineData("Anetó")]
    [InlineData("anetó")]
    public async Task Search_IsCaseAndAccentInsensitive_ReturnsPeak(string term)
    {
        await factory.ResetAsync();
        await factory.SeedAsync(TestPeaks.Create("Aneto", 42.63, 0.65, 3404));

        HttpResponseMessage response = await factory.CreateClient()
            .GetAsync($"/api/peaks/search?q={Uri.EscapeDataString(term)}");

        PagedResponse<PeakListItemResponse>? body = await response.ReadAsync<PagedResponse<PeakListItemResponse>>();
        body!.Items.Should().ContainSingle(item => item.Name == "Aneto");
    }

    [Fact]
    public async Task Search_MatchesAlternativeNamesInAnyLanguage()
    {
        await factory.ResetAsync();
        await factory.SeedAsync(TestPeaks.Create(
            "Matterhorn", 45.976, 7.658, 4478, countryCode: "CH",
            alternativeNames: [new PeakNameDraft("it", "Cervino", true)]));

        HttpResponseMessage response = await factory.CreateClient().GetAsync("/api/peaks/search?q=cervino");

        PagedResponse<PeakListItemResponse>? body = await response.ReadAsync<PagedResponse<PeakListItemResponse>>();
        body!.Items.Should().ContainSingle(item => item.Name == "Matterhorn");
    }

    [Fact]
    public async Task Search_WithNoMatch_ReturnsEmptyOk()
    {
        await factory.ResetAsync();
        await factory.SeedAsync(TestPeaks.Create("Aneto", 42.63, 0.65, 3404));

        HttpResponseMessage response = await factory.CreateClient().GetAsync("/api/peaks/search?q=zzzznotapeak");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PagedResponse<PeakListItemResponse>? body = await response.ReadAsync<PagedResponse<PeakListItemResponse>>();
        body!.Items.Should().BeEmpty();
        body.TotalCount.Should().Be(0);
    }
}
