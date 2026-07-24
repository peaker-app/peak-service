using Common.Application.Messaging;

namespace PeakService.Application.Ingestion.RunPeakIngestion;

public sealed record RunPeakIngestionCommand(IngestionMode Mode) : ICommand<PeakIngestionRunResponse>;
