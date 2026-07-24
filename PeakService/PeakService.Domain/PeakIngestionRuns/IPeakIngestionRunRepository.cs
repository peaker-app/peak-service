namespace PeakService.Domain.PeakIngestionRuns;

public interface IPeakIngestionRunRepository
{
    Task<PeakIngestionRun?> GetLastCompletedAsync(CancellationToken cancellationToken);

    void Add(PeakIngestionRun run);
}
