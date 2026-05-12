using MotoService.Repositories;
using MotoService.Repositories.Interfaces;
using MotoService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

builder.Services.AddSingleton<IMotoRepository, MotoRepository>();

var app = builder.Build();

app.MapGrpcService<MotoGrpcService>();
app.MapGrpcReflectionService();

app.MapGet("/", () => "Moto Service em execução.");

app.Run();