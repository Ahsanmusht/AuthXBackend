using AuthX.API.Services;
using AuthX.Core.Interfaces;
using AuthX.Services.BackgroundJobs;
using AuthX.Services.Helpers;
using AuthX.Services.Implementations;
using Hangfire;
using Hangfire.SqlServer;

namespace AuthX.API.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services, IConfiguration config)
    {
        // ── Core services ─────────────────────────────────
        services.AddScoped<IJwtHelper,           JwtHelper>();
        services.AddScoped<IAuthService,         AuthService>();
        services.AddScoped<IUserService,         UserService>();
        services.AddScoped<IRoleService,         RoleService>();
        services.AddScoped<ICategoryService,     CategoryService>();
        services.AddScoped<IProductService,      ProductService>();
        services.AddScoped<IBatchService,        BatchService>();
        services.AddScoped<IQRService,           QRService>();
        services.AddScoped<IDispatchService,     DispatchService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IClaimService,        ClaimService>();
        services.AddScoped<IDashboardService,    DashboardService>();
        services.AddScoped<QRGenerationJob>();
services.AddScoped<PrintProcessingJob>();
services.AddScoped<ScanLogCleanupJob>();
services.AddScoped<IReturnReasonService, ReturnReasonService>();
services.AddScoped<IProductConditionService, ProductConditionService>();
services.AddScoped<IMenuService,      MenuService>();
services.AddScoped<IPromotionService, PromotionService>();

        // ── SignalR push service ───────────────────────────
        services.AddScoped<ISignalRService, SignalRService>();

        // ── Hangfire ──────────────────────────────────────
        services.AddHangfire(hf => hf
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(
                config.GetConnectionString("DefaultConnection"),
                new SqlServerStorageOptions
                {
                    CommandBatchMaxTimeout       = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout   = TimeSpan.FromMinutes(5),
                    QueuePollInterval            = TimeSpan.Zero,
                    UseRecommendedIsolationLevel = true,
                    DisableGlobalLocks           = true
                }));

        services.AddHangfireServer(opt =>
        {
            opt.WorkerCount   = Environment.ProcessorCount * 2;
            opt.Queues        = ["qr_generation", "print_jobs", "default"];
        });

        return services;
    }
}