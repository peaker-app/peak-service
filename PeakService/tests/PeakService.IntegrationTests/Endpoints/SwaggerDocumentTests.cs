using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace PeakService.IntegrationTests.Endpoints;

[Collection(PeakServiceCollection.Name)]
public sealed class SwaggerDocumentTests(PeakServiceApiFactory factory)
{
    [Fact]
    public async Task Document_IsTitledAfterItsOwnService()
    {
        using JsonDocument document = await ReadDocumentAsync();

        document.RootElement.GetProperty("info").GetProperty("title").GetString()
            .Should().Be("peak-service");
    }

    [Fact]
    public async Task Document_OnlyExposesItsOwnRoutes()
    {
        using JsonDocument document = await ReadDocumentAsync();

        IEnumerable<string> paths = document.RootElement
            .GetProperty("paths").EnumerateObject().Select(path => path.Name);

        paths.Should().OnlyContain(path => path.StartsWith("/api/peaks", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Document_DeclaresARelativeServerSoItWorksBehindTheGateway()
    {
        using JsonDocument document = await ReadDocumentAsync();

        document.RootElement.GetProperty("servers")[0].GetProperty("url").GetString()
            .Should().Be("/");
    }

    [Fact]
    public async Task Document_DeclaresTheBearerSecurityScheme()
    {
        using JsonDocument document = await ReadDocumentAsync();

        JsonElement bearer = document.RootElement
            .GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");

        bearer.GetProperty("type").GetString().Should().Be("http");
        bearer.GetProperty("scheme").GetString().Should().Be("bearer");
    }

    private async Task<JsonDocument> ReadDocumentAsync()
    {
        using HttpClient client = factory.CreateClient();

        return JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
    }
}
