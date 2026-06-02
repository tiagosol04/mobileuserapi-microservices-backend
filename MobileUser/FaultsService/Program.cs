using FaultsService.Repositories;
using FaultsService.Repositories.Interfaces;
using FaultsService.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

builder.Services.AddSingleton<IFaultsRepository, FaultsRepository>();

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5186, listenOptions =>
    {
        listenOptions.UseHttps();
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

var app = builder.Build();

app.MapGrpcService<FaultsGrpcService>();

if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

app.MapGet("/", () => "FaultsService gRPC em execução.");

app.Run();
