using System.Net.Http.Headers;
using Common.Application.Abstractions;
using Common.Infrastructure.Messaging;
using Common.Infrastructure.Persistence;
using Common.Infrastructure.Persistence.Outbox;
using Common.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PeakService.Application.Abstractions;
using PeakService.Domain.MountainRanges;
using PeakService.Domain.PeakIngestionRuns;
using PeakService.Domain.Peaks;
using PeakService.Domain.Peaks.Events;
using PeakService.Infrastructure.Deduplication;
using PeakService.Infrastructure.ExternalServices;
using PeakService.Infrastructure.Messaging;
using PeakService.Infrastructure.Persistence;
using PeakService.Infrastructure.Persistence.Repositories;

namespace PeakService.Infrastructure;

public static class DependencyInjection
{
    private const string SparqlResultsMediaType = "application/sparql-results+json";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<AuditableEntityInterceptor>();
        services.AddSingleton<OutboxInterceptor>();
        services.Configure<OutboxOptions>(configuration.GetSection(OutboxOptions.SectionName));

        services.AddPeakDbContext();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<PeakDbContext>());
        services.AddScoped<IPeakRepository, PeakRepository>();
        services.AddScoped<IMountainRangeRepository, MountainRangeRepository>();
        services.AddScoped<IPeakIngestionRunRepository, PeakIngestionRunRepository>();
        services.AddScoped<IPeakCatalogReader, PeakCatalogReader>();

        return services;
    }

    public static IServiceCollection AddEventPublishing(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEventBus(configuration);

        services.AddScoped<IDomainEventHandler<PeakCreatedDomainEvent>, PeakCreatedDomainEventHandler>();
        services.AddScoped<IDomainEventHandler<PeakUpdatedDomainEvent>, PeakUpdatedDomainEventHandler>();
        services.AddScoped<IDomainEventHandler<PeakRenamedDomainEvent>, PeakRenamedDomainEventHandler>();

        services.AddHostedService<OutboxProcessor<PeakDbContext>>();

        return services;
    }

    public static IServiceCollection AddIngestion(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<IngestionOptions>()
            .Bind(configuration.GetSection(IngestionOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<IPeakDeduplicationStrategy, WikidataThenProximityDeduplicationStrategy>();
        services.AddPeakSourceClient();

        return services;
    }

    private static void AddPeakSourceClient(this IServiceCollection services) =>
        services.AddHttpClient<IPeakSourceClient, WikidataPeakSourceClient>(ConfigureSourceClient)
            .AddStandardResilienceHandler(resilience =>
            {
                resilience.AttemptTimeout.Timeout = TimeSpan.FromMinutes(2);
                resilience.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(4);
                resilience.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(6);
            });

    private static void ConfigureSourceClient(IServiceProvider provider, HttpClient client)
    {
        IngestionOptions settings = provider.GetRequiredService<IOptions<IngestionOptions>>().Value;

        client.BaseAddress = settings.Endpoint;
        client.Timeout = settings.RequestTimeout;
        client.DefaultRequestHeaders.UserAgent.ParseAdd(settings.UserAgent);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(SparqlResultsMediaType));
    }

    private static void AddPeakDbContext(this IServiceCollection services) =>
        services.AddDbContext<PeakDbContext>((provider, options) => options
            .UseNpgsql(
                ResolveConnectionString(provider),
                npgsql => npgsql.UseNetTopologySuite())
            .AddInterceptors(
                provider.GetRequiredService<AuditableEntityInterceptor>(),
                provider.GetRequiredService<OutboxInterceptor>()));

    private static string ResolveConnectionString(IServiceProvider provider)
    {
        string? connectionString = provider.GetRequiredService<IConfiguration>()
            .GetConnectionString("PeakDatabase");

        return string.IsNullOrWhiteSpace(connectionString)
            ? throw new InvalidOperationException("Connection string 'PeakDatabase' is not configured.")
            : connectionString;
    }
}
