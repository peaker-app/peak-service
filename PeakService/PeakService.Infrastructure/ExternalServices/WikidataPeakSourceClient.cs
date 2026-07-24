using System.Globalization;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using PeakService.Application.Abstractions;
using PeakService.Domain.Peaks;

namespace PeakService.Infrastructure.ExternalServices;

internal sealed class WikidataPeakSourceClient(HttpClient httpClient, IOptions<IngestionOptions> options)
    : IPeakSourceClient
{
    private const string EntityPrefix = "http://www.wikidata.org/entity/";
    private const string PointPrefix = "Point(";

    public async IAsyncEnumerable<PeakSourceRecord> StreamAsync(
        PeakSourceCursor cursor,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        IngestionOptions settings = options.Value;

        for (int offset = 0; ; offset += settings.PageSize)
        {
            SparqlPage page = new(offset, PageTake(offset, settings));

            if (page.Take == 0)
            {
                yield break;
            }

            IReadOnlyList<PeakSourceRecord> records = await FetchAsync(cursor, page, cancellationToken);

            foreach (PeakSourceRecord record in records)
            {
                yield return record;
            }

            if (records.Count < page.Take)
            {
                yield break;
            }

            await Task.Delay(settings.DelayBetweenPages, cancellationToken);
        }
    }

    private static int PageTake(int offset, IngestionOptions settings) =>
        settings.MaxRecords is { } max
            ? Math.Clamp(max - offset, 0, settings.PageSize)
            : settings.PageSize;

    private async Task<IReadOnlyList<PeakSourceRecord>> FetchAsync(
        PeakSourceCursor cursor,
        SparqlPage page,
        CancellationToken cancellationToken)
    {
        string query = WikidataSparqlQuery.Build(options.Value, cursor.ModifiedSinceUtc, page);

        using FormUrlEncodedContent content = new([new KeyValuePair<string, string>("query", query)]);
        using HttpResponseMessage response = await httpClient.PostAsync((Uri?)null, content, cancellationToken);

        response.EnsureSuccessStatusCode();

        SparqlResponse? payload = await response.Content
            .ReadFromJsonAsync<SparqlResponse>(cancellationToken);

        return [.. (payload?.Results?.Bindings ?? []).Select(ToRecord).OfType<PeakSourceRecord>()];
    }

    private static PeakSourceRecord? ToRecord(Dictionary<string, SparqlBinding> binding)
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
            Read(binding, "modified"))
        {
            AlternativeNames = ReadAlternativeNames(binding, name)
        };
    }

    private static IReadOnlyList<PeakNameDraft> ReadAlternativeNames(
        Dictionary<string, SparqlBinding> binding,
        string canonicalName)
    {
        string? names = Read(binding, "names");

        if (names is null)
        {
            return [];
        }

        return
        [
            .. names.Split(WikidataSparqlQuery.NameSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Select(ToNameDraft)
                .OfType<PeakNameDraft>()
                .Where(draft => !string.Equals(draft.Name, canonicalName, StringComparison.OrdinalIgnoreCase))
                .DistinctBy(draft => (draft.LanguageCode, draft.Name))
        ];
    }

    private static PeakNameDraft? ToNameDraft(string entry)
    {
        string[] parts = entry.Split(WikidataSparqlQuery.NameFieldSeparator, 3);

        return parts.Length == 3 && !string.IsNullOrWhiteSpace(parts[2])
            ? new PeakNameDraft(parts[0], parts[2], parts[1] == "1")
            : null;
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

    private static string? ReadEntityId(Dictionary<string, SparqlBinding> binding, string key)
    {
        string? uri = Read(binding, key);

        return uri is null || !uri.StartsWith(EntityPrefix, StringComparison.Ordinal)
            ? null
            : uri[EntityPrefix.Length..];
    }

    private static string? Read(Dictionary<string, SparqlBinding> binding, string key) =>
        binding.TryGetValue(key, out SparqlBinding? value) && !string.IsNullOrWhiteSpace(value.Value)
            ? value.Value
            : null;
}
