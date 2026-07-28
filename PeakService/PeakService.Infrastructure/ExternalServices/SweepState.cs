using PeakService.Application.Abstractions;

namespace PeakService.Infrastructure.ExternalServices;

internal sealed class SweepState(IngestionOptions settings, PeakSourceCursor cursor)
{
    private readonly HashSet<string> _seen = [];
    private readonly List<ElevationBand> _deferred = [];

    public IngestionOptions Settings => settings;

    public PeakSourceCursor Cursor => cursor;

    public IReadOnlyList<ElevationBand> Deferred => _deferred;

    public bool IsFinalPass { get; private set; }

    public bool ReachedLimit => _seen.Count >= settings.MaxRecords;

    public bool AlreadySeen(string wikidataId) => _seen.Contains(wikidataId);

    public bool WouldReachLimit(int pendingCount) => _seen.Count + pendingCount >= settings.MaxRecords;

    public void Commit(IEnumerable<PeakSourceRecord> records) =>
        _seen.UnionWith(records.Select(record => record.WikidataId));

    public bool TryDefer(ElevationBand band)
    {
        if (IsFinalPass)
        {
            return false;
        }

        _deferred.Add(band);

        return true;
    }

    public List<ElevationBand> StartFinalPass()
    {
        List<ElevationBand> bands = [.. _deferred];
        _deferred.Clear();
        IsFinalPass = true;

        return bands;
    }
}
