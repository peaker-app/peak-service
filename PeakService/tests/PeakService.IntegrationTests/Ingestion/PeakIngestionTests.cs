using Common.API.Responses;
using Common.Domain.Results;
using Common.Infrastructure.Persistence.Outbox;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PeakService.Application.Abstractions;
using PeakService.Application.Ingestion.RunPeakIngestion;
using PeakService.Application.Peaks.ListPeaks;
using PeakService.Domain.PeakIngestionRuns;
using PeakService.Domain.Peaks;
using PeakService.IntegrationTests.TestData;
using Xunit;

namespace PeakService.IntegrationTests.Ingestion;

[Collection(PeakServiceCollection.Name)]
public sealed class PeakIngestionTests(PeakServiceApiFactory factory)
{
    [Fact]
    public async Task Ingestion_WithNewPeaks_StoresThemInTheCatalogue()
    {
        await factory.ResetAsync();
        factory.PeakSource.Returns(
            SourceRecords.Valid("Q192580", "Aneto", 3404, 42.6316, 0.6577),
            SourceRecords.Valid("Q513", "Everest", 8849, 27.9881, 86.9250));

        PeakIngestionRunResponse run = await RunAsync();

        run.PeaksCreated.Should().Be(2);
        List<string> names = await factory.QueryAsync(context =>
            context.Peaks.OrderBy(peak => peak.Name).Select(peak => peak.Name).ToListAsync());
        names.Should().BeEquivalentTo(["Aneto", "Everest"]);
    }

    [Fact]
    public async Task Ingestion_RunTwiceWithTheSameData_DoesNotDuplicateAnyPeak()
    {
        await factory.ResetAsync();
        factory.PeakSource.Returns(SourceRecords.Valid("Q192580", "Aneto", 3404, 42.6316, 0.6577));

        await RunAsync();
        PeakIngestionRunResponse second = await RunAsync(IngestionMode.Full);

        (second.PeaksCreated, second.PeaksUpdated).Should().Be((0, 0));
        int total = await factory.QueryAsync(context => context.Peaks.CountAsync());
        total.Should().Be(1);
    }

    [Fact]
    public async Task Ingestion_WithChangedAltitude_UpdatesTheExistingPeak()
    {
        await factory.ResetAsync();
        factory.PeakSource.Returns(SourceRecords.Valid("Q192580", "Aneto", 3404, 42.6316, 0.6577));
        await RunAsync();

        factory.PeakSource.Returns(SourceRecords.Valid("Q192580", "Aneto", 3410, 42.6316, 0.6577));
        PeakIngestionRunResponse second = await RunAsync(IngestionMode.Full);

        second.PeaksUpdated.Should().Be(1);
        Peak peak = await factory.QueryAsync(context => context.Peaks.SingleAsync());
        peak.AltitudeMeters.Should().Be(3410);
    }

    [Fact]
    public async Task Ingestion_WithNearbyPeakOfSimilarName_SkipsItAsDuplicate()
    {
        await factory.ResetAsync();
        factory.PeakSource.Returns(SourceRecords.Valid("Q192580", "Aneto", 3404, 42.6316, 0.6577));
        await RunAsync();

        factory.PeakSource.Returns(SourceRecords.Valid("Q999999", "Anetó", 3404, 42.63161, 0.65771));
        PeakIngestionRunResponse second = await RunAsync(IngestionMode.Full);

        second.PeaksCreated.Should().Be(0);
        int total = await factory.QueryAsync(context => context.Peaks.CountAsync());
        total.Should().Be(1);
    }

    [Fact]
    public async Task Ingestion_WithDistantPeakOfSimilarName_TreatsItAsANewPeak()
    {
        await factory.ResetAsync();
        factory.PeakSource.Returns(SourceRecords.Valid("Q192580", "Aneto", 3404, 42.6316, 0.6577));
        await RunAsync();

        factory.PeakSource.Returns(SourceRecords.Valid("Q999999", "Aneto", 3404, 45.8326, 6.8652));
        PeakIngestionRunResponse second = await RunAsync(IngestionMode.Full);

        second.PeaksCreated.Should().Be(1);
        int total = await factory.QueryAsync(context => context.Peaks.CountAsync());
        total.Should().Be(2);
    }

    [Fact]
    public async Task Ingestion_WithInvalidRecord_RecordsItAsFailedAndKeepsGoing()
    {
        await factory.ResetAsync();
        factory.PeakSource.Returns(
            SourceRecords.Valid("Q1", "Sin coordenada", 3000, 999, 0.65),
            SourceRecords.Valid("Q2", "Aneto", 3404, 42.6316, 0.6577));

        PeakIngestionRunResponse run = await RunAsync();

        (run.PeaksCreated, run.PeaksFailed).Should().Be((1, 1));
        run.Status.Should().Be(nameof(IngestionStatus.Completed));
    }

    [Fact]
    public async Task Ingestion_Always_AuditsTheRunInTheDatabase()
    {
        await factory.ResetAsync();
        factory.PeakSource.Returns(SourceRecords.Valid("Q192580", "Aneto", 3404, 42.6316, 0.6577));

        PeakIngestionRunResponse response = await RunAsync();

        PeakIngestionRun stored = await factory.QueryAsync(context =>
            context.PeakIngestionRuns.SingleAsync(run => run.Id == response.RunId));
        stored.Status.Should().Be(IngestionStatus.Completed);
        stored.PeaksCreated.Should().Be(1);
        stored.FinishedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task Ingestion_WhenTheSourceFails_AuditsTheRunAsFailed()
    {
        await factory.ResetAsync();
        factory.PeakSource.Fails(new InvalidOperationException("SPARQL endpoint unavailable"));

        PeakIngestionRunResponse response = await RunAsync();

        PeakIngestionRun stored = await factory.QueryAsync(context =>
            context.PeakIngestionRuns.SingleAsync(run => run.Id == response.RunId));
        stored.Status.Should().Be(IngestionStatus.Failed);
        stored.Error.Should().Be("SPARQL endpoint unavailable");
    }

    [Fact]
    public async Task Ingestion_WithoutPreviousRun_RequestsTheWholeCatalogue()
    {
        await factory.ResetAsync();
        factory.PeakSource.Returns();

        await RunAsync();

        factory.PeakSource.LastCursor!.IsIncremental.Should().BeFalse();
    }

    [Fact]
    public async Task Ingestion_AfterACompletedRun_RequestsOnlyTheChangesSinceThatRun()
    {
        await factory.ResetAsync();
        factory.PeakSource.Returns();
        await RunAsync();
        DateTime firstRunStart = await factory.QueryAsync(context =>
            context.PeakIngestionRuns.OrderBy(run => run.StartedAtUtc).Select(run => run.StartedAtUtc).FirstAsync());

        await RunAsync();

        factory.PeakSource.LastCursor!.ModifiedSinceUtc.Should().BeCloseTo(firstRunStart, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Ingestion_WithNewPeak_WritesTheIntegrationEventToTheOutbox()
    {
        await factory.ResetAsync();
        factory.PeakSource.Returns(SourceRecords.Valid("Q192580", "Aneto", 3404, 42.6316, 0.6577));

        await RunAsync();

        List<string> types = await factory.QueryAsync(context =>
            context.Set<OutboxMessage>().Select(message => message.Type).ToListAsync());
        types.Should().ContainSingle().Which.Should().Contain("PeakCreatedDomainEvent");
    }

    [Fact]
    public async Task Ingestion_WithRenamedPeak_WritesUpdatedAndRenamedEventsToTheOutbox()
    {
        await factory.ResetAsync();
        factory.PeakSource.Returns(SourceRecords.Valid("Q192580", "Aneto", 3404, 42.6316, 0.6577));
        await RunAsync();

        factory.PeakSource.Returns(SourceRecords.Valid("Q192580", "Pico de Aneto", 3404, 42.6316, 0.6577));
        await RunAsync(IngestionMode.Full);

        List<string> types = await factory.QueryAsync(context =>
            context.Set<OutboxMessage>().Select(message => message.Type).ToListAsync());
        types.Should().HaveCount(3);
        types.Should().Contain(type => type.Contains("PeakRenamedDomainEvent", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Ingestion_StoresTheAlternativeNamesOfEachPeak()
    {
        await factory.ResetAsync();
        factory.PeakSource.Returns(Matterhorn());

        await RunAsync();

        Peak peak = await factory.QueryAsync(context =>
            context.Peaks.Include(item => item.AlternativeNames).SingleAsync());
        peak.AlternativeNames.Select(name => name.Name)
            .Should().BeEquivalentTo(["Cervino", "Cervin", "Monte Cervino"]);
    }

    [Fact]
    public async Task Ingestion_MakesThePeakSearchableByItsAlternativeName()
    {
        await factory.ResetAsync();
        factory.PeakSource.Returns(Matterhorn());
        await RunAsync();

        HttpResponseMessage response = await factory.CreateClient().GetAsync("/api/peaks/search?q=cervino");

        PagedResponse<PeakListItemResponse>? body = await response.ReadAsync<PagedResponse<PeakListItemResponse>>();
        body!.Items.Should().ContainSingle(item => item.Name == "Matterhorn");
    }

    [Fact]
    public async Task Ingestion_WithAnAlternativeNameRemovedAtSource_StopsMatchingIt()
    {
        await factory.ResetAsync();
        factory.PeakSource.Returns(Matterhorn());
        await RunAsync();

        factory.PeakSource.Returns(Matterhorn([new PeakNameDraft("fr", "Cervin", true)]));
        PeakIngestionRunResponse second = await RunAsync(IngestionMode.Full);

        second.PeaksUpdated.Should().Be(1);
        Peak peak = await factory.QueryAsync(context =>
            context.Peaks.Include(item => item.AlternativeNames).SingleAsync());
        peak.AlternativeNames.Select(name => name.Name).Should().BeEquivalentTo(["Cervin"]);
    }

    [Fact]
    public async Task Ingestion_RunTwiceWithTheSameAlternativeNames_ChangesNothing()
    {
        await factory.ResetAsync();
        factory.PeakSource.Returns(Matterhorn());
        await RunAsync();

        PeakIngestionRunResponse second = await RunAsync(IngestionMode.Full);

        (second.PeaksCreated, second.PeaksUpdated).Should().Be((0, 0));
    }

    private static PeakSourceRecord Matterhorn(IReadOnlyList<PeakNameDraft>? alternativeNames = null) =>
        SourceRecords.Valid(
            "Q1291",
            "Matterhorn",
            4478,
            45.9763,
            7.6586,
            countryCode: "CH",
            region: "Valais",
            alternativeNames: alternativeNames ??
            [
                new PeakNameDraft("it", "Cervino", true),
                new PeakNameDraft("fr", "Cervin", true),
                new PeakNameDraft("it", "Monte Cervino", false)
            ]);

    [Fact]
    public async Task Ingestion_PublishesEveryOutboxMessageToTheBroker()
    {
        await factory.ResetAsync();
        factory.PeakSource.Returns(SourceRecords.Valid("Q192580", "Aneto", 3404, 42.6316, 0.6577));
        await RunAsync();

        factory.PeakSource.Returns(SourceRecords.Valid("Q192580", "Pico de Aneto", 3410, 42.6316, 0.6577));
        await RunAsync(IngestionMode.Full);

        List<OutboxMessage> published = await WaitForPublishedMessagesAsync(expected: 3);

        published.Should().HaveCount(3);
        published.Should().OnlyContain(message => message.Error == null);
    }

    private async Task<List<OutboxMessage>> WaitForPublishedMessagesAsync(int expected)
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(60));

        while (!timeout.IsCancellationRequested)
        {
            List<OutboxMessage> processed = await factory.QueryAsync(context =>
                context.Set<OutboxMessage>().Where(message => message.ProcessedAtUtc != null).ToListAsync());

            if (processed.Count >= expected)
            {
                return processed;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), CancellationToken.None);
        }

        return await factory.QueryAsync(context =>
            context.Set<OutboxMessage>().Where(message => message.ProcessedAtUtc != null).ToListAsync());
    }

    private async Task<PeakIngestionRunResponse> RunAsync(IngestionMode mode = IngestionMode.Automatic)
    {
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

        Result<PeakIngestionRunResponse> result = await sender.Send(new RunPeakIngestionCommand(mode));

        result.IsSuccess.Should().BeTrue();

        return result.Value;
    }
}
