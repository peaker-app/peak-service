using System.Text.RegularExpressions;
using PeakService.Application.Abstractions;

namespace PeakService.Infrastructure.ExternalServices;

internal static partial class WikidataSparqlQuery
{
    private const string Prefixes =
        """
        PREFIX wd: <http://www.wikidata.org/entity/>
        PREFIX wdt: <http://www.wikidata.org/prop/direct/>
        PREFIX wikibase: <http://wikiba.se/ontology#>
        PREFIX bd: <http://www.bigdata.com/rdf#>
        PREFIX schema: <http://schema.org/>
        PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>
        PREFIX skos: <http://www.w3.org/2004/02/skos/core#>
        PREFIX xsd: <http://www.w3.org/2001/XMLSchema#>
        """;

    private const string CatalogTemplate =
        """
        SELECT ?item ?itemLabel ?elevation ?coord ?countryCode ?adminLabel ?modified WHERE {
          ?item wdt:P31/wdt:P279* wd:Q8502 ;
                wdt:P2044 ?elevation ;
                wdt:P625 ?coord ;
                schema:dateModified ?modified .
          {BAND_FILTER}
          {MODIFIED_FILTER}
          OPTIONAL { ?item wdt:P17 ?country . ?country wdt:P297 ?countryCode . }
          OPTIONAL { ?item wdt:P131 ?admin . }
          SERVICE <http://wikiba.se/ontology#label> { bd:serviceParam wikibase:language "{LANGUAGE},en". }
        }
        """;

    private const string NamesTemplate =
        """
        SELECT ?item ?name ?official WHERE {
          VALUES ?item { {ITEMS} }
          { ?item rdfs:label ?name . BIND("1" AS ?official) }
          UNION
          { ?item skos:altLabel ?name . BIND("0" AS ?official) }
          FILTER(LANG(?name) IN ({LANGUAGES}))
        }
        """;

    public static string BuildCatalog(IngestionOptions options, PeakSourceCursor cursor, ElevationBand band) =>
        Compose(CatalogTemplate
            .Replace("{BAND_FILTER}", band.FilterClause(), StringComparison.Ordinal)
            .Replace("{MODIFIED_FILTER}", BuildModifiedFilter(cursor.ModifiedSinceUtc), StringComparison.Ordinal)
            .Replace("{LANGUAGE}", options.Language, StringComparison.Ordinal));

    public static string BuildNames(IngestionOptions options, IEnumerable<string> wikidataIds) =>
        Compose(NamesTemplate
            .Replace("{ITEMS}", BuildItemList(wikidataIds), StringComparison.Ordinal)
            .Replace("{LANGUAGES}", BuildLanguageList(options.AlternativeNameLanguages), StringComparison.Ordinal));

    public static IEnumerable<string> ValidLanguages(IEnumerable<string> languages) =>
        languages.Where(language => LanguageTag().IsMatch(language));

    private static string Compose(string body) => $"{Prefixes}\n{body}";

    private static string BuildItemList(IEnumerable<string> wikidataIds) =>
        string.Join(' ', wikidataIds.Where(id => EntityId().IsMatch(id)).Select(id => $"wd:{id}"));

    private static string BuildLanguageList(IEnumerable<string> languages) =>
        string.Join(',', ValidLanguages(languages).Select(language => $"\"{language}\""));

    private static string BuildModifiedFilter(DateTime? modifiedSinceUtc) =>
        modifiedSinceUtc is null
            ? string.Empty
            : $"""FILTER(?modified >= "{modifiedSinceUtc.Value:yyyy-MM-ddTHH:mm:ssZ}"^^xsd:dateTime)""";

    [GeneratedRegex("^[a-zA-Z]{2,3}(-[a-zA-Z0-9]{2,8})?$")]
    private static partial Regex LanguageTag();

    [GeneratedRegex("^Q[1-9][0-9]*$")]
    private static partial Regex EntityId();
}
