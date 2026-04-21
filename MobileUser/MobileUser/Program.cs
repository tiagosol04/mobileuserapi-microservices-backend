using MobileUser.Repositories;
using MobileUser.Repositories.Interfaces;
using MobileUser.Services;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

builder.Services.AddScoped<IMotasRepository, MotasRepository>();

var app = builder.Build();

app.MapGrpcService<MotasGrpcService>();

app.MapGet("/", () => "MobileUser gRPC API em execução.");

app.Run();