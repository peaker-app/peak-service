using System.ComponentModel.DataAnnotations;

namespace PeakService.Infrastructure.ExternalServices;

public sealed class IngestionOptions : IValidatableObject
{
    public const string SectionName = "Ingestion";

    [Required]
    public Uri Endpoint { get; init; } = new("https://query.wikidata.org/sparql");

    [Required]
    public string UserAgent { get; init; } = "PeakerIngestion/1.0 (https://github.com/peaker-app)";

    public TimeSpan DelayBetweenRequests { get; init; } = TimeSpan.FromSeconds(2);

    public TimeSpan DeferredRetryDelay { get; init; } = TimeSpan.FromMinutes(1);

    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromMinutes(2);

    [Range(1, int.MaxValue)]
    public int? MaxRecords { get; init; }

    [Range(1, 10000)]
    public int NameBatchSize { get; init; } = 500;

    [Range(1, 10000)]
    public double ElevationBandMeters { get; init; } = 250;

    [Range(1, 10000)]
    public double MinElevationBandMeters { get; init; } = 15;

    [Range(-500, 10000)]
    public double MinElevationMeters { get; init; }

    [Range(-500, 10000)]
    public double MaxElevationMeters { get; init; } = 9000;

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

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!WikidataSparqlQuery.ValidLanguages(AlternativeNameLanguages).Any())
        {
            yield return new ValidationResult(
                "At least one entry of AlternativeNameLanguages must be a valid language tag.",
                [nameof(AlternativeNameLanguages)]);
        }

        if (MaxElevationMeters <= MinElevationMeters)
        {
            yield return new ValidationResult(
                "MaxElevationMeters must be greater than MinElevationMeters.",
                [nameof(MaxElevationMeters)]);
        }

        if (MinElevationBandMeters > ElevationBandMeters)
        {
            yield return new ValidationResult(
                "MinElevationBandMeters must not exceed ElevationBandMeters.",
                [nameof(MinElevationBandMeters)]);
        }
    }
}
