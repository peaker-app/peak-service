using Common.Application.Abstractions;
using Common.Domain.Results;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PeakService.Application.Abstractions;
using PeakService.Domain.PeakIngestionRuns;
using PeakService.Domain.Peaks;
using PeakService.Ingestion.RunPeakIngestion;
using PeakService.Ingestion.UnitTests.TestData;
using Xunit;

namespace PeakService.Ingestion.UnitTests;

public sealed class RunPeakIngestionCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);

    private readonly IPeakSourceClient _sourceClient = Substitute.For<IPeakSourceClient>();
    private readonly IPeakDeduplicationStrategy _deduplication = Substitute.For<IPeakDeduplicationStrategy>();
    private readonly IPeakRepository _peakRepository = Substitute.For<IPeakRepository>();
    private readonly IPeakIngestionRunRepository _runRepository = Substitute.For<IPeakIngestionRunRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly RunPeakIngestionCommandHandler _handler;

    public RunPeakIngestionCommandHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(Now);
        _handler = new RunPeakIngestionCommandHandler(
            _sourceClient,
            _deduplication,
            _peakRepository,
            _runRepository,
            _unitOfWork,
            _dateTimeProvider,
            Substitute.For<ILogger<RunPeakIngestionCommandHandler>>());
    }

    [Fact]
    public async Task Handle_WithUnknownPeak_CreatesItAndCountsItAsCreated()
    {
        GivenSource(PeakSourceRecords.Valid());
        GivenNoDuplicate();

        Result<PeakIngestionRunResponse> result = await RunAsync();

        result.Value.PeaksCreated.Should().Be(1);
        _peakRepository.Received(1).Add(Arg.Any<Peak>());
    }

    [Fact]
    public async Task Handle_WithKnownPeakAndNewData_UpdatesItInsteadOfCreatingAnother()
    {
        GivenSource(PeakSourceRecords.Valid(altitudeMeters: 3410));
        GivenDuplicate(ExistingPeak());

        Result<PeakIngestionRunResponse> result = await RunAsync();

        result.Value.PeaksUpdated.Should().Be(1);
        result.Value.PeaksCreated.Should().Be(0);
        _peakRepository.DidNotReceive().Add(Arg.Any<Peak>());
    }

    [Fact]
    public async Task Handle_WithKnownPeakAndIdenticalData_CountsNeitherCreatedNorUpdated()
    {
        GivenSource(PeakSourceRecords.Valid());
        GivenDuplicate(ExistingPeak());

        Result<PeakIngestionRunResponse> result = await RunAsync();

        (result.Value.PeaksCreated, result.Value.PeaksUpdated, result.Value.PeaksFailed)
            .Should().Be((0, 0, 0));
    }

    [Fact]
    public async Task Handle_WithNearDuplicateOfAnotherSource_DoesNotCatalogueItTwice()
    {
        Peak existing = ExistingPeak();
        GivenSource(PeakSourceRecords.Valid(wikidataId: "Q999", name: "Pico de Aneto"));
        GivenNearDuplicate(existing);

        Result<PeakIngestionRunResponse> result = await RunAsync();

        (result.Value.PeaksCreated, result.Value.PeaksUpdated, result.Value.PeaksFailed)
            .Should().Be((0, 0, 0));
        _peakRepository.DidNotReceive().Add(Arg.Any<Peak>());
    }

    [Fact]
    public async Task Handle_WithNearDuplicateOfAnotherSource_LeavesTheCataloguedPeakUntouched()
    {
        Peak existing = ExistingPeak();
        GivenSource(PeakSourceRecords.Valid(wikidataId: "Q999", name: "Pico de Aneto"));
        GivenNearDuplicate(existing);

        await RunAsync();

        existing.WikidataId.Should().Be("Q192580");
        existing.Name.Should().Be("Aneto");
    }

    [Fact]
    public async Task Handle_WithOutOfRangeCoordinates_CountsTheRecordAsFailed()
    {
        GivenSource(PeakSourceRecords.Valid(latitude: 120));
        GivenNoDuplicate();

        Result<PeakIngestionRunResponse> result = await RunAsync();

        result.Value.PeaksFailed.Should().Be(1);
        _peakRepository.DidNotReceive().Add(Arg.Any<Peak>());
    }

    [Fact]
    public async Task Handle_WithInvalidRecord_KeepsIngestingTheRemainingOnes()
    {
        GivenSource(
            PeakSourceRecords.Valid(wikidataId: "Q1", name: string.Empty),
            PeakSourceRecords.Valid(wikidataId: "Q2"),
            PeakSourceRecords.Valid(wikidataId: "Q3", latitude: 999));
        GivenNoDuplicate();

        Result<PeakIngestionRunResponse> result = await RunAsync();

        (result.Value.PeaksCreated, result.Value.PeaksFailed).Should().Be((1, 2));
        result.Value.Status.Should().Be(nameof(IngestionStatus.Completed));
    }

    [Fact]
    public async Task Handle_WithoutFailures_CompletesTheRun()
    {
        GivenSource(PeakSourceRecords.Valid());
        GivenNoDuplicate();

        Result<PeakIngestionRunResponse> result = await RunAsync();

        result.Value.Status.Should().Be(nameof(IngestionStatus.Completed));
        result.Value.FinishedAtUtc.Should().Be(Now);
    }

    [Fact]
    public async Task Handle_WhenTheSourceFails_MarksTheRunAsFailedAndKeepsTheReason()
    {
        _sourceClient.StreamAsync(Arg.Any<PeakSourceCursor>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("SPARQL endpoint unavailable"));

        Result<PeakIngestionRunResponse> result = await RunAsync();

        result.Value.Status.Should().Be(nameof(IngestionStatus.Failed));
        result.Value.Error.Should().Be("SPARQL endpoint unavailable");
    }

    [Fact]
    public async Task Handle_WhenAPartitionFails_MarksTheRunAsPartialAndKeepsTheReason()
    {
        GivenPartitions(
            PeakSourcePartition.Loaded([PeakSourceRecords.Valid()]),
            PeakSourcePartition.Failed("Band [3000, 3250) could not be loaded. HTTP 504"));
        GivenNoDuplicate();

        Result<PeakIngestionRunResponse> result = await RunAsync();

        result.Value.Status.Should().Be(nameof(IngestionStatus.Partial));
        result.Value.Error.Should().Contain("[3000, 3250)");
    }

    [Fact]
    public async Task Handle_WhenAPartitionFails_StillPersistsThePeaksOfTheHealthyOnes()
    {
        GivenPartitions(
            PeakSourcePartition.Failed("Band [0, 250) could not be loaded. HTTP 504"),
            PeakSourcePartition.Loaded([PeakSourceRecords.Valid()]));
        GivenNoDuplicate();

        Result<PeakIngestionRunResponse> result = await RunAsync();

        result.Value.PeaksCreated.Should().Be(1);
        _peakRepository.Received(1).Add(Arg.Any<Peak>());
    }

    [Fact]
    public async Task Handle_WhenEveryPartitionSucceeds_MarksTheRunAsCompleted()
    {
        GivenSource(PeakSourceRecords.Valid());
        GivenNoDuplicate();

        Result<PeakIngestionRunResponse> result = await RunAsync();

        result.Value.Status.Should().Be(nameof(IngestionStatus.Completed));
    }

    [Fact]
    public async Task Handle_Always_RegistersTheRunBeforeIngesting()
    {
        GivenSource();

        await RunAsync();

        _runRepository.Received(1).Add(Arg.Any<PeakIngestionRun>());
    }

    [Fact]
    public async Task Handle_WithoutPreviousRun_AsksTheSourceForEverything()
    {
        GivenSource();
        _runRepository.GetLastCompletedAsync(Arg.Any<CancellationToken>()).Returns((PeakIngestionRun?)null);

        await RunAsync();

        ReceivedCursor(cursor => !cursor.IsIncremental);
    }

    [Fact]
    public async Task Handle_WithPreviousCompletedRun_AsksTheSourceOnlyForChangesSinceThen()
    {
        DateTime previousStart = Now.AddDays(-1);
        GivenSource();
        _runRepository.GetLastCompletedAsync(Arg.Any<CancellationToken>())
            .Returns(PeakIngestionRun.Start(previousStart));

        await RunAsync();

        ReceivedCursor(cursor => cursor.ModifiedSinceUtc == previousStart);
    }

    [Fact]
    public async Task Handle_InFullMode_IgnoresThePreviousRunAndReloadsEverything()
    {
        GivenSource();
        _runRepository.GetLastCompletedAsync(Arg.Any<CancellationToken>())
            .Returns(PeakIngestionRun.Start(Now.AddDays(-1)));

        await RunAsync(IngestionMode.Full);

        ReceivedCursor(cursor => !cursor.IsIncremental);
        await _runRepository.DidNotReceive().GetLastCompletedAsync(Arg.Any<CancellationToken>());
    }

    private Task<Result<PeakIngestionRunResponse>> RunAsync(IngestionMode mode = IngestionMode.Automatic) =>
        _handler.Handle(new RunPeakIngestionCommand(mode), CancellationToken.None);

    private void ReceivedCursor(Func<PeakSourceCursor, bool> predicate) =>
        _sourceClient.Received(1)
            .StreamAsync(
                Arg.Is<PeakSourceCursor>(cursor => cursor != null && predicate(cursor)),
                Arg.Any<CancellationToken>());

    private void GivenSource(params PeakSourceRecord[] records) =>
        GivenPartitions(PeakSourcePartition.Loaded(records));

    private void GivenPartitions(params PeakSourcePartition[] partitions) =>
        _sourceClient.StreamAsync(Arg.Any<PeakSourceCursor>(), Arg.Any<CancellationToken>())
            .Returns(AsyncSequence.Of(partitions));

    private void GivenNoDuplicate() =>
        _deduplication.FindMatchAsync(Arg.Any<PeakSourceData>(), Arg.Any<CancellationToken>())
            .Returns((PeakMatch?)null);

    private void GivenDuplicate(Peak peak) =>
        _deduplication.FindMatchAsync(Arg.Any<PeakSourceData>(), Arg.Any<CancellationToken>())
            .Returns(PeakMatch.SameSource(peak));

    private void GivenNearDuplicate(Peak peak) =>
        _deduplication.FindMatchAsync(Arg.Any<PeakSourceData>(), Arg.Any<CancellationToken>())
            .Returns(PeakMatch.NearDuplicate(peak));

    private static Peak ExistingPeak() =>
        Peak.Create(new PeakDraft(
            new PeakSourceData(
                "Q192580",
                "Aneto",
                3404,
                2812,
                Coordinates.Create(42.6316, 0.6577).Value,
                "ES",
                "Huesca",
                SourceRevision: null),
            RangeId: null,
            AlternativeNames: [])).Value;
}
