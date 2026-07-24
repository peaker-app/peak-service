using Common.Domain.Results;
using FluentAssertions;
using PeakService.Domain.PeakIngestionRuns;
using Xunit;

namespace PeakService.Domain.UnitTests.PeakIngestionRuns;

public sealed class PeakIngestionRunTests
{
    private static readonly DateTime StartedAt = new(2026, 7, 24, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime FinishedAt = new(2026, 7, 24, 10, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Start_SetsRunningStatusAndStartTime()
    {
        PeakIngestionRun run = PeakIngestionRun.Start(StartedAt);

        run.Status.Should().Be(IngestionStatus.Running);
        run.StartedAtUtc.Should().Be(StartedAt);
        run.FinishedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Start_LeavesAllCountersAtZero()
    {
        PeakIngestionRun run = PeakIngestionRun.Start(StartedAt);

        (run.PeaksCreated, run.PeaksUpdated, run.PeaksFailed).Should().Be((0, 0, 0));
    }

    [Fact]
    public void RecordCreated_IncrementsTheCreatedCounter()
    {
        PeakIngestionRun run = PeakIngestionRun.Start(StartedAt);

        run.RecordCreated();
        run.RecordCreated();

        run.PeaksCreated.Should().Be(2);
    }

    [Fact]
    public void RecordUpdated_IncrementsTheUpdatedCounter()
    {
        PeakIngestionRun run = PeakIngestionRun.Start(StartedAt);

        run.RecordUpdated();

        run.PeaksUpdated.Should().Be(1);
    }

    [Fact]
    public void RecordFailed_IncrementsTheFailedCounter()
    {
        PeakIngestionRun run = PeakIngestionRun.Start(StartedAt);

        run.RecordFailed();

        run.PeaksFailed.Should().Be(1);
    }

    [Fact]
    public void Complete_SetsCompletedStatusAndFinishTime()
    {
        PeakIngestionRun run = PeakIngestionRun.Start(StartedAt);

        Result result = run.Complete(FinishedAt);

        result.IsSuccess.Should().BeTrue();
        run.Status.Should().Be(IngestionStatus.Completed);
        run.FinishedAtUtc.Should().Be(FinishedAt);
    }

    [Fact]
    public void Fail_SetsFailedStatusAndKeepsTheReason()
    {
        PeakIngestionRun run = PeakIngestionRun.Start(StartedAt);

        Result result = run.Fail("El endpoint SPARQL no responde.", FinishedAt);

        result.IsSuccess.Should().BeTrue();
        run.Status.Should().Be(IngestionStatus.Failed);
        run.Error.Should().Be("El endpoint SPARQL no responde.");
    }

    [Fact]
    public void Complete_OnAlreadyFinishedRun_ReturnsRunAlreadyFinished()
    {
        PeakIngestionRun run = PeakIngestionRun.Start(StartedAt);
        run.Complete(FinishedAt);

        Result result = run.Complete(FinishedAt);

        result.Error.Should().Be(PeakIngestionErrors.RunAlreadyFinished);
    }

    [Fact]
    public void Fail_OnAlreadyFinishedRun_ReturnsRunAlreadyFinished()
    {
        PeakIngestionRun run = PeakIngestionRun.Start(StartedAt);
        run.Complete(FinishedAt);

        Result result = run.Fail("tarde", FinishedAt);

        result.Error.Should().Be(PeakIngestionErrors.RunAlreadyFinished);
    }

    [Fact]
    public void RecordCreated_OnAlreadyFinishedRun_ReturnsRunAlreadyFinished()
    {
        PeakIngestionRun run = PeakIngestionRun.Start(StartedAt);
        run.Complete(FinishedAt);

        Result result = run.RecordCreated();

        result.Error.Should().Be(PeakIngestionErrors.RunAlreadyFinished);
    }

    [Fact]
    public void RecordCreated_OnAlreadyFinishedRun_DoesNotIncrementTheCounter()
    {
        PeakIngestionRun run = PeakIngestionRun.Start(StartedAt);
        run.Complete(FinishedAt);

        run.RecordCreated();

        run.PeaksCreated.Should().Be(0);
    }
}
