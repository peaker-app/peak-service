using System.ComponentModel.DataAnnotations;

namespace PeakService.Infrastructure.ExternalServices;

public sealed class IngestionOptions
{
    public const string SectionName = "Ingestion";

    [Required]
    public Uri Endpoint { get; init; } = new("https://query.wikidata.org/sparql");

    [Required]
    public string UserAgent { get; init; } = "PeakerIngestion/1.0 (https://github.com/peaker-app)";

    [Range(1, 10000)]
    public int PageSize { get; init; } = 500;

    public TimeSpan DelayBetweenPages { get; init; } = TimeSpan.FromSeconds(2);

    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromMinutes(2);

    [Range(1, int.MaxValue)]
    public int? MaxRecords { get; init; }

    [Required]
    [RegularExpression("^[a-zA-Z]{2,3}(-[a-zA-Z0-9]{2,8})?$")]
    public string Language { get; init; } = "en";

    [Required]
    [MinLength(1)]
    public IReadOnlyList<string> AlternativeNameLanguages { get; init; } =
        ["es", "en", "fr", "it", "de", "ca", "eu", "gl", "pt"];

    [Range(0, 100000)]
    public double DuplicateRadiusMeters { get; init; } = 250;

    [Range(0, 1)]
    public double DuplicateNameSimilarity { get; init; } = 0.6;
}
