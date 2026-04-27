using System.Text;
using AuthX.API.Extensions;
using AuthX.API.Filters;
using AuthX.API.Hubs;
using AuthX.API.Middleware;
using AuthX.Infrastructure.Cache;
using AuthX.Infrastructure.Data;
using AuthX.Infrastructure.Repositories;
using AuthX.Core.Interfaces;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Threading.RateLimiting;
using AuthX.Services.BackgroundJobs;

// ─── Serilog Bootstrap ────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// ─── Database ─────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(
        builder.Configuration.GetConnectionString("Default"),
        sql =>
        {
            sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
            sql.CommandTimeout(120);
        }));

// ─── Redis ────────────────────────────────────────────────
builder.Services.AddStackExchangeRedisCache(opt =>
{
    opt.Configuration = builder.Configuration["Redis:Connection"];
    opt.InstanceName = "PV_";
});

builder.Services.AddSingleton<IRedisCacheService, RedisCacheService>();

// ─── Repository / UoW ────────────────────────────────────
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ─── All Application Services + Hangfire ─────────────────
builder.Services.AddApplicationServices(builder.Configuration);

// ─── JWT Authentication ───────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is missing in appsettings.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
var jwtAud = builder.Configuration["Jwt:Audience"]!;

builder.Services.AddAuthentication(opt =>
{
    opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(opt =>
{
    opt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAud,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    opt.Events = new JwtBearerEvents
    {
        OnMessageReceived = ctx =>
        {
            var token = ctx.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(token) &&
                ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                ctx.Token = token;
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// ─── SignalR + Redis backplane ────────────────────────────
builder.Services.AddSignalR(opt =>
{
    opt.EnableDetailedErrors = builder.Environment.IsDevelopment();
    opt.MaximumReceiveMessageSize = 102_400;
    opt.KeepAliveInterval = TimeSpan.FromSeconds(15);
    opt.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
})
.AddStackExchangeRedis(builder.Configuration["Redis:Connection"]!);

// ─── CORS ─────────────────────────────────────────────────
builder.Services.AddCors(opt =>
    opt.AddPolicy("ReactApp", p => p
        .WithOrigins(
            builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:5173"])
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

// ─── Rate Limiting ────────────────────────────────────────
builder.Services.AddRateLimiter(opt =>
{
    opt.AddPolicy("ScanLimit", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    opt.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// ─── Controllers ──────────────────────────────────────────
builder.Services.AddControllers(opt =>
{
    opt.Filters.Add<ValidationFilter>();
})
.AddJsonOptions(o =>
{
    o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    o.JsonSerializerOptions.DefaultIgnoreCondition =
        System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

// ─── Swagger ──────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ProductVerification API",
        Version = "v1",
        Description = "Product Verification & Digital Warranty System"
    });

    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {token}",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
    {{
        new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id   = "Bearer"
            }
        },
        Array.Empty<string>()
    }});
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();

// ═══════════════════════════════════════════════════════════
var app = builder.Build();
// ═══════════════════════════════════════════════════════════

JobScheduler.RegisterRecurringJobs();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PV API v1"));
}

app.UseSerilogRequestLogging();
app.UseCors("ReactApp");
app.UseRateLimiter();
app.UseMiddleware<ExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

// ─── Hangfire Dashboard ───────────────────────────────────
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireAuthFilter()]
});

app.MapControllers();

// ─── SignalR Hub ──────────────────────────────────────────
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();