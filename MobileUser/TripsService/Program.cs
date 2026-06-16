using Microsoft.AspNetCore.Server.Kestrel.Core;
using TripsService.Repositories;
using TripsService.Repositories.Interfaces;
using TripsService.Services;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

builder.Services.AddSingleton<ITripRepository, TripRepository>();

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5278, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});


var app = builder.Build();

app.MapGrpcService<TripsGrpcService>();

if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

app.MapGet("/", () => "Trips Service em execução.");

app.Run();