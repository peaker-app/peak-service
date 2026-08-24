using PeakService.Ingestion.RunPeakIngestion;

namespace PeakService.Ingestion;

public sealed class PeakIngestionWorkerOptions
{
    public const string SectionName = "IngestionWorker";

    public IngestionMode Mode { get; init; } = IngestionMode.Automatic;
}
