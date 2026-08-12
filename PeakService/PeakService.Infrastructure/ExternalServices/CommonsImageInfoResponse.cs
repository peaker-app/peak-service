using System.Text.Json.Serialization;

namespace PeakService.Infrastructure.ExternalServices;

internal sealed record CommonsImageInfoResponse
{
    [JsonPropertyName("query")]
    public CommonsQuery? Query { get; init; }
}

internal sealed record CommonsQuery
{
    [JsonPropertyName("pages")]
    public IReadOnlyList<CommonsPage> Pages { get; init; } = [];
}

internal sealed record CommonsPage
{
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("imageinfo")]
    public IReadOnlyList<CommonsImageInfo> ImageInfo { get; init; } = [];
}

internal sealed record CommonsImageInfo
{
    [JsonPropertyName("descriptionurl")]
    public string? DescriptionUrl { get; init; }

    [JsonPropertyName("extmetadata")]
    public IReadOnlyDictionary<string, CommonsMetadataValue> ExtMetadata { get; init; } =
        new Dictionary<string, CommonsMetadataValue>(StringComparer.Ordinal);
}

internal sealed record CommonsMetadataValue
{
    [JsonPropertyName("value")]
    public string? Value { get; init; }
}
