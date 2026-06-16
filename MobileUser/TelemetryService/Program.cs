using Microsoft.AspNetCore.Server.Kestrel.Core;
using TelemetryService.Repositories;
using TelemetryService.Repositories.Interfaces;
using TelemetryService.Services;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

builder.Services.AddSingleton<ITelemetryRepository, TelemetryRepository>();

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5066, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

var app = builder.Build();

app.MapGrpcService<TelemetryGrpcService>();

if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

app.MapGet("/", () => "Telemetry Service em execução.");

app.Run();