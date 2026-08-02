using System.Net;
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

    private const string OnePeakTwice =
        """
        {"results":{"bindings":[
          {"item":{"value":"http://www.wikidata.org/entity/Q192580"},
           "itemLabel":{"value":"Aneto"},"elevation":{"value":"3404"},
           "coord":{"value":"Point(0.6577 42.6316)"},"adminLabel":{"value":"Huesca"}},
          {"item":{"value":"http://www.wikidata.org/entity/Q192580"},
           "itemLabel":{"value":"Aneto"},"elevation":{"value":"3404"},
           "coord":{"value":"Point(0.6577 42.6316)"},"adminLabel":{"value":"Benasque"}}
        ]}}
        """;

    private const string Matterhorn =
        """
        {"results":{"bindings":[
          {"item":{"value":"http://www.wikidata.org/entity/Q1291"},
           "itemLabel":{"value":"Matterhorn"},
           "elevation":{"value":"4478"},
           "coord":{"value":"Point(7.6586 45.9763)"},
           "modified":{"value":"2026-07-01T10:00:00Z"}}
        ]}}
        """;

    private const string MatterhornNames =
        """
        {"results":{"bindings":[
          {"item":{"value":"http://www.wikidata.org/entity/Q1291"},
           "name":{"value":"Cervino","xml:lang":"it"},"official":{"value":"1"}},
          {"item":{"value":"http://www.wikidata.org/entity/Q1291"},
           "name":{"value":"Cervin","xml:lang":"fr"},"official":{"value":"1"}},
          {"item":{"value":"http://www.wikidata.org/entity/Q1291"},
           "name":{"value":"Monte Cervino","xml:lang":"it"},"official":{"value":"0"}},
          {"item":{"value":"http://www.wikidata.org/entity/Q1291"},
           "name":{"value":"Matterhorn","xml:lang":"de"},"official":{"value":"1"}}
        ]}}
        """;

    private const string UnlabelledPeak =
        """
        {"results":{"bindings":[
          {"item":{"value":"http://www.wikidata.org/entity/Q8538208"},
           "itemLabel":{"value":"Q8538208"},
           "elevation":{"value":"438"},
           "coord":{"value":"Point(-5.84111 43.45361)"},
           "modified":{"value":"2026-07-01T10:00:00Z"}}
        ]}}
        """;

    private const string UnlabelledPeakNames =
        """
        {"results":{"bindings":[
          {"item":{"value":"http://www.wikidata.org/entity/Q8538208"},
           "name":{"value":"Santo Firme","xml:lang":"es"},"official":{"value":"1"}},
          {"item":{"value":"http://www.wikidata.org/entity/Q8538208"},
           "name":{"value":"Picu Santu Firme","xml:lang":"ast"},"official":{"value":"0"}}
        ]}}
        """;

    private const string UnlabelledPeakEnglishNames =
        """
        {"results":{"bindings":[
          {"item":{"value":"http://www.wikidata.org/entity/Q8538208"},
           "name":{"value":"Santo Firme","xml:lang":"es"},"official":{"value":"1"}},
          {"item":{"value":"http://www.wikidata.org/entity/Q8538208"},
           "name":{"value":"Holy Firme","xml:lang":"en"},"official":{"value":"1"}}
        ]}}
        """;

    private const string PeakWithPhoto =
        """
        {"results":{"bindings":[
          {"item":{"value":"http://www.wikidata.org/entity/Q192580"},
           "itemLabel":{"value":"Aneto"},
           "elevation":{"value":"3404"},
           "coord":{"value":"Point(0.6577 42.6316)"},
           "image":{"value":"http://commons.wikimedia.org/wiki/Special:FilePath/Aneto%20south.jpg"},
           "modified":{"value":"2026-07-01T10:00:00Z"}}
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

    private const string NoNames = """{"results":{"bindings":[]}}""";

    private const string NoPeaks = """{"results":{"bindings":[]}}""";

    private const string TruncatedStream =
        "{\"results\":{\"bindings\":[\n" +
        "  {\"item\":{\"value\":\"http://www.wikidata.org/entity/Q192580\"},\n" +
        "   \"itemLabel\":{\"value\":\"Aneto\"},\"elevation\":{\"value\":\"3404\"},\n" +
        "   \"coord\":{\"datatype\":\"http://www.opengis.net/ont/geosparql#wktLi\n";

    [Fact]
    public async Task StreamAsync_MapsEveryFieldOfTheSparqlBinding()
    {
        List<PeakSourceRecord> records = await RecordsAsync(StubHttpMessageHandler.WithBodies(OnePeak, NoNames));

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
    public async Task StreamAsync_SkipsBindingsWithoutCoordinates()
    {
        List<PeakSourceRecord> records = await RecordsAsync(StubHttpMessageHandler.WithBodies(PeakWithoutCoordinates));

        records.Should().BeEmpty();
    }

    [Fact]
    public async Task StreamAsync_MergesTheNamesFetchedInASeparateBatch()
    {
        List<PeakSourceRecord> records = await RecordsAsync(
            StubHttpMessageHandler.WithBodies(Matterhorn, MatterhornNames));

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
        List<PeakSourceRecord> records = await RecordsAsync(
            StubHttpMessageHandler.WithBodies(Matterhorn, MatterhornNames));

        records.Single().AlternativeNames.Should().NotContain(name => name.Name == "Matterhorn");
    }

    [Fact]
    public async Task StreamAsync_WhenTheLabelIsTheWikidataId_PromotesAnAlternativeNameAsCanonical()
    {
        List<PeakSourceRecord> records = await RecordsAsync(
            StubHttpMessageHandler.WithBodies(UnlabelledPeak, UnlabelledPeakNames));

        records.Single().Name.Should().Be("Santo Firme");
    }

    [Fact]
    public async Task StreamAsync_WhenTheLabelIsTheWikidataId_DoesNotRepeatThePromotedNameAsAnAlternativeOne()
    {
        List<PeakSourceRecord> records = await RecordsAsync(
            StubHttpMessageHandler.WithBodies(UnlabelledPeak, UnlabelledPeakNames));

        records.Single().AlternativeNames.Should().BeEquivalentTo(
            [new PeakNameDraft("ast", "Picu Santu Firme", false)]);
    }

    [Fact]
    public async Task StreamAsync_WhenTheLabelIsTheWikidataId_PrefersTheCanonicalLanguage()
    {
        List<PeakSourceRecord> records = await RecordsAsync(
            StubHttpMessageHandler.WithBodies(UnlabelledPeak, UnlabelledPeakEnglishNames));

        records.Single().Name.Should().Be("Holy Firme");
    }

    [Fact]
    public async Task StreamAsync_WhenTheLabelIsTheWikidataIdAndNoNameExists_KeepsTheIdentifier()
    {
        List<PeakSourceRecord> records = await RecordsAsync(
            StubHttpMessageHandler.WithBodies(UnlabelledPeak, NoNames));

        records.Single().Name.Should().Be("Q8538208");
    }

    [Fact]
    public async Task StreamAsync_WithARealLabel_LeavesTheCanonicalNameUntouched()
    {
        List<PeakSourceRecord> records = await RecordsAsync(
            StubHttpMessageHandler.WithBodies(Matterhorn, MatterhornNames));

        records.Single().Name.Should().Be("Matterhorn");
    }

    [Fact]
    public async Task StreamAsync_WithAnImage_KeepsTheCommonsUrlOverHttps()
    {
        List<PeakSourceRecord> records = await RecordsAsync(
            StubHttpMessageHandler.WithBodies(PeakWithPhoto, NoNames));

        records.Single().ImageUrl.Should()
            .Be("https://commons.wikimedia.org/wiki/Special:FilePath/Aneto%20south.jpg");
    }

    [Fact]
    public async Task StreamAsync_WithoutAnImage_LeavesTheUrlEmpty()
    {
        List<PeakSourceRecord> records = await RecordsAsync(
            StubHttpMessageHandler.WithBodies(OnePeak, NoNames));

        records.Single().ImageUrl.Should().BeNull();
    }

    [Fact]
    public async Task StreamAsync_AsksWikidataForTheDepictedImage()
    {
        StubHttpMessageHandler handler = StubHttpMessageHandler.WithBodies(OnePeak, NoNames);

        await PartitionsAsync(handler, Settings(bandMeters: 9000));

        handler.ReceivedQueries[0].Should().Contain("wdt%3AP18");
    }

    [Fact]
    public async Task StreamAsync_DeduplicatesRowsRepeatedByMultivaluedProperties()
    {
        List<PeakSourceRecord> records = await RecordsAsync(StubHttpMessageHandler.WithBodies(OnePeakTwice, NoNames));

        records.Should().ContainSingle().Which.WikidataId.Should().Be("Q192580");
    }

    [Fact]
    public async Task StreamAsync_SplitsTheBandInHalfWhenTheEndpointTimesOut()
    {
        StubHttpMessageHandler handler = new(
            StubbedResponse.Ok(NoPeaks),
            StubbedResponse.Status(HttpStatusCode.GatewayTimeout),
            StubbedResponse.Ok(OnePeak),
            StubbedResponse.Ok(NoNames));

        List<PeakSourcePartition> partitions = await PartitionsAsync(handler, Settings(bandMeters: 9000));

        partitions.Should().NotContain(partition => partition.IsFailed);
        handler.ReceivedQueries[1].Should().Contain("%3E%3D+0").And.Contain("%3C+9000");
        handler.ReceivedQueries[2].Should().Contain("%3E%3D+0").And.Contain("%3C+4500");
    }

    [Fact]
    public async Task StreamAsync_RetriesAnExhaustedBandInADeferredSecondPass()
    {
        StubHttpMessageHandler handler = new(
            StubbedResponse.Ok(NoPeaks),
            StubbedResponse.Status(HttpStatusCode.GatewayTimeout),
            StubbedResponse.Ok(NoPeaks),
            StubbedResponse.Ok(OnePeak),
            StubbedResponse.Ok(NoNames));

        List<PeakSourcePartition> partitions = await PartitionsAsync(
            handler, Settings(bandMeters: 9000, minBandMeters: 9000));

        partitions.Should().NotContain(partition => partition.IsFailed);
        partitions.SelectMany(partition => partition.Records).Should().ContainSingle();
        handler.ReceivedQueries[3].Should().Contain("%3E%3D+0").And.Contain("%3C+9000");
    }

    [Fact]
    public async Task StreamAsync_ReportsAFailedPartitionWhenTheBandFailsInBothPasses()
    {
        StubHttpMessageHandler handler = new(
            StubbedResponse.Ok(NoPeaks),
            StubbedResponse.Status(HttpStatusCode.GatewayTimeout),
            StubbedResponse.Ok(NoPeaks),
            StubbedResponse.Status(HttpStatusCode.GatewayTimeout));

        List<PeakSourcePartition> partitions = await PartitionsAsync(
            handler, Settings(bandMeters: 9000, minBandMeters: 9000));

        partitions.Should().ContainSingle(partition => partition.IsFailed)
            .Which.FailureReason.Should().Contain("504");
    }

    [Fact]
    public async Task StreamAsync_WithATruncatedStream_DoesNotPropagateTheParsingFailure()
    {
        StubHttpMessageHandler handler = new(
            StubbedResponse.Ok(NoPeaks),
            StubbedResponse.Ok(TruncatedStream));

        Func<Task> streaming = () => PartitionsAsync(handler, Settings(bandMeters: 9000, minBandMeters: 9000));

        await streaming.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StreamAsync_WithATruncatedStream_SplitsTheBandInsteadOfFailingTheRun()
    {
        StubHttpMessageHandler handler = new(
            StubbedResponse.Ok(NoPeaks),
            StubbedResponse.Ok(TruncatedStream),
            StubbedResponse.Ok(NoPeaks),
            StubbedResponse.Ok(OnePeak),
            StubbedResponse.Ok(NoNames));

        List<PeakSourcePartition> partitions = await PartitionsAsync(handler, Settings(bandMeters: 9000));

        partitions.Should().NotContain(partition => partition.IsFailed);
        partitions.SelectMany(partition => partition.Records).Should().ContainSingle();
        handler.ReceivedQueries[2].Should().Contain("%3C+4500");
    }

    [Fact]
    public async Task StreamAsync_WithATruncatedStreamInBothPasses_ReportsOneFailedPartition()
    {
        StubHttpMessageHandler handler = new(
            StubbedResponse.Ok(NoPeaks),
            StubbedResponse.Ok(TruncatedStream),
            StubbedResponse.Ok(NoPeaks),
            StubbedResponse.Ok(TruncatedStream));

        List<PeakSourcePartition> partitions = await PartitionsAsync(
            handler, Settings(bandMeters: 9000, minBandMeters: 9000));

        partitions.Should().ContainSingle(partition => partition.IsFailed)
            .Which.FailureReason.Should().Contain("JsonException");
    }

    [Fact]
    public async Task StreamAsync_DoesNotSplitNorDeferOnANonRetriableStatus()
    {
        StubHttpMessageHandler handler = new(
            StubbedResponse.Ok(NoPeaks),
            StubbedResponse.Status(HttpStatusCode.BadRequest));

        List<PeakSourcePartition> partitions = await PartitionsAsync(handler, Settings(bandMeters: 9000));

        partitions.Should().ContainSingle(partition => partition.IsFailed)
            .Which.FailureReason.Should().Contain("400");
        handler.ReceivedQueries.Should().HaveCount(3);
    }

    [Fact]
    public async Task StreamAsync_WhenTheNamesBatchFails_DoesNotLoseThePeaksOnTheDeferredRetry()
    {
        StubHttpMessageHandler handler = new(
            StubbedResponse.Ok(OnePeak),
            StubbedResponse.Status(HttpStatusCode.TooManyRequests),
            StubbedResponse.Ok(NoPeaks),
            StubbedResponse.Ok(NoPeaks),
            StubbedResponse.Ok(OnePeak),
            StubbedResponse.Ok(NoNames));

        List<PeakSourcePartition> partitions = await PartitionsAsync(handler, Settings(bandMeters: 9000));

        partitions.SelectMany(partition => partition.Records).Should().ContainSingle()
            .Which.WikidataId.Should().Be("Q192580");
    }

    [Fact]
    public async Task StreamAsync_WithMaxRecordsReached_SkipsTheDeferredPass()
    {
        StubHttpMessageHandler handler = new(
            StubbedResponse.Status(HttpStatusCode.GatewayTimeout),
            StubbedResponse.Ok(OnePeak),
            StubbedResponse.Ok(NoNames));

        await PartitionsAsync(handler, Settings(bandMeters: 9000, minBandMeters: 9000, maxRecords: 1));

        handler.ReceivedQueries.Should().HaveCount(3);
    }

    [Fact]
    public async Task StreamAsync_CoversTheWholeElevationDomainWithOpenEndedBands()
    {
        StubHttpMessageHandler handler = new();

        await PartitionsAsync(handler, Settings(bandMeters: 9000));

        handler.ReceivedQueries.Should().HaveCount(3);
        handler.ReceivedQueries[0].Should().Contain("%3C+0").And.NotContain("%3E%3D");
        handler.ReceivedQueries[2].Should().Contain("%3E%3D+9000").And.NotContain("%26%26");
    }

    [Fact]
    public async Task StreamAsync_WithIncrementalCursor_UsesASingleUnboundedRequest()
    {
        StubHttpMessageHandler handler = StubHttpMessageHandler.WithBodies(OnePeak, NoNames);
        PeakSourceCursor cursor = PeakSourceCursor.Since(new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc));

        await PartitionsAsync(handler, Settings(), cursor);

        handler.ReceivedQueries.Should().HaveCount(2);
        handler.ReceivedQueries[0].Should().Contain("2026-07-01T00%3A00%3A00Z").And.NotContain("elevation+%3E%3D");
    }

    [Fact]
    public async Task StreamAsync_FallsBackToTheBandedSweepWhenTheUnboundedRequestTimesOut()
    {
        StubHttpMessageHandler handler = new(StubbedResponse.Status(HttpStatusCode.GatewayTimeout));
        PeakSourceCursor cursor = PeakSourceCursor.Since(new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc));

        List<PeakSourcePartition> partitions = await PartitionsAsync(handler, Settings(bandMeters: 9000), cursor);

        partitions.Should().NotContain(partition => partition.IsFailed);
        handler.ReceivedQueries.Should().HaveCount(4);
        handler.ReceivedQueries[1].Should().Contain("%3C+0");
    }

    [Fact]
    public async Task StreamAsync_WithoutCursor_DoesNotFilterByModificationDate()
    {
        StubHttpMessageHandler handler = StubHttpMessageHandler.WithBodies(OnePeak, NoNames);

        await PartitionsAsync(handler, Settings(bandMeters: 9000));

        handler.ReceivedQueries[0].Should().NotContain("xsd%3AdateTime");
    }

    [Fact]
    public async Task StreamAsync_RequestsOnlyTheConfiguredLanguages()
    {
        StubHttpMessageHandler handler = StubHttpMessageHandler.WithBodies(OnePeak, NoNames);

        await PartitionsAsync(handler, Settings(bandMeters: 9000, languages: ["es", "it"]));

        handler.ReceivedQueries[1].Should().Contain("%22es%22%2C%22it%22");
    }

    [Fact]
    public async Task StreamAsync_WithMaxRecords_StopsAtTheConfiguredBound()
    {
        StubHttpMessageHandler handler = new(
            StubbedResponse.Ok(OnePeak),
            StubbedResponse.Ok(NoNames),
            StubbedResponse.Ok(Matterhorn),
            StubbedResponse.Ok(NoNames));

        List<PeakSourceRecord> records = await RecordsAsync(handler, Settings(maxRecords: 1));

        records.Should().ContainSingle();
    }

    [Fact]
    public async Task StreamAsync_SendsTheConfiguredUserAgent()
    {
        StubHttpMessageHandler handler = StubHttpMessageHandler.WithBodies(OnePeak, NoNames);

        await PartitionsAsync(handler, Settings(bandMeters: 9000));

        handler.ReceivedUserAgents.Should().AllBe(UserAgent);
    }

    private static IngestionOptions Settings(
        double bandMeters = 9000,
        double minBandMeters = 15,
        int? maxRecords = null,
        IReadOnlyList<string>? languages = null) =>
        new()
        {
            MaxRecords = maxRecords,
            ElevationBandMeters = bandMeters,
            MinElevationBandMeters = minBandMeters,
            DelayBetweenRequests = TimeSpan.Zero,
            DeferredRetryDelay = TimeSpan.Zero,
            UserAgent = UserAgent,
            AlternativeNameLanguages = languages ?? ["es", "en"]
        };

    private static async Task<List<PeakSourceRecord>> RecordsAsync(
        StubHttpMessageHandler handler,
        IngestionOptions? settings = null)
    {
        List<PeakSourcePartition> partitions = await PartitionsAsync(handler, settings ?? Settings());

        return [.. partitions.SelectMany(partition => partition.Records)];
    }

    private static async Task<List<PeakSourcePartition>> PartitionsAsync(
        StubHttpMessageHandler handler,
        IngestionOptions settings,
        PeakSourceCursor? cursor = null)
    {
        using HttpClient httpClient = new(handler) { BaseAddress = settings.Endpoint };
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(settings.UserAgent);
        httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/sparql-results+json"));

        WikidataPeakSourceClient client = new(httpClient, Options.Create(settings));

        List<PeakSourcePartition> partitions = [];

        await foreach (PeakSourcePartition partition in
            client.StreamAsync(cursor ?? PeakSourceCursor.Full, CancellationToken.None))
        {
            partitions.Add(partition);
        }

        return partitions;
    }
}
