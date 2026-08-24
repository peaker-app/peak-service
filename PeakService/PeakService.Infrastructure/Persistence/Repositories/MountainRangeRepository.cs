using Microsoft.EntityFrameworkCore;
using PeakService.Domain.MountainRanges;

namespace PeakService.Infrastructure.Persistence.Repositories;

internal sealed class MountainRangeRepository(PeakDbContext context) : IMountainRangeRepository
{
    public Task<MountainRange?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.MountainRanges.FirstOrDefaultAsync(range => range.Id == id, cancellationToken);
}
