namespace PeakService.Application.Abstractions;

public interface IPeakSourceClient
{
    IAsyncEnumerable<PeakSourcePartition> StreamAsync(PeakSourceCursor cursor, CancellationToken cancellationToken);
}
