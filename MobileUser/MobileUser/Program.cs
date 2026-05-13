using Microsoft.AspNetCore.Server.Kestrel.Core;
using MobileUser.Repositories;
using MobileUser.Repositories.Interfaces;
using MobileUser.Services;
using MobileUser.Services.EmailService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();
builder.Services.AddSingleton<IDelegationsRepository, DelegationsRepository>();
builder.Services.AddSingleton<IEmailService, EmailService>();

// Singleton para manter o estado em memória entre chamadas
builder.Services.AddSingleton<IMotasRepository, MotasRepository>();

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