using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using PeakService.Application.Abstractions;
using PeakService.Domain.Peaks;

namespace PeakService.Infrastructure.ExternalServices;

internal sealed class WikidataPeakSourceClient(HttpClient httpClient, IOptions<IngestionOptions> options)
    : IPeakSourceClient
{
    private readonly SparqlEndpoint _endpoint = new(httpClient);

    public async IAsyncEnumerable<PeakSourcePartition> StreamAsync(
        PeakSourceCursor cursor,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        SweepState state = new(options.Value, cursor);

        await foreach (PeakSourcePartition partition in
            SweepAsync(ElevationBandPlan.Build(state.Settings, cursor), state, cancellationToken))
        {
            yield return partition;
        }

        if (state.Deferred.Count == 0 || state.ReachedLimit)
        {
            yield break;
        }

        List<ElevationBand> retried = state.StartFinalPass();

        await Task.Delay(state.Settings.DeferredRetryDelay, cancellationToken);

        await foreach (PeakSourcePartition partition in SweepAsync(retried, state, cancellationToken))
        {
            yield return partition;
        }
    }

    private async IAsyncEnumerable<PeakSourcePartition> SweepAsync(
        IReadOnlyList<ElevationBand> bands,
        SweepState state,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Stack<ElevationBand> pending = new(bands.Reverse());

        while (pending.Count > 0)
        {
            ElevationBand band = pending.Pop();
            BandResult result = await LoadAsync(band, state, cancellationToken);

            if (result.IsFailed && Reschedule(band, result, state, pending))
            {
                continue;
            }

            yield return result.Partition;

            if (state.ReachedLimit)
            {
                yield break;
            }

            await Task.Delay(state.Settings.DelayBetweenRequests, cancellationToken);
        }
    }

    private async Task<BandResult> LoadAsync(
        ElevationBand band,
        SweepState state,
        CancellationToken cancellationToken)
    {
        SparqlOutcome outcome = await _endpoint.ExecuteAsync(
            WikidataSparqlQuery.BuildCatalog(state.Settings, state.Cursor, band), cancellationToken);

        return outcome.IsFailed
            ? new BandResult(
                PeakSourcePartition.Failed($"Band {band} could not be loaded. {outcome.FailureReason}"),
                outcome.IsRetriable)
            : await AcceptAsync(outcome, state, cancellationToken);
    }

    private static bool Reschedule(
        ElevationBand band,
        BandResult result,
        SweepState state,
        Stack<ElevationBand> pending) =>
        result.IsRetriable && (TrySplit(band, state.Settings, pending) || state.TryDefer(band));

    private static bool TrySplit(ElevationBand band, IngestionOptions settings, Stack<ElevationBand> pending)
    {
        if (band.IsUnbounded)
        {
            return PushAll(ElevationBandPlan.FullSweep(settings), pending);
        }

        if (!band.CanSplit(settings.MinElevationBandMeters))
        {
            return false;
        }

        (ElevationBand lower, ElevationBand upper) = band.Split();

        return PushAll([lower, upper], pending);
    }

    private static bool PushAll(List<ElevationBand> bands, Stack<ElevationBand> pending)
    {
        for (int index = bands.Count - 1; index >= 0; index--)
        {
            pending.Push(bands[index]);
        }

        return true;
    }

    private async Task<BandResult> AcceptAsync(
        SparqlOutcome outcome,
        SweepState state,
        CancellationToken cancellationToken)
    {
        List<PeakSourceRecord> records = Deduplicate(outcome.Bindings, state);
        BandResult result = await EnrichAsync(records, state.Settings, cancellationToken);

        if (!result.IsFailed)
        {
            state.Commit(records);
        }

        return result;
    }

    private static List<PeakSourceRecord> Deduplicate(
        IReadOnlyList<Dictionary<string, SparqlBinding>> bindings,
        SweepState state)
    {
        List<PeakSourceRecord> records = [];
        HashSet<string> batch = [];

        foreach (Dictionary<string, SparqlBinding> binding in bindings)
        {
            if (state.WouldReachLimit(batch.Count))
            {
                break;
            }

            if (SparqlBindingReader.ToRecord(binding) is { } record
                && !state.AlreadySeen(record.WikidataId)
                && batch.Add(record.WikidataId))
            {
                records.Add(record);
            }
        }

        return records;
    }

    private async Task<BandResult> EnrichAsync(
        IReadOnlyList<PeakSourceRecord> records,
        IngestionOptions settings,
        CancellationToken cancellationToken)
    {
        Dictionary<string, List<PeakNameDraft>> names = [];

        foreach (string[] batch in records.Select(record => record.WikidataId).Chunk(settings.NameBatchSize))
        {
            SparqlOutcome outcome = await _endpoint.ExecuteAsync(
                WikidataSparqlQuery.BuildNames(settings, batch), cancellationToken);

            if (outcome.IsFailed)
            {
                return new BandResult(
                    PeakSourcePartition.Failed(
                        $"Names for {batch.Length} peaks could not be loaded. {outcome.FailureReason}"),
                    outcome.IsRetriable);
            }

            Collect(outcome.Bindings, names);
        }

        return new BandResult(
            PeakSourcePartition.Loaded([.. records.Select(record => Attach(record, names, settings.Language))]),
            IsRetriable: false);
    }

    private static void Collect(
        IReadOnlyList<Dictionary<string, SparqlBinding>> bindings,
        Dictionary<string, List<PeakNameDraft>> names)
    {
        foreach (Dictionary<string, SparqlBinding> binding in bindings)
        {
            if (SparqlBindingReader.ToNameDraft(binding) is { } entry)
            {
                names.TryAdd(entry.WikidataId, []);
                names[entry.WikidataId].Add(entry.Name);
            }
        }
    }

    private static PeakSourceRecord Attach(
        PeakSourceRecord record,
        Dictionary<string, List<PeakNameDraft>> names,
        string language)
    {
        if (!names.TryGetValue(record.WikidataId, out List<PeakNameDraft>? drafts))
        {
            return record;
        }

        string canonicalName = ResolveCanonicalName(record.Name, drafts, language);

        return record with
        {
            Name = canonicalName,
            AlternativeNames = Alternatives(drafts, canonicalName)
        };
    }

    private static string ResolveCanonicalName(string sourceName, List<PeakNameDraft> drafts, string language) =>
        WikidataIdentifier.IsIdentifier(sourceName)
            ? BestName(drafts, language) ?? sourceName
            : sourceName;

    private static string? BestName(List<PeakNameDraft> drafts, string language)
    {
        List<PeakNameDraft> candidates =
            [.. drafts.Where(draft => !WikidataIdentifier.IsIdentifier(draft.Name))];

        PeakNameDraft? best =
            candidates.Find(draft => draft.IsOfficial && SpeaksLanguage(draft, language))
            ?? candidates.Find(draft => SpeaksLanguage(draft, language))
            ?? candidates.Find(draft => draft.IsOfficial)
            ?? candidates.FirstOrDefault();

        return best?.Name;
    }

    private static bool SpeaksLanguage(PeakNameDraft draft, string language) =>
        string.Equals(draft.LanguageCode, language, StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<PeakNameDraft> Alternatives(List<PeakNameDraft> drafts, string canonicalName) =>
    [
        .. drafts
            .Where(draft => !string.Equals(draft.Name, canonicalName, StringComparison.OrdinalIgnoreCase))
            .DistinctBy(draft => (draft.LanguageCode, draft.Name))
    ];

    private sealed record BandResult(PeakSourcePartition Partition, bool IsRetriable)
    {
        public bool IsFailed => Partition.IsFailed;
    }
}
