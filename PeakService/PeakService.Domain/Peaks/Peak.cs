using Common.Domain.Abstractions;
using Common.Domain.Results;
using PeakService.Domain.Peaks.Events;

namespace PeakService.Domain.Peaks;

public sealed class Peak : AggregateRoot
{
    public const int MaxWikidataIdLength = 20;
    public const int MaxNameLength = 200;
    public const int MaxRegionLength = 120;
    public const int MaxSourceRevisionLength = 50;
    public const int MaxImageUrlLength = 500;
    public const int MaxImageAuthorLength = 200;
    public const int MaxImageLicenseLength = 60;

    private readonly List<PeakName> _alternativeNames = [];

    private Peak()
    {
    }

    private Peak(Guid id, PeakDraft draft) : base(id)
    {
        Apply(draft.Source);
        RangeId = draft.RangeId;
    }

    public string WikidataId { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public int AltitudeMeters { get; private set; }

    public int? ProminenceMeters { get; private set; }

    public Coordinates Coordinates { get; private set; } = null!;

    public string? CountryCode { get; private set; }

    public string? Region { get; private set; }

    public string? SourceRevision { get; private set; }

    public string? ImageUrl { get; private set; }

    public string? ImageAuthor { get; private set; }

    public string? ImageLicense { get; private set; }

    public string? ImageLicenseUrl { get; private set; }

    public string? ImageCreditUrl { get; private set; }

    public PeakImageAttribution Attribution =>
        new(ImageAuthor, ImageLicense, ImageLicenseUrl, ImageCreditUrl);

    public Guid? RangeId { get; private set; }

    public IReadOnlyCollection<PeakName> AlternativeNames => _alternativeNames.AsReadOnly();

    public static Result<Peak> Create(PeakDraft draft)
    {
        Result validation = Validate(draft.Source);
        if (validation.IsFailure)
        {
            return Result.Failure<Peak>(validation.Error);
        }

        Peak peak = new(Guid.CreateVersion7(), draft);
        foreach (PeakNameDraft name in draft.AlternativeNames.Where(PeakName.IsAcceptable))
        {
            peak._alternativeNames.Add(PeakName.Create(name.LanguageCode, name.Name, name.IsOfficial));
        }

        peak.Raise(new PeakCreatedDomainEvent(peak.Id, peak.Name, peak.AltitudeMeters, peak.CountryCode));

        return peak;
    }

    public Result<PeakUpdateOutcome> UpdateFromSource(
        PeakSourceData source,
        IReadOnlyList<PeakNameDraft> alternativeNames)
    {
        Result validation = Validate(source);
        if (validation.IsFailure)
        {
            return Result.Failure<PeakUpdateOutcome>(validation.Error);
        }

        bool dataChanged = HasChanges(source);
        bool namesChanged = SyncAlternativeNames(alternativeNames);

        if (!dataChanged && !namesChanged)
        {
            return PeakUpdateOutcome.Unchanged;
        }

        bool renamed = !string.Equals(Name, source.Name, StringComparison.Ordinal);

        Apply(source);
        Raise(new PeakUpdatedDomainEvent(Id, Name, AltitudeMeters, CountryCode));

        if (renamed)
        {
            Raise(new PeakRenamedDomainEvent(Id, Name, AltitudeMeters, CountryCode));
        }

        return PeakUpdateOutcome.Updated;
    }

    private bool SyncAlternativeNames(IReadOnlyList<PeakNameDraft> candidates)
    {
        List<PeakNameDraft> drafts = [.. candidates.Where(PeakName.IsAcceptable)];

        HashSet<(string LanguageCode, string Name)> incoming =
            [.. drafts.Select(draft => (draft.LanguageCode.Trim(), draft.Name.Trim()))];

        int removed = _alternativeNames.RemoveAll(name => !incoming.Contains((name.LanguageCode, name.Name)));

        HashSet<(string LanguageCode, string Name)> current =
            [.. _alternativeNames.Select(name => (name.LanguageCode, name.Name))];

        List<PeakNameDraft> missing = [.. drafts.Where(draft =>
            current.Add((draft.LanguageCode.Trim(), draft.Name.Trim())))];

        foreach (PeakNameDraft draft in missing)
        {
            _alternativeNames.Add(PeakName.Create(draft.LanguageCode, draft.Name, draft.IsOfficial));
        }

        return removed > 0 || missing.Count > 0;
    }

    private static Result Validate(PeakSourceData source)
    {
        if (string.IsNullOrWhiteSpace(source.WikidataId) || source.WikidataId.Length > MaxWikidataIdLength)
        {
            return Result.Failure(PeakErrors.WikidataIdInvalid);
        }

        Result text = ValidateText(source);

        return text.IsFailure ? text : ValidateOrigin(source);
    }

    private static Result ValidateText(PeakSourceData source)
    {
        string? name = PeakText.Normalize(source.Name);

        if (string.IsNullOrEmpty(name) || name.Length > MaxNameLength || !PeakText.IsPrintable(name))
        {
            return Result.Failure(PeakErrors.NameInvalid);
        }

        string? region = PeakText.Normalize(source.Region);

        if (region is { Length: > MaxRegionLength })
        {
            return Result.Failure(PeakErrors.RegionTooLong);
        }

        return PeakText.IsPrintable(region) ? Result.Success() : Result.Failure(PeakErrors.RegionInvalid);
    }

    private static Result ValidateOrigin(PeakSourceData source)
    {
        if (source.SourceRevision is { Length: > MaxSourceRevisionLength })
        {
            return Result.Failure(PeakErrors.SourceRevisionTooLong);
        }

        if (source.ImageUrl is { Length: > MaxImageUrlLength })
        {
            return Result.Failure(PeakErrors.ImageUrlTooLong);
        }

        if (source.ImageUrl is not null && !IsSecureAbsoluteUrl(source.ImageUrl))
        {
            return Result.Failure(PeakErrors.ImageUrlInvalid);
        }

        return source.Attribution.IsWithinLimits()
            ? Result.Success()
            : Result.Failure(PeakErrors.ImageAttributionTooLong);
    }

    private static bool IsSecureAbsoluteUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out Uri? parsed) && parsed.Scheme == Uri.UriSchemeHttps;

    private bool HasChanges(PeakSourceData source) =>
        !string.Equals(Name, PeakText.Normalize(source.Name), StringComparison.Ordinal)
        || AltitudeMeters != source.AltitudeMeters
        || ProminenceMeters != source.ProminenceMeters
        || !Coordinates.Equals(source.Coordinates)
        || !string.Equals(CountryCode, source.CountryCode, StringComparison.Ordinal)
        || !string.Equals(Region, PeakText.Normalize(source.Region), StringComparison.Ordinal)
        || !string.Equals(SourceRevision, source.SourceRevision, StringComparison.Ordinal)
        || !string.Equals(ImageUrl, source.ImageUrl, StringComparison.Ordinal)
        || Attribution != AttributionOf(source);

    private void Apply(PeakSourceData source)
    {
        WikidataId = source.WikidataId;
        Name = PeakText.Normalize(source.Name)!;
        AltitudeMeters = source.AltitudeMeters;
        ProminenceMeters = source.ProminenceMeters;
        Coordinates = source.Coordinates;
        CountryCode = source.CountryCode;
        Region = PeakText.Normalize(source.Region);
        SourceRevision = source.SourceRevision;
        ImageUrl = source.ImageUrl;
        ApplyAttribution(source);
    }

    private void ApplyAttribution(PeakSourceData source)
    {
        PeakImageAttribution attribution = AttributionOf(source);

        ImageAuthor = attribution.Author;
        ImageLicense = attribution.License;
        ImageLicenseUrl = attribution.LicenseUrl;
        ImageCreditUrl = attribution.CreditUrl;
    }

    private static PeakImageAttribution AttributionOf(PeakSourceData source) =>
        source.ImageUrl is null ? PeakImageAttribution.None : source.Attribution.Normalized();
}
