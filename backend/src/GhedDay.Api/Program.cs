using System.Text;
using GhedDay.Api.Auth;
using GhedDay.Api.Hubs;
using GhedDay.Api.Middleware;
using GhedDay.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using GhedDay.Application;
using GhedDay.Application.Common;
using GhedDay.Application.Services;
using GhedDay.Infrastructure;
using GhedDay.Infrastructure.Data;
using GhedDay.Infrastructure.Jobs;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;
var corsOrigin = configuration["Cors:AllowedOrigin"] ?? "http://localhost:3000";

// ---------- Application + Infrastructure ----------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(configuration);

// Per-request tenant context (resolved from JWT claims by TenantResolutionMiddleware).
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

// SignalR + notification dispatch.
builder.Services.AddSignalR();
builder.Services.AddScoped<INotificationService, SignalRNotificationService>();

// ---------- Auth: JWT bearer ----------
builder.Services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

var jwtKey = configuration["Jwt:Key"] ?? "dev-only-insecure-signing-key-change-me-please-32+chars";
var jwtIssuer = configuration["Jwt:Issuer"] ?? "ghedday";
var jwtAudience = configuration["Jwt:Audience"] ?? "ghedday";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };

        // SignalR sends the access token via query string on the WebSocket handshake.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ---------- CORS ----------
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(corsOrigin)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

// ---------- Hangfire (Postgres storage) ----------
var connectionString = configuration.GetConnectionString("Postgres");
if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));
    builder.Services.AddHangfireServer();
}

// ---------- Health checks ----------
builder.Services.AddHealthChecks()
    .AddDbContextCheck<GhedDayDbContext>("database");

// ---------- Controllers + Swagger ----------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ---------- Pipeline ----------
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapHub<BookingHub>("/hubs/booking");
app.MapHealthChecks("/health").AllowAnonymous();

if (!string.IsNullOrWhiteSpace(connectionString))
{
    app.UseHangfireDashboard("/hangfire");
    RecurringJob.AddOrUpdate<ProcessedEventCleanupJob>(
        "processed-events-cleanup", job => job.RunAsync(CancellationToken.None), Cron.Daily);
}

// ---------- Dev: migrate + seed ----------
if (app.Environment.IsDevelopment() && !string.IsNullOrWhiteSpace(connectionString))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<GhedDayDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db, pw => passwordHasher.HashPassword(new User(), pw));
}

app.Run();

public partial class Program;
