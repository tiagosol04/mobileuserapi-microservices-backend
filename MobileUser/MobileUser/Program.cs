using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.IdentityModel.Tokens;
using MobileUser.Repositories;
using MobileUser.Repositories.Interfaces;
using MobileUser.Services;
using MobileUser.Services.EmailService;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();
builder.Services.AddSingleton<IDelegationsRepository, DelegationsRepository>();
builder.Services.AddSingleton<IEmailService, EmailService>();

builder.Services.AddSingleton<IDealershipRepository, DealershipRepository>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
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

builder.Services.AddGrpcClient<UserService.Grpc.UserService.UserServiceClient>(o =>
    o.Address = new Uri(builder.Configuration["ServiceAddresses:UserService"]!));

builder.Services.AddGrpcClient<NotificationsService.Grpc.NotificationsService.NotificationsServiceClient>(o =>
    o.Address = new Uri(builder.Configuration["ServiceAddresses:NotificationsService"]!));

builder.Services.AddGrpcClient<MaintenanceService.Grpc.MaintenanceService.MaintenanceServiceClient>(o =>
    o.Address = new Uri(builder.Configuration["ServiceAddresses:MaintenanceService"]!));

builder.Services.AddGrpcClient<ChargingService.Grpc.ChargingService.ChargingServiceClient>(o =>
    o.Address = new Uri(builder.Configuration["ServiceAddresses:ChargingService"]!));

builder.Services.AddGrpcClient<FaultsService.Grpc.FaultsService.FaultsServiceClient>(o =>
    o.Address = new Uri(builder.Configuration["ServiceAddresses:FaultsService"]!));

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

// Fluxo principal de autenticação mock.
// Em produção, substituir por integração com IdP externo (Keycloak, OIDC).
app.MapPost("/auth/login", (LoginRequest req, IConfiguration config) =>
{
    if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
        return Results.Json(new { error = "Credenciais inválidas." }, statusCode: 401);

    var mockUsers = config.GetSection("MockUsers").Get<MockUser[]>() ?? [];

    var user = mockUsers.FirstOrDefault(u =>
        string.Equals(u.Username, req.Username.Trim(), StringComparison.OrdinalIgnoreCase)
        && u.Password == req.Password);

    if (user is null)
        return Results.Json(new { error = "Credenciais inválidas." }, statusCode: 401);

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var claims = new[]
    {
        new Claim("sub", user.UserId),
        new Claim("name", user.Name),
        new Claim("email", user.Email),
        new Claim("preferred_username", user.Username)
    };
    var token = new JwtSecurityToken(
        issuer: config["Jwt:Issuer"],
        audience: config["Jwt:Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(double.Parse(config["Jwt:ExpirationMinutes"]!)),
        signingCredentials: creds);

    return Results.Ok(new
    {
        token = new JwtSecurityTokenHandler().WriteToken(token),
        userId = user.UserId,
        username = user.Username
    });
});

if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();

    // Atalho de desenvolvimento: gera token para user-diana-001 sem credenciais.
    // Não substituir pelo /auth/login — este endpoint não deve existir fora de Development.
    app.MapGet("/dev/token", (IConfiguration config) =>
    {
        var mockUsers = config.GetSection("MockUsers").Get<MockUser[]>() ?? [];
        var diana = mockUsers.FirstOrDefault(u => u.UserId == "user-diana-001");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = diana is not null
            ? new[]
            {
                new Claim("sub", diana.UserId),
                new Claim("name", diana.Name),
                new Claim("email", diana.Email),
                new Claim("preferred_username", diana.Username)
            }
            : new[] { new Claim("sub", "user-diana-001") };
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

record LoginRequest(string Username, string Password);
record MockUser(string Username, string Password, string UserId, string Name, string Email);
