using PeakService.Domain.Peaks;

namespace PeakService.Application.Abstractions;

public interface IPeakDeduplicationStrategy
{
    Task<PeakMatch?> FindMatchAsync(PeakSourceData candidate, CancellationToken cancellationToken);
}
