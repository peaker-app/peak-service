namespace PeakService.Ingestion.RunPeakIngestion;

public sealed record PeakIngestionRunResponse(
    Guid RunId,
    string Status,
    int PeaksCreated,
    int PeaksUpdated,
    int PeaksUnchanged,
    int PeaksFailed,
    DateTime StartedAtUtc,
    DateTime? FinishedAtUtc,
    string? Error);
