using MaintenanceService.Repositories;
using MaintenanceService.Repositories.Interfaces;
using MaintenanceService.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

builder.Services.AddSingleton<IMaintenanceRepository, MaintenanceRepository>();

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5184, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

var app = builder.Build();

app.MapGrpcService<MaintenanceGrpcService>();

if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

app.MapGet("/", () => "MaintenanceService gRPC em execução.");

app.Run();
