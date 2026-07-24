using Common.Domain.Abstractions;
using Common.Domain.Results;

namespace PeakService.Domain.PeakIngestionRuns;

public sealed class PeakIngestionRun : AggregateRoot
{
    private PeakIngestionRun()
    {
    }

    private PeakIngestionRun(Guid id, DateTime startedAtUtc) : base(id)
    {
        StartedAtUtc = startedAtUtc;
        Status = IngestionStatus.Running;
    }

    public DateTime StartedAtUtc { get; private set; }

    public DateTime? FinishedAtUtc { get; private set; }

    public int PeaksCreated { get; private set; }

    public int PeaksUpdated { get; private set; }

    public int PeaksFailed { get; private set; }

    public IngestionStatus Status { get; private set; }

    public string? Error { get; private set; }

    public bool IsRunning => Status is IngestionStatus.Running;

    public static PeakIngestionRun Start(DateTime startedAtUtc) =>
        new(Guid.CreateVersion7(), startedAtUtc);

    public Result RecordCreated() => Increment(() => PeaksCreated++);

    public Result RecordUpdated() => Increment(() => PeaksUpdated++);

    public Result RecordFailed() => Increment(() => PeaksFailed++);

    public Result Complete(DateTime finishedAtUtc) => Finish(IngestionStatus.Completed, finishedAtUtc, error: null);

    public Result Fail(string error, DateTime finishedAtUtc) =>
        Finish(IngestionStatus.Failed, finishedAtUtc, error);

    private Result Increment(Action increment)
    {
        if (!IsRunning)
        {
            return Result.Failure(PeakIngestionErrors.RunAlreadyFinished);
        }

        increment();

        return Result.Success();
    }

    private Result Finish(IngestionStatus status, DateTime finishedAtUtc, string? error)
    {
        if (!IsRunning)
        {
            return Result.Failure(PeakIngestionErrors.RunAlreadyFinished);
        }

        Status = status;
        FinishedAtUtc = finishedAtUtc;
        Error = error;

        return Result.Success();
    }
}
