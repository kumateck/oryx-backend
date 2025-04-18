using System.Collections.Concurrent;
using APP.Claims;
using APP.IRepository;
using APP.Repository;
using APP.Services.Background;
using APP.Services.Email;
using APP.Services.Pdf;
using APP.Services.Storage;
using APP.Services.Token;
using DinkToPdf;
using DinkToPdf.Contracts;
using DOMAIN.Entities.ActivityLogs;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using SHARED.Provider;
using SHARED.Services.Identity;
using StackExchange.Redis;

namespace APP;

public static class DependencyInjection
{
    public static void AddTransientServices(this IServiceCollection services)
    {
    }
    
    public static void AddScopedServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IBoMRepository, BoMRepository>();
        services.AddScoped<ICollectionRepository, CollectionRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductionScheduleRepository, ProductionScheduleRepository>();
        services.AddScoped<IWorkOrderRepository, WorkOrderRepository>();
        services.AddScoped<IConfigurationRepository, ConfigurationRepository>();
        services.AddScoped<IMaterialRepository, MaterialRepository>();
        services.AddScoped<IRequisitionRepository, RequisitionRepository>();
        services.AddScoped<IApprovalRepository, ApprovalRepository>();
        services.AddScoped<IProcurementRepository, ProcurementRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<IFileRepository, FileRepository>();
        services.AddScoped<IFormRepository, FormRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IDesignationRepository, DesignationRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        
        
        services.AddScoped<IBlobStorageService, BlobStorageService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<ITenantProvider, TenantProvider>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IPdfService, PdfService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IBackgroundWorkerService, BackgroundWorkerService>();
        services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
    }

    public static void AddSingletonServices(this IServiceCollection services)
    {
        var redisConnectionString = Environment.GetEnvironmentVariable("redisConnectionString") ?? "localhost:6379,abortConnect=false";
        services.AddSingleton<IConnectionMultiplexer>(_ => 
            ConnectionMultiplexer.Connect(redisConnectionString));
        services.AddSingleton(sp => 
            sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());
        services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));
        services.AddSingleton<MongoDbContext>();
        services.AddHostedService<ConsumeBackgroundWorkerService>();
        services.AddSingleton<ConcurrentQueue<CreateActivityLog>>();
        services.AddSingleton<ConcurrentQueue<PrevStateCaptureRequest>>();
    }
}