using System.Globalization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PeakService.Application.Abstractions;
using PeakService.Domain.MountainRanges;
using PeakService.Domain.Peaks;
using PeakService.Infrastructure;
using PeakService.Infrastructure.Persistence;
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

    public FakePeakSourceClient PeakSource { get; } = new();

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
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PeakDatabase"] = _postgres.GetConnectionString(),
                ["Messaging:Host"] = _rabbitMq.Hostname,
                ["Messaging:Port"] = _rabbitMq.GetMappedPublicPort(5672).ToString(CultureInfo.InvariantCulture),
                ["Messaging:Username"] = "peaker",
                ["Messaging:Password"] = "peaker",
                ["Messaging:VirtualHost"] = "/",
                ["Outbox:PollingInterval"] = "00:00:01",
                ["Ingestion:Endpoint"] = "https://query.wikidata.org/sparql",
                ["Ingestion:UserAgent"] = "PeakerIngestionTests/1.0 (tests)"
            }));

        builder.ConfigureServices((context, services) =>
        {
            services.AddIngestion(context.Configuration);
            services.RemoveAll<IPeakSourceClient>();
            services.AddSingleton<IPeakSourceClient>(PeakSource);
        });
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _rabbitMq.StartAsync());

        using IServiceScope scope = Services.CreateScope();
        PeakDbContext context = scope.ServiceProvider.GetRequiredService<PeakDbContext>();
        await context.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _rabbitMq.DisposeAsync();
        await base.DisposeAsync();
    }
}
