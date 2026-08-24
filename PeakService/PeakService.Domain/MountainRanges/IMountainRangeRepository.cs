namespace PeakService.Domain.MountainRanges;

public interface IMountainRangeRepository
{
    Task<MountainRange?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
