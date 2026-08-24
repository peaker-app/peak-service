using System.Text.Json.Serialization;

namespace PeakService.Infrastructure.ExternalServices;

internal sealed record SparqlResponse(SparqlResults? Results);

internal sealed record SparqlResults(IReadOnlyList<Dictionary<string, SparqlBinding>>? Bindings);

internal sealed record SparqlBinding(string? Value)
{
    [JsonPropertyName("xml:lang")]
    public string? Language { get; init; }
}
