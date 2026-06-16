using ChargingService.Repositories;
using ChargingService.Repositories.Interfaces;
using ChargingService.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

builder.Services.AddSingleton<IChargingRepository, ChargingRepository>();

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5185, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

var app = builder.Build();

app.MapGrpcService<ChargingGrpcService>();

if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

app.MapGet("/", () => "ChargingService gRPC em execução.");

app.Run();
