using Microsoft.EntityFrameworkCore;
using PeakService.Domain.Peaks;

namespace PeakService.Infrastructure.Persistence.Repositories;

internal sealed class PeakRepository(PeakDbContext context) : IPeakRepository
{
    public Task<Peak?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Peaks.FirstOrDefaultAsync(peak => peak.Id == id, cancellationToken);

    public Task<Peak?> GetByWikidataIdAsync(string wikidataId, CancellationToken cancellationToken) =>
        context.Peaks.FirstOrDefaultAsync(peak => peak.WikidataId == wikidataId, cancellationToken);

    public void Add(Peak peak) => context.Peaks.Add(peak);
}
