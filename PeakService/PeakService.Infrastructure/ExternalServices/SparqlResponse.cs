namespace PeakService.Infrastructure.ExternalServices;

internal sealed record SparqlResponse(SparqlResults? Results);

internal sealed record SparqlResults(IReadOnlyList<Dictionary<string, SparqlBinding>>? Bindings);

internal sealed record SparqlBinding(string? Value);
