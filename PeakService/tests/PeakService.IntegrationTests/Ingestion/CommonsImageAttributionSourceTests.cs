using PeakService.Domain.Peaks;
using PeakService.Infrastructure.ExternalServices;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using PeakService.IntegrationTests.Fakes;
using Xunit;

namespace PeakService.IntegrationTests.Ingestion;

public sealed class CommonsImageAttributionSourceTests
{
    private const string ImageUrl = "https://commons.wikimedia.org/wiki/Special:FilePath/Aneto%20south.jpg";

    private const string OneCreditedFile =
        """
        {"query":{"pages":[
          {"title":"File:Aneto south.jpg",
           "descriptionurl":"https://commons.wikimedia.org/wiki/File:Aneto_south.jpg",
           "imageinfo":[{
             "descriptionurl":"https://commons.wikimedia.org/wiki/File:Aneto_south.jpg",
             "extmetadata":{
               "Artist":{"value":"<a href=\"/wiki/User:Ana\" title=\"User:Ana\">Ana  Ruiz</a>"},
               "LicenseShortName":{"value":"CC BY-SA 4.0"},
               "LicenseUrl":{"value":"https://creativecommons.org/licenses/by-sa/4.0"}}}]}
        ]}}
        """;

    private const string FileWithoutImageInfo = """{"query":{"pages":[{"title":"File:Aneto south.jpg"}]}}""";

    [Fact]
    public async Task GetAsync_WithACreditedFile_ReadsTheAuthorAsPlainText()
    {
        PeakImageAttribution attribution = await SingleAsync(OneCreditedFile);

        attribution.Author.Should().Be("Ana Ruiz");
    }

    [Fact]
    public async Task GetAsync_WithACreditedFile_ReadsTheLicenceAndItsLinks()
    {
        PeakImageAttribution attribution = await SingleAsync(OneCreditedFile);

        attribution.Should().BeEquivalentTo(new
        {
            License = "CC BY-SA 4.0",
            LicenseUrl = "https://creativecommons.org/licenses/by-sa/4.0",
            CreditUrl = "https://commons.wikimedia.org/wiki/File:Aneto_south.jpg"
        });
    }

    [Fact]
    public async Task GetAsync_AsksCommonsForTheFileBehindTheFilePathUrl()
    {
        StubHttpMessageHandler handler = StubHttpMessageHandler.WithBodies(OneCreditedFile);

        await CreateSource(handler).GetAsync([ImageUrl], CancellationToken.None);

        handler.ReceivedUris[0].Should().Contain("File%3AAneto%20south.jpg");
    }

    [Fact]
    public async Task GetAsync_WithAFileWithoutImageInfo_ReturnsNoAttribution()
    {
        IReadOnlyDictionary<string, PeakImageAttribution> attributions =
            await CreateSource(StubHttpMessageHandler.WithBodies(FileWithoutImageInfo))
                .GetAsync([ImageUrl], CancellationToken.None);

        attributions.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAsync_WithAUrlThatIsNotACommonsFile_SkipsIt()
    {
        StubHttpMessageHandler handler = StubHttpMessageHandler.WithBodies(OneCreditedFile);

        IReadOnlyDictionary<string, PeakImageAttribution> attributions =
            await CreateSource(handler).GetAsync(["https://example.com/photo.jpg"], CancellationToken.None);

        attributions.Should().BeEmpty();
        handler.ReceivedUris.Should().BeEmpty();
    }

    private static async Task<PeakImageAttribution> SingleAsync(string body)
    {
        IReadOnlyDictionary<string, PeakImageAttribution> attributions =
            await CreateSource(StubHttpMessageHandler.WithBodies(body))
                .GetAsync([ImageUrl], CancellationToken.None);

        return attributions[ImageUrl];
    }

    private static CommonsImageAttributionSource CreateSource(StubHttpMessageHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://commons.wikimedia.org/w/api.php") },
            NullLogger<CommonsImageAttributionSource>.Instance);
}
