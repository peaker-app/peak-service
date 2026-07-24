using PeakService.Domain.Peaks;

namespace PeakService.Application.Ingestion.RunPeakIngestion;

internal sealed record IngestedPeak(PeakSourceData Source, IReadOnlyList<PeakNameDraft> AlternativeNames);
