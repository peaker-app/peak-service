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
        foreach (PeakNameDraft name in draft.AlternativeNames)
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

    private bool SyncAlternativeNames(IReadOnlyList<PeakNameDraft> drafts)
    {
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

        if (string.IsNullOrWhiteSpace(source.Name) || source.Name.Length > MaxNameLength)
        {
            return Result.Failure(PeakErrors.NameInvalid);
        }

        if (source.Region is { Length: > MaxRegionLength })
        {
            return Result.Failure(PeakErrors.RegionTooLong);
        }

        if (source.SourceRevision is { Length: > MaxSourceRevisionLength })
        {
            return Result.Failure(PeakErrors.SourceRevisionTooLong);
        }

        return source.ImageUrl is { Length: > MaxImageUrlLength }
            ? Result.Failure(PeakErrors.ImageUrlTooLong)
            : Result.Success();
    }

    private bool HasChanges(PeakSourceData source) =>
        !string.Equals(Name, source.Name, StringComparison.Ordinal)
        || AltitudeMeters != source.AltitudeMeters
        || ProminenceMeters != source.ProminenceMeters
        || !Coordinates.Equals(source.Coordinates)
        || !string.Equals(CountryCode, source.CountryCode, StringComparison.Ordinal)
        || !string.Equals(Region, source.Region, StringComparison.Ordinal)
        || !string.Equals(SourceRevision, source.SourceRevision, StringComparison.Ordinal)
        || !string.Equals(ImageUrl, source.ImageUrl, StringComparison.Ordinal);

    private void Apply(PeakSourceData source)
    {
        WikidataId = source.WikidataId;
        Name = source.Name;
        AltitudeMeters = source.AltitudeMeters;
        ProminenceMeters = source.ProminenceMeters;
        Coordinates = source.Coordinates;
        CountryCode = source.CountryCode;
        Region = source.Region;
        SourceRevision = source.SourceRevision;
        ImageUrl = source.ImageUrl;
    }
}
