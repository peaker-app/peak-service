using Common.Infrastructure.Observability;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PeakService.Application;
using PeakService.Infrastructure;
using PeakService.Ingestion;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.AddCommonSerilog("peak-ingestion");
builder.AddCommonOpenTelemetry("peak-ingestion");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddIngestion(builder.Configuration);

builder.Services.Configure<PeakIngestionWorkerOptions>(
    builder.Configuration.GetSection(PeakIngestionWorkerOptions.SectionName));
builder.Services.AddHostedService<PeakIngestionWorker>();

IHost host = builder.Build();

await host.RunAsync();
