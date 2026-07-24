using System.Net;
using Common.API.Responses;
using FluentAssertions;
using PeakService.Application.Peaks.NearbyPeaks;
using PeakService.IntegrationTests.TestData;
using Xunit;

namespace PeakService.IntegrationTests.Endpoints;

[Collection(PeakServiceCollection.Name)]
public sealed class NearbyEndpointTests(PeakServiceApiFactory factory)
{
    [Fact]
    public async Task Nearby_ReturnsPeaksWithinRadiusOrderedByDistance()
    {
        await factory.ResetAsync();
        await factory.SeedAsync(
            TestPeaks.Create("Near", 42.64, 0.66),
            TestPeaks.Create("Mid", 42.80, 0.90),
            TestPeaks.Create("Far", 43.60, 2.10));

        HttpResponseMessage response = await factory.CreateClient()
            .GetAsync("/api/peaks/nearby?lat=42.63&lon=0.65&radius=50000");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PagedResponse<NearbyPeakResponse>? body = await response.ReadAsync<PagedResponse<NearbyPeakResponse>>();
        body!.Items.Select(item => item.Name).Should().ContainInOrder("Near", "Mid");
        body.Items.Should().NotContain(item => item.Name == "Far");
        body.Items.Should().BeInAscendingOrder(item => item.DistanceMeters);
    }

    [Fact]
    public async Task Nearby_WithRadiusAboveMaximum_ReturnsBadRequest()
    {
        await factory.ResetAsync();

        HttpResponseMessage response = await factory.CreateClient()
            .GetAsync("/api/peaks/nearby?lat=42.63&lon=0.65&radius=200001");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Nearby_WithInvalidLatitude_ReturnsBadRequest()
    {
        await factory.ResetAsync();

        HttpResponseMessage response = await factory.CreateClient()
            .GetAsync("/api/peaks/nearby?lat=91&lon=0.65&radius=5000");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
