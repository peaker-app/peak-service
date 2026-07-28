using System.Runtime.CompilerServices;
using PeakService.Application.Abstractions;

namespace PeakService.IntegrationTests.Fakes;

public sealed class FakePeakSourceClient : IPeakSourceClient
{
    private IReadOnlyList<PeakSourcePartition> _partitions = [];
    private Exception? _failure;

    public PeakSourceCursor? LastCursor { get; private set; }

    public void Returns(params PeakSourceRecord[] records)
    {
        _partitions = [PeakSourcePartition.Loaded(records)];
        _failure = null;
    }

    public void ReturnsPartitions(params PeakSourcePartition[] partitions)
    {
        _partitions = partitions;
        _failure = null;
    }

    public void Fails(Exception failure)
    {
        _failure = failure;
        _partitions = [];
    }

    public async IAsyncEnumerable<PeakSourcePartition> StreamAsync(
        PeakSourceCursor cursor,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        LastCursor = cursor;

        if (_failure is not null)
        {
            throw _failure;
        }

        await Task.CompletedTask;

        foreach (PeakSourcePartition partition in _partitions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return partition;
        }
    }
}
