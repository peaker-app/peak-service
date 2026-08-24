using Common.Application.Messaging;

namespace PeakService.Ingestion.RunPeakIngestion;

public sealed record RunPeakIngestionCommand(IngestionMode Mode) : ICommand<PeakIngestionRunResponse>;
