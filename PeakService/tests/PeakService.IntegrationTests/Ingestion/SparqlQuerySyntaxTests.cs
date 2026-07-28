using FluentAssertions;
using PeakService.Application.Abstractions;
using PeakService.Infrastructure.ExternalServices;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using Xunit;

namespace PeakService.IntegrationTests.Ingestion;

public sealed class SparqlQuerySyntaxTests
{
    private static readonly SparqlQueryParser Parser = new();

    [Theory]
    [InlineData(null, 0d)]
    [InlineData(0d, 250d)]
    [InlineData(3000d, 3125d)]
    [InlineData(9000d, null)]
    [InlineData(null, null)]
    public void BuildCatalog_ForEveryBandShape_ProducesParseableSparql(double? from, double? to)
    {
        string query = WikidataSparqlQuery.BuildCatalog(Options(), PeakSourceCursor.Full, new ElevationBand(from, to));

        Parsing(query).Should().NotThrow();
    }

    [Fact]
    public void BuildCatalog_WithIncrementalCursor_ProducesParseableSparql()
    {
        PeakSourceCursor cursor = PeakSourceCursor.Since(new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc));

        string query = WikidataSparqlQuery.BuildCatalog(Options(), cursor, ElevationBand.Unbounded);

        Parsing(query).Should().NotThrow();
        query.Should().Contain("2026-07-01T00:00:00Z");
    }

    [Fact]
    public void BuildCatalog_KeepsTheLabelServiceLanguageListInsideTheLiteral()
    {
        string query = WikidataSparqlQuery.BuildCatalog(Options(), PeakSourceCursor.Full, ElevationBand.Unbounded);

        query.Should().Contain("""wikibase:language "en,en".""");
        query.Should().NotContain("""wikibase:language "en",en.""");
    }

    [Fact]
    public void BuildCatalog_DoesNotOrderOrPaginate()
    {
        string query = WikidataSparqlQuery.BuildCatalog(Options(), PeakSourceCursor.Full, ElevationBand.Unbounded);

        query.Should().NotContain("ORDER BY");
        query.Should().NotContain("OFFSET");
        query.Should().NotContain("GROUP BY");
    }

    [Fact]
    public void BuildNames_ProducesParseableSparql()
    {
        string query = WikidataSparqlQuery.BuildNames(Options(), ["Q192580", "Q1291"]);

        Parsing(query).Should().NotThrow();
        query.Should().Contain("wd:Q192580 wd:Q1291");
    }

    [Fact]
    public void BuildNames_WithASingleLanguage_ProducesParseableSparql()
    {
        string query = WikidataSparqlQuery.BuildNames(Options(["eu"]), ["Q192580"]);

        Parsing(query).Should().NotThrow();
    }

    [Fact]
    public void BuildNames_IgnoresEntityIdentifiersThatAreNotWikidataItems()
    {
        string query = WikidataSparqlQuery.BuildNames(Options(), ["Q192580", "'; DROP TABLE peaks; --", "P625"]);

        Parsing(query).Should().NotThrow();
        query.Should().Contain("wd:Q192580");
        query.Should().NotContain("DROP TABLE");
    }

    private static Action Parsing(string query) => () => Parser.ParseFromString(query);

    private static IngestionOptions Options(IReadOnlyList<string>? languages = null) =>
        new() { AlternativeNameLanguages = languages ?? ["es", "en", "fr", "it", "de", "ca", "eu", "gl", "pt"] };
}
