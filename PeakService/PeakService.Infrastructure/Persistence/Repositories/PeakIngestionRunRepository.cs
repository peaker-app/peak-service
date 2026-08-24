using Microsoft.EntityFrameworkCore;
using PeakService.Domain.PeakIngestionRuns;

namespace PeakService.Infrastructure.Persistence.Repositories;

internal sealed class PeakIngestionRunRepository(PeakDbContext context) : IPeakIngestionRunRepository
{
    public Task<PeakIngestionRun?> GetLastCompletedAsync(CancellationToken cancellationToken) =>
        context.PeakIngestionRuns
            .Where(run => run.Status == IngestionStatus.Completed)
            .OrderByDescending(run => run.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public void Add(PeakIngestionRun run) => context.PeakIngestionRuns.Add(run);
}
