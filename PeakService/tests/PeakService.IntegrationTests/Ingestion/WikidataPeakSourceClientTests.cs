using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.Extensions.Options;
using PeakService.Application.Abstractions;
using PeakService.Domain.Peaks;
using PeakService.Infrastructure.ExternalServices;
using PeakService.IntegrationTests.Fakes;
using Xunit;

namespace PeakService.IntegrationTests.Ingestion;

public sealed class WikidataPeakSourceClientTests
{
    private const string UserAgent = "PeakerIngestionTests/1.0";

    private const string OnePeak =
        """
        {"results":{"bindings":[
          {"item":{"value":"http://www.wikidata.org/entity/Q192580"},
           "itemLabel":{"value":"Aneto"},
           "elevation":{"value":"3404.0"},
           "coord":{"value":"Point(0.6577 42.6316)"},
           "countryCode":{"value":"es"},
           "adminLabel":{"value":"Huesca"},
           "modified":{"value":"2026-07-01T10:00:00Z"}}
        ]}}
        """;

    private const string MatterhornWithNames =
        """
        {"results":{"bindings":[
          {"item":{"value":"http://www.wikidata.org/entity/Q1291"},
           "itemLabel":{"value":"Matterhorn"},
           "elevation":{"value":"4478"},
           "coord":{"value":"Point(7.6586 45.9763)"},
           "modified":{"value":"2026-07-01T10:00:00Z"},
           "names":{"value":"it~1~Cervino||fr~1~Cervin||it~0~Monte Cervino||de~1~Matterhorn"}}
        ]}}
        """;

    private const string PeakWithoutCoordinates =
        """
        {"results":{"bindings":[
          {"item":{"value":"http://www.wikidata.org/entity/Q1"},
           "itemLabel":{"value":"Sin coordenada"},
           "elevation":{"value":"1000"}}
        ]}}
        """;

    [Fact]
    public async Task StreamAsync_MapsEveryFieldOfTheSparqlBinding()
    {
        List<PeakSourceRecord> records = await StreamAsync(new StubHttpMessageHandler(OnePeak));

        records.Should().ContainSingle().Which.Should().BeEquivalentTo(new PeakSourceRecord(
            "Q192580",
            "Aneto",
            3404,
            ProminenceMeters: null,
            42.6316,
            0.6577,
            "ES",
            "Huesca",
            "2026-07-01T10:00:00Z"));
    }

    [Fact]
    public async Task StreamAsync_ParsesLabelsAndAliasesIntoAlternativeNames()
    {
        List<PeakSourceRecord> records = await StreamAsync(new StubHttpMessageHandler(MatterhornWithNames));

        records.Single().AlternativeNames.Should().BeEquivalentTo(
        [
            new PeakNameDraft("it", "Cervino", true),
            new PeakNameDraft("fr", "Cervin", true),
            new PeakNameDraft("it", "Monte Cervino", false)
        ]);
    }

    [Fact]
    public async Task StreamAsync_DoesNotRepeatTheCanonicalNameAsAnAlternativeOne()
    {
        List<PeakSourceRecord> records = await StreamAsync(new StubHttpMessageHandler(MatterhornWithNames));

        records.Single().AlternativeNames.Should().NotContain(name => name.Name == "Matterhorn");
    }

    [Fact]
    public async Task StreamAsync_RequestsOnlyTheConfiguredLanguages()
    {
        StubHttpMessageHandler handler = new(OnePeak);

        await StreamAsync(handler, languages: ["es", "it"]);

        handler.ReceivedQueries[0].Should().Contain("%22es%22%2C%22it%22");
    }

    [Fact]
    public async Task StreamAsync_SkipsBindingsWithoutCoordinates()
    {
        List<PeakSourceRecord> records = await StreamAsync(new StubHttpMessageHandler(PeakWithoutCoordinates));

        records.Should().BeEmpty();
    }

    [Fact]
    public async Task StreamAsync_StopsWhenAPageIsNotFull()
    {
        StubHttpMessageHandler handler = new(OnePeak);

        await StreamAsync(handler);

        handler.ReceivedQueries.Should().ContainSingle();
    }

    [Fact]
    public async Task StreamAsync_RequestsFurtherPagesWhileTheyComeFull()
    {
        StubHttpMessageHandler handler = new(OnePeak, OnePeak);

        await StreamAsync(handler, pageSize: 1);

        handler.ReceivedQueries.Should().HaveCount(3);
        handler.ReceivedQueries[1].Should().Contain("OFFSET+1");
    }

    [Fact]
    public async Task StreamAsync_SendsTheConfiguredUserAgent()
    {
        StubHttpMessageHandler handler = new(OnePeak);

        await StreamAsync(handler);

        handler.ReceivedUserAgents.Should().ContainSingle().Which.Should().Be(UserAgent);
    }

    [Fact]
    public async Task StreamAsync_WithoutCursor_DoesNotFilterByModificationDate()
    {
        StubHttpMessageHandler handler = new(OnePeak);

        await StreamAsync(handler);

        handler.ReceivedQueries[0].Should().NotContain("xsd%3AdateTime");
    }

    [Fact]
    public async Task StreamAsync_WithIncrementalCursor_FiltersByModificationDate()
    {
        StubHttpMessageHandler handler = new(OnePeak);
        PeakSourceCursor cursor = PeakSourceCursor.Since(new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc));

        await StreamAsync(handler, cursor: cursor);

        handler.ReceivedQueries[0].Should().Contain("2026-07-01T00%3A00%3A00Z");
    }

    [Fact]
    public async Task StreamAsync_WithMaxRecords_StopsAtTheConfiguredBound()
    {
        StubHttpMessageHandler handler = new(OnePeak, OnePeak, OnePeak);

        List<PeakSourceRecord> records = await StreamAsync(handler, pageSize: 1, maxRecords: 2);

        records.Should().HaveCount(2);
    }

    private static async Task<List<PeakSourceRecord>> StreamAsync(
        StubHttpMessageHandler handler,
        int pageSize = 500,
        int? maxRecords = null,
        PeakSourceCursor? cursor = null,
        IReadOnlyList<string>? languages = null)
    {
        IngestionOptions settings = new()
        {
            PageSize = pageSize,
            MaxRecords = maxRecords,
            DelayBetweenPages = TimeSpan.Zero,
            UserAgent = UserAgent,
            AlternativeNameLanguages = languages ?? ["es", "en"]
        };

        using HttpClient httpClient = new(handler) { BaseAddress = settings.Endpoint };
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(settings.UserAgent);
        httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/sparql-results+json"));

        WikidataPeakSourceClient client = new(httpClient, Options.Create(settings));

        List<PeakSourceRecord> records = [];

        await foreach (PeakSourceRecord record in client.StreamAsync(cursor ?? PeakSourceCursor.Full, CancellationToken.None))
        {
            records.Add(record);
        }

        return records;
    }
}
