namespace PeakService.Domain.Peaks;

public sealed record PeakDraft(
    PeakSourceData Source,
    Guid? RangeId,
    IReadOnlyList<PeakNameDraft> AlternativeNames);
