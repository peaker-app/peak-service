namespace PeakService.Domain.Peaks;

public interface IPeakRepository
{
    Task<Peak?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Peak?> GetByWikidataIdAsync(string wikidataId, CancellationToken cancellationToken);

    void Add(Peak peak);
}
