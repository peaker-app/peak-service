namespace PeakService.Application.Abstractions;

public interface IPeakSourceClient
{
    IAsyncEnumerable<PeakSourceRecord> StreamAsync(PeakSourceCursor cursor, CancellationToken cancellationToken);
}
