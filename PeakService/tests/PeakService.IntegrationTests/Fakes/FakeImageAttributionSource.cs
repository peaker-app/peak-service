using PeakService.Application.Abstractions;
using PeakService.Domain.Peaks;

namespace PeakService.IntegrationTests.Fakes;

internal sealed class FakeImageAttributionSource : IImageAttributionSource
{
    private readonly Dictionary<string, PeakImageAttribution> _attributions = new(StringComparer.Ordinal);

    public List<string> ReceivedUrls { get; } = [];

    public void Register(string imageUrl, PeakImageAttribution attribution) =>
        _attributions[imageUrl] = attribution;

    public Task<IReadOnlyDictionary<string, PeakImageAttribution>> GetAsync(
        IReadOnlyCollection<string> imageUrls,
        CancellationToken cancellationToken)
    {
        ReceivedUrls.AddRange(imageUrls);

        IReadOnlyDictionary<string, PeakImageAttribution> found = _attributions
            .Where(entry => imageUrls.Contains(entry.Key))
            .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);

        return Task.FromResult(found);
    }
}
