using Microsoft.AspNetCore.Server.Kestrel.Core;
using UserService.Repositories;
using UserService.Repositories.Interfaces;
using UserService.Services;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

builder.Services.AddSingleton<IUserRepository, UserRepository>();


builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5182, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

var app = builder.Build();

app.MapGrpcService<UserGrpcService>();

if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

app.MapGet("/", () => "User Service em execução.");

app.Run();
