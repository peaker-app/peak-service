using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using PeakService.IntegrationTests.TestData;
using Xunit;

namespace PeakService.IntegrationTests.Endpoints;

[Collection(PeakServiceCollection.Name)]
public sealed class CatalogCacheTests(PeakServiceApiFactory factory)
{
    private const string SearchRoute = "/api/peaks/search?q=aneto";

    [Fact]
    public async Task Search_AnnouncesAnEntityTag()
    {
        await SeedAnetoAsync();

        using HttpResponseMessage response = await factory.CreateClient().GetAsync(SearchRoute);

        response.Headers.ETag.Should().NotBeNull();
    }

    [Fact]
    public async Task Search_AnnouncesHowLongTheAnswerStaysFresh()
    {
        await SeedAnetoAsync();

        using HttpResponseMessage response = await factory.CreateClient().GetAsync(SearchRoute);

        response.Headers.CacheControl!.MaxAge.Should().Be(TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Search_WithAMatchingEntityTag_ReturnsNotModified()
    {
        await SeedAnetoAsync();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage first = await client.GetAsync(SearchRoute);
        using HttpResponseMessage second = await GetWithEntityTagAsync(client, first.Headers.ETag!.ToString());

        second.StatusCode.Should().Be(HttpStatusCode.NotModified);
    }

    [Fact]
    public async Task Search_WithTheStrongFormOfTheSameEntityTag_StillReturnsNotModified()
    {
        await SeedAnetoAsync();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage first = await client.GetAsync(SearchRoute);
        using HttpResponseMessage second = await GetWithEntityTagAsync(client, first.Headers.ETag!.Tag);

        second.StatusCode.Should().Be(HttpStatusCode.NotModified);
    }

    [Fact]
    public async Task Search_WithAStaleEntityTag_ReturnsTheFullPayload()
    {
        await SeedAnetoAsync();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await GetWithEntityTagAsync(client, "W/\"0000\"");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Search_AfterTheCatalogChanges_ChangesTheEntityTag()
    {
        await SeedAnetoAsync();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage before = await client.GetAsync(SearchRoute);
        await factory.SeedAsync(TestPeaks.Create("Aneto Menor", 42.64, 0.66));
        using HttpResponseMessage after = await client.GetAsync(SearchRoute);

        after.Headers.ETag!.ToString().Should().NotBe(before.Headers.ETag!.ToString());
    }

    private static async Task<HttpResponseMessage> GetWithEntityTagAsync(HttpClient client, string entityTag)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, SearchRoute);
        request.Headers.IfNoneMatch.Add(EntityTagHeaderValue.Parse(entityTag));

        return await client.SendAsync(request);
    }

    private async Task SeedAnetoAsync()
    {
        await factory.ResetAsync();
        await factory.SeedAsync(TestPeaks.Create("Aneto", 42.63, 0.65));
    }
}
