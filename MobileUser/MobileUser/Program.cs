using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.IdentityModel.Tokens;
using MobileUser.Repositories;
using MobileUser.Repositories.Interfaces;
using MobileUser.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

builder.Services.AddSingleton<IMotasRepository, MotasRepository>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<MotasGrpcService>();

if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();

    app.MapGet("/dev/token", (IConfiguration config) =>
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[] { new Claim("sub", "user-diana-001") };
        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(config["Jwt:ExpirationMinutes"]!)),
            signingCredentials: creds);
        return Results.Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
    });
}

app.MapGet("/", () => "MobileUser gRPC API em execução.");

app.Run();
