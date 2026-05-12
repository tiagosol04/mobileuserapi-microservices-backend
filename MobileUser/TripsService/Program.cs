using TripsService.Repositories;
using TripsService.Repositories.Interfaces;
using TripsService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

builder.Services.AddSingleton<ITripRepository, TripRepository>();

var app = builder.Build();

app.MapGrpcService<TripsGrpcService>();
app.MapGrpcReflectionService();

app.MapGet("/", () => "Trips Service em execução.");

app.Run();