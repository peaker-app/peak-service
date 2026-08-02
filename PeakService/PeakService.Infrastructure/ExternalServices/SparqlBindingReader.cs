using System.Globalization;
using PeakService.Application.Abstractions;
using PeakService.Domain.Peaks;

namespace PeakService.Infrastructure.ExternalServices;

internal static class SparqlBindingReader
{
#pragma warning disable S5332 // Motivo: son los IRI que Wikidata devuelve literalmente en sus bindings;
    // solo se usan como prefijo para reconocerlos, nunca como endpoint. Con https no casaría nada.
    private const string EntityPrefix = "http://www.wikidata.org/entity/";
    private const string CommonsPrefix = "http://commons.wikimedia.org/";
#pragma warning restore S5332
    private const string SecureCommonsPrefix = "https://commons.wikimedia.org/";
    private const string PointPrefix = "Point(";

    public static PeakSourceRecord? ToRecord(Dictionary<string, SparqlBinding> binding)
    {
        string? wikidataId = ReadEntityId(binding, "item");
        string? name = Read(binding, "itemLabel");

        if (wikidataId is null || string.IsNullOrWhiteSpace(name) || !TryReadPoint(binding, out double latitude, out double longitude))
        {
            return null;
        }

        return new PeakSourceRecord(
            wikidataId,
            name,
            ReadAltitude(binding),
            ProminenceMeters: null,
            latitude,
            longitude,
            Read(binding, "countryCode")?.ToUpperInvariant(),
            Read(binding, "adminLabel"),
            Read(binding, "modified"),
            ReadImageUrl(binding));
    }

    public static (string WikidataId, PeakNameDraft Name)? ToNameDraft(Dictionary<string, SparqlBinding> binding)
    {
        string? wikidataId = ReadEntityId(binding, "item");
        string? name = Read(binding, "name");
        string? language = binding.TryGetValue("name", out SparqlBinding? value) ? value.Language : null;

        return wikidataId is null || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(language)
            ? null
            : (wikidataId, new PeakNameDraft(language, name, Read(binding, "official") == "1"));
    }

    public static string? ReadEntityId(Dictionary<string, SparqlBinding> binding, string key)
    {
        string? uri = Read(binding, key);

        return uri is null || !uri.StartsWith(EntityPrefix, StringComparison.Ordinal)
            ? null
            : uri[EntityPrefix.Length..];
    }

    private static string? ReadImageUrl(Dictionary<string, SparqlBinding> binding)
    {
        string? url = Read(binding, "image");

        if (url is null)
        {
            return null;
        }

        string secure = url.StartsWith(CommonsPrefix, StringComparison.Ordinal)
            ? string.Concat(SecureCommonsPrefix, url[CommonsPrefix.Length..])
            : url;

        return secure.Length > Peak.MaxImageUrlLength ? null : secure;
    }

    private static int ReadAltitude(Dictionary<string, SparqlBinding> binding) =>
        double.TryParse(Read(binding, "elevation"), NumberStyles.Float, CultureInfo.InvariantCulture, out double meters)
            ? (int)Math.Round(meters)
            : 0;

    private static bool TryReadPoint(
        Dictionary<string, SparqlBinding> binding,
        out double latitude,
        out double longitude)
    {
        latitude = 0;
        longitude = 0;

        string? point = Read(binding, "coord");

        if (point is null || !point.StartsWith(PointPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string[] parts = point[PointPrefix.Length..].TrimEnd(')').Split(' ');

        return parts.Length == 2
            && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out longitude)
            && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out latitude);
    }

    private static string? Read(Dictionary<string, SparqlBinding> binding, string key) =>
        binding.TryGetValue(key, out SparqlBinding? value) && !string.IsNullOrWhiteSpace(value.Value)
            ? value.Value
            : null;
}
