using System.Globalization;
using System.Text.RegularExpressions;

namespace PeakService.Infrastructure.ExternalServices;

internal static partial class WikidataSparqlQuery
{
    public const string NameSeparator = "||";
    public const char NameFieldSeparator = '~';

    private const string Template =
        """
        SELECT ?item ?itemLabel ?elevation ?coord ?countryCode ?adminLabel ?modified
               (GROUP_CONCAT(DISTINCT ?nameEntry; separator="{NAME_SEPARATOR}") AS ?names) WHERE {
          ?item wdt:P31/wdt:P279* wd:Q8502.
          ?item wdt:P2044 ?elevation.
          ?item wdt:P625 ?coord.
          ?item schema:dateModified ?modified.
          OPTIONAL { ?item wdt:P17 ?country. ?country wdt:P297 ?countryCode. }
          OPTIONAL { ?item wdt:P131 ?admin. }
          OPTIONAL {
            { ?item rdfs:label ?name. BIND("1" AS ?official) }
            UNION
            { ?item skos:altLabel ?name. BIND("0" AS ?official) }
            FILTER(LANG(?name) IN ({LANGUAGES}))
            BIND(CONCAT(LANG(?name), "{FIELD_SEPARATOR}", ?official, "{FIELD_SEPARATOR}", STR(?name)) AS ?nameEntry)
          }
          {FILTER}
          SERVICE wikibase:label { bd:serviceParam wikibase:language "{LANGUAGE}",en. }
        }
        GROUP BY ?item ?itemLabel ?elevation ?coord ?countryCode ?adminLabel ?modified
        ORDER BY ?item
        LIMIT {LIMIT}
        OFFSET {OFFSET}
        """;

    public static string Build(IngestionOptions options, DateTime? modifiedSinceUtc, SparqlPage page) =>
        Template
            .Replace("{NAME_SEPARATOR}", NameSeparator, StringComparison.Ordinal)
            .Replace("{FIELD_SEPARATOR}", NameFieldSeparator.ToString(), StringComparison.Ordinal)
            .Replace("{LANGUAGES}", BuildLanguageList(options.AlternativeNameLanguages), StringComparison.Ordinal)
            .Replace("{FILTER}", BuildFilter(modifiedSinceUtc), StringComparison.Ordinal)
            .Replace("{LANGUAGE}", options.Language, StringComparison.Ordinal)
            .Replace("{LIMIT}", page.Take.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{OFFSET}", page.Offset.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);

    private static string BuildLanguageList(IEnumerable<string> languages) =>
        string.Join(",", languages.Where(IsLanguageTag).Select(language => $"\"{language}\""));

    private static bool IsLanguageTag(string language) => LanguageTag().IsMatch(language);

    private static string BuildFilter(DateTime? modifiedSinceUtc) =>
        modifiedSinceUtc is null
            ? string.Empty
            : $"""FILTER(?modified >= "{modifiedSinceUtc.Value:yyyy-MM-ddTHH:mm:ssZ}"^^xsd:dateTime)""";

    [GeneratedRegex("^[a-zA-Z]{2,3}(-[a-zA-Z0-9]{2,8})?$")]
    private static partial Regex LanguageTag();
}
