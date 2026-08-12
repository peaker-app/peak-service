using PeakService.Domain.Peaks;

namespace PeakService.Application.Abstractions;

public interface IImageAttributionSource
{
    Task<IReadOnlyDictionary<string, PeakImageAttribution>> GetAsync(
        IReadOnlyCollection<string> imageUrls,
        CancellationToken cancellationToken);
}
