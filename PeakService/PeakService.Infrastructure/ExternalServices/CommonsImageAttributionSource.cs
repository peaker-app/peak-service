using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PeakService.Application.Abstractions;
using PeakService.Domain.Peaks;

namespace PeakService.Infrastructure.ExternalServices;

internal sealed class CommonsImageAttributionSource(
    HttpClient httpClient,
    ILogger<CommonsImageAttributionSource> logger) : IImageAttributionSource
{
    private const int BatchSize = 50;
    private const string ArtistKey = "Artist";
    private const string LicenseKey = "LicenseShortName";
    private const string LicenseUrlKey = "LicenseUrl";

    public async Task<IReadOnlyDictionary<string, PeakImageAttribution>> GetAsync(
        IReadOnlyCollection<string> imageUrls,
        CancellationToken cancellationToken)
    {
        Dictionary<string, string> titles = TitlesByUrl(imageUrls);
        Dictionary<string, PeakImageAttribution> attributions = new(StringComparer.Ordinal);

        foreach (string[] batch in titles.Values.Distinct(StringComparer.Ordinal).Chunk(BatchSize))
        {
            IReadOnlyDictionary<string, PeakImageAttribution> page =
                await FetchAsync(batch, cancellationToken);

            foreach ((string url, string title) in titles.Where(entry => page.ContainsKey(entry.Value)))
            {
                attributions[url] = page[title];
            }
        }

        return attributions;
    }

    private static Dictionary<string, string> TitlesByUrl(IReadOnlyCollection<string> imageUrls)
    {
        Dictionary<string, string> titles = new(StringComparer.Ordinal);

        foreach (string url in imageUrls)
        {
            if (CommonsFileTitle.From(url) is { } title)
            {
                titles[url] = title;
            }
        }

        return titles;
    }

    private async Task<IReadOnlyDictionary<string, PeakImageAttribution>> FetchAsync(
        string[] titles,
        CancellationToken cancellationToken)
    {
        try
        {
            CommonsImageInfoResponse? payload = await httpClient.GetFromJsonAsync<CommonsImageInfoResponse>(
                BuildQuery(titles), cancellationToken);

            return Read(payload);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or IOException
                                              or TaskCanceledException
                                          && !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                exception,
                "Attribution for {Count} Commons files could not be read; the photos stay without credit",
                titles.Length);

            return new Dictionary<string, PeakImageAttribution>(StringComparer.Ordinal);
        }
    }

    private static string BuildQuery(string[] titles) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"?action=query&format=json&formatversion=2&prop=imageinfo&iiprop=extmetadata%7Curl"
            + $"&titles={Uri.EscapeDataString(string.Join('|', titles))}");

    private static Dictionary<string, PeakImageAttribution> Read(CommonsImageInfoResponse? payload)
    {
        Dictionary<string, PeakImageAttribution> attributions = new(StringComparer.Ordinal);

        foreach (CommonsPage page in payload?.Query?.Pages ?? [])
        {
            if (page.Title is { } title && page.ImageInfo.Count > 0)
            {
                attributions[title] = ToAttribution(page.ImageInfo[0]);
            }
        }

        return attributions;
    }

    private static PeakImageAttribution ToAttribution(CommonsImageInfo info) =>
        new(
            HtmlText.ToPlainText(Metadata(info, ArtistKey), Peak.MaxImageAuthorLength),
            HtmlText.ToPlainText(Metadata(info, LicenseKey), Peak.MaxImageLicenseLength),
            Truncate(Metadata(info, LicenseUrlKey)),
            Truncate(info.DescriptionUrl));

    private static string? Metadata(CommonsImageInfo info, string key) =>
        info.ExtMetadata.TryGetValue(key, out CommonsMetadataValue? value) ? value.Value : null;

    private static string? Truncate(string? url) =>
        string.IsNullOrWhiteSpace(url) || url.Length > Peak.MaxImageUrlLength ? null : url;
}
