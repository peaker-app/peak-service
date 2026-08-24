using Common.Infrastructure.Observability;
using PeakService.Application;
using PeakService.Infrastructure;

namespace PeakService.Ingestion;

internal static class IngestionHost
{
    private static async Task Main(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        builder.AddCommonSerilog("peak-ingestion");
        builder.AddCommonOpenTelemetry("peak-ingestion");

        builder.Services.AddApplication(typeof(PeakIngestionWorker).Assembly);
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddIngestion(builder.Configuration);

        builder.Services.Configure<PeakIngestionWorkerOptions>(
            builder.Configuration.GetSection(PeakIngestionWorkerOptions.SectionName));
        builder.Services.AddHostedService<PeakIngestionWorker>();

        IHost host = builder.Build();

        await host.RunAsync();
    }
}
