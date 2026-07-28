using PeakService.Domain.Peaks;

namespace PeakService.Ingestion.RunPeakIngestion;

internal sealed record IngestedPeak(PeakSourceData Source, IReadOnlyList<PeakNameDraft> AlternativeNames);
