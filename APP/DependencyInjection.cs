using System.Collections.Concurrent;
using APP.Claims;
using APP.IRepository;
using APP.Repository;
using APP.Services.Background;
using APP.Services.Email;
using APP.Services.Message;
using APP.Services.NotificationService;
using APP.Services.Pdf;
using APP.Services.Storage;
using APP.Services.Token;
using DinkToPdf;
using DinkToPdf.Contracts;
using DOMAIN.Entities.ActivityLogs;
using DOMAIN.Entities.Notifications;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using MassTransit;
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
    
     public static void AddInfrastructure(this IServiceCollection services)
    {
        //add mass transit
        var rabbitHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST");
        var rabbitUserName = Environment.GetEnvironmentVariable("RABBITMQ_DEFAULT_USER");
        var rabbitPassword = Environment.GetEnvironmentVariable("RABBITMQ_DEFAULT_PASS");

        services.AddMassTransit(configure =>
        {
            configure.SetKebabCaseEndpointNameFormatter();
            
            configure.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitHost ?? throw new ArgumentException("Invalid rabbit host name"), h =>
                {
                    h.Username(rabbitUserName ?? throw new ArgumentException("Invalid rabbit username"));
                    h.Password(rabbitPassword ?? throw new ArgumentException("Invalid rabbit password"));
                });
        
                cfg.ReceiveEndpoint("push_notification_queue", e =>
                {
                    e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
                    e.UseMessageRetry(r =>
                    {
                        r.Immediate(5); 
                    });
                });
                cfg.ConfigureEndpoints(context);
            });
        });
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
        services.AddScoped<ILeaveEntitlementRepository, LeaveEntitlementRepository>();
        services.AddScoped<ILeaveTypeRepository, LeaveTypeRepository>();
        services.AddScoped<ILeaveRequestRepository, LeaveRequestRepository>();
        services.AddScoped<IShiftTypeRepository, ShiftTypeRepository>();
        services.AddScoped<IShiftScheduleRepository, ShiftScheduleRepository>();
        services.AddScoped<ICompanyWorkingDaysRepository, CompanyWorkingDaysRepository>();
        services.AddScoped<IHolidayRepository, HolidayRepository>();
        services.AddScoped<ICountryRepository, CountryRepository>();
        services.AddScoped<IOvertimeRequestRepository, OvertimeRequestRepository>();
        services.AddScoped<IMaterialStandardTestProcedureRepository, MaterialStandardTestProcedureRepository>();
        services.AddScoped<IProductStandardTestProcedureRepository, ProductStandardTestProcedureRepository>();
        services.AddScoped<IMaterialAnalyticalRawDataRepository, MaterialAnalyticalRawDataRepository>();
        services.AddScoped<IProductAnalyticalRawDataRepository, ProductAnalyticalRawDataRepository>();
        services.AddScoped<IAnalyticalTestRequestRepository, AnalyticalTestRequestRepository>();
        services.AddScoped<IStaffRequisitionRepository, StaffRequisitionRepository>();
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddScoped<IAlertRepository, AlertRepository>();
        services.AddScoped<IProductSamplingRepository, ProductSamplingRepository>();
        services.AddScoped<IMaterialSamplingRepository, MaterialSamplingRepository>();
        services.AddScoped<IMaterialSpecificationRepository, MaterialSpecificationRepository>();
        services.AddScoped<IProductSpecificationRepository, ProductSpecificationRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IProductionOrderRepository, ProductionOrderRepository>();
        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<IServiceProviderRepository, ServiceProviderRepository>();
        services.AddScoped<IVendorRepository, VendorRepository>();
        services.AddScoped<IItemStockRequisitionRepository, ItemStockRequisitionRepository>();
        services.AddScoped<IInventoryProcurementRepository, InventoryProcurementRepository>();
        
        
        services.AddScoped<IBlobStorageService, BlobStorageService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<ITenantProvider, TenantProvider>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IPdfService, PdfService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IBackgroundWorkerService, BackgroundWorkerService>();
        services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
        services.AddScoped<IMessagingService, MessagingService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddHostedService<ApprovalEscalationService>();
        services.AddHostedService<LeaveExpiryService>();
        services.AddHostedService<ServiceExpiryService>();
        services.AddHostedService<MaterialStockService>();
        services.AddHostedService<EmployeeSuspensionService>();
    }

    public static void AddSingletonServices(this IServiceCollection services)
    {
        var redisConnectionString = Environment.GetEnvironmentVariable("redisConnectionString") ?? "localhost:6380,abortConnect=false";
        services.AddSingleton<IConnectionMultiplexer>(_ => 
            ConnectionMultiplexer.Connect(redisConnectionString));
        services.AddSingleton(sp => 
            sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());
        services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));
        services.AddSingleton<MongoDbContext>();
        services.AddHostedService<ConsumeBackgroundWorkerService>();
        services.AddSingleton<ConcurrentQueue<CreateActivityLog>>();
        services.AddSingleton<ConcurrentQueue<PrevStateCaptureRequest>>();
        services.AddSingleton<ConcurrentQueue<(string message, NotificationType type, Guid? departmentId, List<User> users)>>();
    }
}