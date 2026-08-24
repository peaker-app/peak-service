using Common.Domain.Results;
using MediatR;
using Microsoft.Extensions.Options;
using PeakService.Ingestion.RunPeakIngestion;

namespace PeakService.Ingestion;

internal sealed class PeakIngestionWorker(
    IServiceScopeFactory scopeFactory,
    IHostApplicationLifetime lifetime,
    IOptions<PeakIngestionWorkerOptions> options,
    ILogger<PeakIngestionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

            Result<PeakIngestionRunResponse> result = await sender.Send(
                new RunPeakIngestionCommand(options.Value.Mode), stoppingToken);

            Report(result);
        }
        finally
        {
            lifetime.StopApplication();
        }
    }

    private void Report(Result<PeakIngestionRunResponse> result)
    {
        if (result.IsFailure)
        {
            logger.LogError("Peak ingestion could not run: {ErrorCode} {ErrorDescription}",
                result.Error.Code, result.Error.Description);
            Environment.ExitCode = 1;

            return;
        }

        PeakIngestionRunResponse run = result.Value;

        logger.LogInformation(
            "Peak ingestion {RunId} ended as {Status}: {Created} created, {Updated} updated, {Failed} failed",
            run.RunId, run.Status, run.PeaksCreated, run.PeaksUpdated, run.PeaksFailed);

        if (run.Error is not null)
        {
            Environment.ExitCode = 1;
        }
    }
}
