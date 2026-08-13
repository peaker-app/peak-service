using System.Globalization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using PeakService.Application;
using PeakService.Application.Abstractions;
using PeakService.Domain.MountainRanges;
using PeakService.Domain.Peaks;
using PeakService.Infrastructure;
using PeakService.Infrastructure.Persistence;
using PeakService.Ingestion.RunPeakIngestion;
using PeakService.IntegrationTests.Fakes;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace PeakService.IntegrationTests;

public sealed class PeakServiceApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgis/postgis:16-3.4")
        .WithDatabase("peaker_peaks")
        .WithUsername("peaker")
        .WithPassword("peaker")
        .Build();

    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder("rabbitmq:4.3.3-management-alpine")
        .WithUsername("peaker")
        .WithPassword("peaker")
        .Build();

    private IHost? _ingestionHost;

    public FakePeakSourceClient PeakSource { get; } = new();

    public IServiceProvider IngestionServices =>
        _ingestionHost?.Services ?? throw new InvalidOperationException("The ingestion host is not built yet.");

    public async Task ResetAsync()
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        PeakDbContext context = scope.ServiceProvider.GetRequiredService<PeakDbContext>();

        await context.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE peaks, peak_names, mountain_ranges, peak_ingestion_runs, outbox_messages
            RESTART IDENTITY CASCADE;
            """);
    }

    public async Task SeedAsync(params Peak[] peaks)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        PeakDbContext context = scope.ServiceProvider.GetRequiredService<PeakDbContext>();

        context.Peaks.AddRange(peaks);
        await context.SaveChangesAsync();
    }

    public async Task SeedRangeAsync(MountainRange range)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        PeakDbContext context = scope.ServiceProvider.GetRequiredService<PeakDbContext>();

        context.MountainRanges.Add(range);
        await context.SaveChangesAsync();
    }

    public async Task<TResult> QueryAsync<TResult>(Func<PeakDbContext, Task<TResult>> query)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        PeakDbContext context = scope.ServiceProvider.GetRequiredService<PeakDbContext>();

        return await query(context);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(SharedSettings()));
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _rabbitMq.StartAsync());

        using IServiceScope scope = Services.CreateScope();
        PeakDbContext context = scope.ServiceProvider.GetRequiredService<PeakDbContext>();
        await context.Database.MigrateAsync();

        _ingestionHost = BuildIngestionHost();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        _ingestionHost?.Dispose();

        await _postgres.DisposeAsync();
        await _rabbitMq.DisposeAsync();
        await base.DisposeAsync();
    }

    private IHost BuildIngestionHost()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();

        builder.Configuration.AddInMemoryCollection(SharedSettings());

        builder.Services.AddApplication(typeof(RunPeakIngestionCommand).Assembly);
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddIngestion(builder.Configuration);

        builder.Services.RemoveAll<IPeakSourceClient>();
        builder.Services.AddSingleton<IPeakSourceClient>(PeakSource);

        return builder.Build();
    }

    private Dictionary<string, string?> SharedSettings() => new()
    {
        ["ConnectionStrings:PeakDatabase"] = _postgres.GetConnectionString(),
        ["Messaging:Host"] = _rabbitMq.Hostname,
        ["Messaging:Port"] = _rabbitMq.GetMappedPublicPort(5672).ToString(CultureInfo.InvariantCulture),
        ["Messaging:Username"] = "peaker",
        ["Messaging:Password"] = "peaker",
        ["Messaging:VirtualHost"] = "/",
        ["Outbox:PollingInterval"] = "00:00:01",
        ["Outbox:RetryBackoffBase"] = "00:00:01",
        ["Outbox:RetryBackoffCap"] = "00:00:01",
        ["Ingestion:Endpoint"] = "https://query.wikidata.org/sparql",
        ["Ingestion:UserAgent"] = "PeakerIngestionTests/1.0 (tests)"
    };
}
