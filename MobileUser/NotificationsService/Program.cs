using Microsoft.AspNetCore.Server.Kestrel.Core;
using NotificationsService.Repositories;
using NotificationsService.Repositories.Interfaces;
using NotificationsService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

builder.Services.AddSingleton<INotificationRepository, NotificationRepository>();

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5183, listenOptions =>
    {
        listenOptions.UseHttps();
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

var app = builder.Build();

app.MapGrpcService<NotificationsGrpcService>();

if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

app.MapGet("/", () => "Notifications Service em execução.");

app.Run();
