using Microsoft.AspNetCore.Server.Kestrel.Core;
using MobileUser.Repositories;
using MobileUser.Repositories.Interfaces;
using MobileUser.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

builder.Services.AddSingleton<IMotasRepository, MotasRepository>();

builder.Services.AddGrpcClient<MotoService.MotoService.MotoServiceClient>(o =>
    o.Address = new Uri(builder.Configuration["ServiceAddresses:MotoService"]!));

builder.Services.AddGrpcClient<TelemetryService.Grpc.TelemetryService.TelemetryServiceClient>(o =>
    o.Address = new Uri(builder.Configuration["ServiceAddresses:TelemetryService"]!));

builder.Services.AddGrpcClient<TripsService.Grpc.TripsService.TripsServiceClient>(o =>
    o.Address = new Uri(builder.Configuration["ServiceAddresses:TripsService"]!));

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5048, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

var app = builder.Build();

app.MapGrpcService<MotasGrpcService>();

if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

app.MapGet("/", () => "MobileUser gRPC API em execução.");

app.Run();