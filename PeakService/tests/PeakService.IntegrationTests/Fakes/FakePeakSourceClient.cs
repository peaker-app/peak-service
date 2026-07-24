using PeakService.Application.Abstractions;

namespace PeakService.IntegrationTests.Fakes;

public sealed class FakePeakSourceClient : IPeakSourceClient
{
    private IReadOnlyList<PeakSourceRecord> _records = [];
    private Exception? _failure;

    public PeakSourceCursor? LastCursor { get; private set; }

    public void Returns(params PeakSourceRecord[] records)
    {
        _records = records;
        _failure = null;
    }

    public void Fails(Exception failure)
    {
        _failure = failure;
        _records = [];
    }

    public async IAsyncEnumerable<PeakSourceRecord> StreamAsync(
        PeakSourceCursor cursor,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        LastCursor = cursor;

        if (_failure is not null)
        {
            throw _failure;
        }

        await Task.CompletedTask;

        foreach (PeakSourceRecord record in _records)
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return record;
        }
    }
}
