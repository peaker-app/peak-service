using Common.Application.Abstractions;
using Common.Application.Messaging;
using Common.Domain.Results;
using PeakService.Application.Abstractions;
using PeakService.Domain.PeakIngestionRuns;
using PeakService.Domain.Peaks;

namespace PeakService.Ingestion.RunPeakIngestion;

internal sealed class RunPeakIngestionCommandHandler(
    IPeakSourceClient sourceClient,
    IPeakDeduplicationStrategy deduplicationStrategy,
    IPeakRepository peakRepository,
    IPeakIngestionRunRepository runRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    ILogger<RunPeakIngestionCommandHandler> logger)
    : ICommandHandler<RunPeakIngestionCommand, PeakIngestionRunResponse>
{
    public async Task<Result<PeakIngestionRunResponse>> Handle(
        RunPeakIngestionCommand command,
        CancellationToken cancellationToken)
    {
        PeakIngestionRun? previous = await runRepository.GetLastCompletedAsync(cancellationToken);

        PeakIngestionRun run = PeakIngestionRun.Start(dateTimeProvider.UtcNow);
        runRepository.Add(run);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        PeakSourceCursor cursor = ResolveCursor(command.Mode, previous);

        logger.LogInformation(
            "Peak ingestion run {RunId} started with cursor {ModifiedSinceUtc}", run.Id, cursor.ModifiedSinceUtc);

        await IngestAsync(run, cursor, cancellationToken);

        logger.LogInformation(
            "Peak ingestion run {RunId} finished as {Status}: {Created} created, {Updated} updated, "
            + "{Unchanged} unchanged, {Failed} failed",
            run.Id, run.Status, run.PeaksCreated, run.PeaksUpdated, run.PeaksUnchanged, run.PeaksFailed);

        WarnOnMassChange(run, previous);

        return run.ToResponse();
    }

    private void WarnOnMassChange(PeakIngestionRun run, PeakIngestionRun? previous)
    {
        if (!run.IsMassChangeComparedTo(previous))
        {
            return;
        }

        logger.LogWarning(
            "Peak ingestion run {RunId} changed {Changed} peaks, well above the {PreviousChanged} of run "
            + "{PreviousRunId}: review the source before trusting the catalogue",
            run.Id, run.PeaksChanged, previous!.PeaksChanged, previous.Id);
    }

    private static PeakSourceCursor ResolveCursor(IngestionMode mode, PeakIngestionRun? previous) =>
        mode is IngestionMode.Full || previous is null
            ? PeakSourceCursor.Full
            : PeakSourceCursor.Since(previous.StartedAtUtc);

    private async Task IngestAsync(PeakIngestionRun run, PeakSourceCursor cursor, CancellationToken cancellationToken)
    {
#pragma warning disable CA1031 // Motivo: un fallo de la fuente externa queda auditado en la ejecución en lugar de propagarse.
        try
        {
            IReadOnlyList<string> failures = await ConsumeAsync(run, cursor, cancellationToken);

            _ = failures.Count == 0
                ? run.Complete(dateTimeProvider.UtcNow)
                : run.CompletePartially(string.Join(" | ", failures), dateTimeProvider.UtcNow);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Peak ingestion run {RunId} could not be completed", run.Id);
            _ = run.Fail(exception.Message, dateTimeProvider.UtcNow);
        }
#pragma warning restore CA1031

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<string>> ConsumeAsync(
        PeakIngestionRun run,
        PeakSourceCursor cursor,
        CancellationToken cancellationToken)
    {
        List<string> failures = [];

        await foreach (PeakSourcePartition partition in sourceClient.StreamAsync(cursor, cancellationToken))
        {
            if (partition.FailureReason is { } reason)
            {
                logger.LogWarning("Peak ingestion run {RunId} skipped a partition: {Reason}", run.Id, reason);
                failures.Add(reason);
                continue;
            }

            foreach (PeakSourceRecord record in partition.Records)
            {
                await ProcessAsync(record, run, cancellationToken);
            }
        }

        return failures;
    }

    private async Task ProcessAsync(
        PeakSourceRecord record,
        PeakIngestionRun run,
        CancellationToken cancellationToken)
    {
        Result<IngestedPeak> ingested = record.ToIngestedPeak();

        Result outcome = ingested.IsSuccess
            ? await UpsertAsync(ingested.Value, run, cancellationToken)
            : Result.Failure(ingested.Error);

        if (outcome.IsFailure)
        {
            Discard(record, outcome.Error, run);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Result> UpsertAsync(
        IngestedPeak ingested,
        PeakIngestionRun run,
        CancellationToken cancellationToken)
    {
        PeakMatch? match = await deduplicationStrategy.FindMatchAsync(ingested.Source, cancellationToken);

        if (match is null)
        {
            return Create(ingested, run);
        }

        return match.Kind is PeakMatchKind.SameSource
            ? Update(match.Peak, ingested, run)
            : SkipDuplicate(match.Peak, ingested.Source);
    }

    private Result Create(IngestedPeak ingested, PeakIngestionRun run)
    {
        Result<Peak> created = Peak.Create(
            new PeakDraft(ingested.Source, RangeId: null, ingested.AlternativeNames));

        if (created.IsFailure)
        {
            return Result.Failure(created.Error);
        }

        peakRepository.Add(created.Value);

        return run.RecordCreated();
    }

    private static Result Update(Peak peak, IngestedPeak ingested, PeakIngestionRun run)
    {
        Result<PeakUpdateOutcome> updated = peak.UpdateFromSource(ingested.Source, ingested.AlternativeNames);

        if (updated.IsFailure)
        {
            return Result.Failure(updated.Error);
        }

        return updated.Value is PeakUpdateOutcome.Updated ? run.RecordUpdated() : run.RecordUnchanged();
    }

    private Result SkipDuplicate(Peak existing, PeakSourceData source)
    {
        logger.LogInformation(
            "Peak {WikidataId} skipped: already catalogued as {PeakId} ({ExistingWikidataId})",
            source.WikidataId, existing.Id, existing.WikidataId);

        return Result.Success();
    }

    private void Discard(PeakSourceRecord record, Error error, PeakIngestionRun run)
    {
        logger.LogWarning(
            "Peak {WikidataId} discarded during ingestion: {ErrorCode} {ErrorDescription}",
            record.WikidataId, error.Code, error.Description);

        _ = run.RecordFailed();
    }
}
