using Microsoft.AspNetCore.Server.Kestrel.Core;
using MotoService.Repositories;
using MotoService.Repositories.Interfaces;
using MotoService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

builder.Services.AddSingleton<IMotoRepository, MotoRepository>();

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5294, listenOptions =>
    {
        listenOptions.UseHttps();
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

var app = builder.Build();

app.MapGrpcService<MotoGrpcService>();

if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

app.MapGet("/", () => "Moto Service em execução.");

app.Run();