using TelemetryService.Repositories;
using TelemetryService.Repositories.Interfaces;
using TelemetryService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

builder.Services.AddSingleton<ITelemetryRepository, TelemetryRepository>();

var app = builder.Build();

app.MapGrpcService<TelemetryGrpcService>();
app.MapGrpcReflectionService();

app.MapGet("/", () => "Telemetry Service em execução.");

app.Run();