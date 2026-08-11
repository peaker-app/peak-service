using System.Text.Json.Serialization;
using Common.API.Caching;
using Common.API.Documentation;
using Common.API.Health;
using Common.API.Middlewares;
using Common.Infrastructure.Observability;
using PeakService.API;
using PeakService.Application;
using PeakService.Infrastructure;
using PeakService.Infrastructure.Persistence;
using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddCommonSerilog("peak-service");
builder.AddCommonOpenTelemetry("peak-service");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddEventPublishing(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddCommonSwagger("peak-service");

builder.Services.AddHealthChecks().AddDbContextCheck<PeakDbContext>();

WebApplication app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();
app.UseCatalogEntityTags(CatalogCache.Policy);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapCommonHealthChecks();

await app.RunAsync();
