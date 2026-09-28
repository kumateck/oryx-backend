using System.Collections.Concurrent;
using APP.Claims;
using APP.IRepository;
using APP.Repository;
using APP.Repository.QcWorksheets;
using APP.Services;
using APP.Services.QcWorksheets;
using APP.Services.QcWorksheets.WorksheetDocxImport;
using APP.Services.QcWorksheets.WorksheetDocxImport.AiExtraction;
using APP.Services.Background;
using APP.Services.Email;
using APP.Services.Formulas;
using APP.Services.JobRequests;
using APP.Services.Message;
using APP.Services.NotificationService;
using APP.Services.OnlyOffice;
using APP.Services.Pdf;
using APP.Services.ProductionActivityStepEventPublisher;
using APP.Services.Storage;
using APP.Services.StpDocuments;
using APP.Services.Token;
using DinkToPdf;
using DinkToPdf.Contracts;
using DOMAIN.Entities.ActivityLogs;
using DOMAIN.Entities.Notifications;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
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
                // No .NET consumer is attached to this queue - it exists purely so
                // RabbitMQ provisions the exchange the frontend's server.mjs binds to
                // for live production-board updates, same as push_notification_queue.
                cfg.ReceiveEndpoint("production_activity_board_queue", e =>
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
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<ISupplierRelationshipRepository, SupplierRelationshipRepository>();
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
        services.AddScoped<IShiftCategoryRepository, ShiftCategoryRepository>();
        services.AddScoped<IWorkingHoursPolicyRepository, WorkingHoursPolicyRepository>();
        services.AddScoped<ICompanyWorkingDaysRepository, CompanyWorkingDaysRepository>();
        services.AddScoped<IHolidayRepository, HolidayRepository>();
        services.AddScoped<ICountryRepository, CountryRepository>();
        services.AddScoped<IOvertimeRequestRepository, OvertimeRequestRepository>();
        services.AddScoped<IMaterialStandardTestProcedureRepository, MaterialStandardTestProcedureRepository>();
        services.AddScoped<IProductStandardTestProcedureRepository, ProductStandardTestProcedureRepository>();
        services.AddScoped<IMaterialAnalyticalRawDataRepository, MaterialAnalyticalRawDataRepository>();
        services.AddScoped<IProductAnalyticalRawDataRepository, ProductAnalyticalRawDataRepository>();
        services.AddScoped<IAnalyticalTestRequestRepository, AnalyticalTestRequestRepository>();
        services.AddScoped<IOosInvestigationRepository, OosInvestigationRepository>();
        services.AddScoped<IQualityAuditRepository, QualityAuditRepository>();
        services.AddScoped<IInstrumentRepository, InstrumentRepository>();
        services.AddScoped<IRndProjectRepository, RndProjectRepository>();
        services.AddScoped<IRndFormulationRepository, RndFormulationRepository>();
        services.AddScoped<IRndTrialBatchRepository, RndTrialBatchRepository>();
        services.AddScoped<IRndAnalyticalMethodRepository, RndAnalyticalMethodRepository>();
        services.AddScoped<IRndStabilityStudyRepository, RndStabilityStudyRepository>();
        services.AddScoped<IRndTechnologyTransferRepository, RndTechnologyTransferRepository>();
        services.AddScoped<IRndDevelopmentReportRepository, RndDevelopmentReportRepository>();
        services.AddScoped<IStaffRequisitionRepository, StaffRequisitionRepository>();
        services.AddScoped<IPayrollCalculationService, PayrollCalculationService>();
        services.AddScoped<IPayrollRepository, PayrollRepository>();
        services.AddScoped<IPerformanceRepository, PerformanceRepository>();
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
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
        services.AddScoped<IItemInventoryTransactionRepository, ItemInventoryTransactionRepository>();
        services.AddScoped<IDamagedStocksRepository, DamagedStocksRepository>();
        services.AddScoped<IRecoverableItemReportRepository, RecoverableItemReportRepository>();
        services.AddScoped<IJobRequestAssignmentNotifier, JobRequestAssignmentNotifier>();
        services.AddScoped<IJobRequestRepository, JobRequestRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IJobExecutionRepository, JobExecutionRepository>();
        services.AddScoped<IJobOrderRepository, JobOrderRepository>();
        services.AddScoped<IServiceQuotationRepository, ServiceQuotationRepository>();
        services.AddScoped<IServiceProformaInvoiceRepository, ServiceProformaInvoiceRepository>();
        services.AddScoped<IServiceMemoRepository, ServiceMemoRepository>();
        services.AddScoped<IVerificationRepository, VerificationRepository>();
        services.AddScoped<IItemGrnRepository, ItemGrnRepository>();
        services.AddScoped<IStockAdjustmentRepository, StockAdjustmentRepository>();
        services.AddScoped<IStpDocumentRepository, StpDocumentRepository>();

        // Rebuilt QC module (additive; coexists with the live Material/Product/Packaging QC path).
        services.AddScoped<IQcReauthContext, QcReauthContext>();
        services.AddScoped<IQcSignatureService, QcSignatureService>();
        services.AddScoped<IStpDocxImportService, StpDocxImportService>();
        services.AddScoped<IWorksheetImportCatalogLoader, WorksheetImportCatalogLoader>();
        services.AddScoped<IWorksheetDocxImportService, WorksheetDocxImportService>();

        // Build brief 10/11 — the AI fallback extractor, now routed by active provider rather
        // than hard-wired to Anthropic. Both providers' HttpClients stay named ("AnthropicClient",
        // "OpenAiClient") for build brief 11's AiWorksheetExtractorFactory to fetch by name at
        // dispatch time, with the key/model resolved fresh from the DB on every call — never a
        // redeploy-only settings singleton. AnthropicWorksheetExtractor/OpenAiWorksheetExtractor
        // are intentionally not resolved from the container as IAiWorksheetExtractor themselves;
        // AiWorksheetExtractorRouter is the only IAiWorksheetExtractor registered, matching
        // IWorksheetDocxImportService's unchanged constructor dependency.
        services.AddHttpClient("AnthropicClient", client =>
        {
            client.BaseAddress = new Uri("https://api.anthropic.com/");
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        services.AddHttpClient("OpenAiClient", client =>
        {
            client.BaseAddress = new Uri("https://api.openai.com/");
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        services.AddScoped<IAiWorksheetExtractorFactory, AiWorksheetExtractorFactory>();
        services.AddScoped<IAiWorksheetExtractor, AiWorksheetExtractorRouter>();

        // Build brief 11 — provider/key management, backing AiWorksheetExtractorRouter above.
        services.AddScoped<IAiExtractionSettingsService, AiExtractionSettingsService>();

        services.AddScoped<IStandardTestProcedureRepository, StandardTestProcedureRepository>();
        services.AddScoped<IWorksheetTemplateRepository, WorksheetTemplateRepository>();
        services.AddScoped<ISpecificationRepository, SpecificationRepository>();
        services.AddScoped<ISamplingPointGroupRepository, SamplingPointGroupRepository>();
        services.AddScoped<ISpecificationProposalRepository, SpecificationProposalRepository>();
        services.AddScoped<ITestRequestRepository, TestRequestRepository>();
        services.AddScoped<IWorksheetInstanceRepository, WorksheetInstanceRepository>();
        services.AddScoped<IQcApprovalRepository, QcApprovalRepository>();

        // Milestone 4 — the formal OOS/OOT workflow. Detection is its own service rather than
        // part of the worksheet repository: submitting a result and judging it against a
        // Specification are separate concerns, and the judging half has to be testable alone.
        services.AddScoped<IQcOosDetectionService, QcOosDetectionService>();
        services.AddScoped<IOosCaseRepository, OosCaseRepository>();

        // Milestone 5 — certificates. Generation is a service rather than part of the repository
        // because it is a system trigger with two callers (a worksheet reaching Reviewed, and an
        // OOS case closing), while the repository serves only the two user actions.
        services.AddScoped<IQcCoaGenerationService, QcCoaGenerationService>();
        services.AddScoped<ICoaRepository, CoaRepository>();

        // Milestone 6 — scheduled routine testing and water validity windows. The due-date scan
        // and the water-period scaffolding are services rather than repository methods for the
        // same reason certificate generation is: both are system triggers hanging off something
        // other than a user action (a nightly clock, a certificate issuance), and both have to be
        // testable without that trigger.
        services.AddScoped<ISamplingPointRepository, SamplingPointRepository>();
        services.AddScoped<IMonitoringProgramRepository, MonitoringProgramRepository>();
        services.AddScoped<IQcMonitoringScanService, QcMonitoringScanService>();
        services.AddScoped<IQcWaterQualityPeriodService, QcWaterQualityPeriodService>();
        services.AddScoped<IWaterQualityRepository, APP.Repository.WaterQualityRepository>();
        services.AddScoped<IQcWaterQualityRepository, APP.Repository.QcWorksheets.WaterQualityRepository>();

        services.AddScoped<IStpDocumentAccessService, StpDocumentAccessService>();
        services.AddSingleton(_ => OnlyOfficeSettings.Load());
        services.AddScoped<IOnlyOfficeConfigService, OnlyOfficeConfigService>();
        services.AddScoped<IFormulaMigrationInventoryService, FormulaMigrationInventoryService>();
        services.AddScoped<IFormulaMigrationEvidenceService, FormulaMigrationEvidenceService>();
        services.AddScoped<IFormulaMigrationApplyService, FormulaMigrationApplyService>();
        services.AddScoped<IFormulaDefinitionService, FormulaDefinitionService>();
        services.AddScoped<IFormulaResponseRuntimeService, FormulaResponseRuntimeService>();
        services.AddScoped<IFormulaSubmissionService, FormulaSubmissionService>();
        services.AddScoped<IFormRevisionService, FormRevisionService>();
        services.AddSingleton(_ => FormulaCalculationSettings.Load());
        services.AddTransient<FormulaCalculationClientAuthHandler>();
        services.AddHttpClient<IFormulaCalculationClient, FormulaCalculationClient>((provider, client) =>
            {
                var settings = provider.GetRequiredService<FormulaCalculationSettings>();
                client.BaseAddress = settings.BaseUri;
                client.Timeout = settings.Timeout;
            })
            .AddHttpMessageHandler<FormulaCalculationClientAuthHandler>();
        services.AddHttpClient();

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
        services.AddScoped<IProductionActivityStepEventPublisher, ProductionActivityStepEventPublisher>();
        //services.AddHostedService<ApprovalEscalationService>();
        services.AddHostedService<LeaveExpiryService>();
        services.AddHostedService<ServiceExpiryService>();
        services.AddHostedService<MaterialStockService>();
        services.AddHostedService<MaterialBatchExpiryService>();
        services.AddHostedService<EmployeeSuspensionService>();
        services.AddHostedService<WaterStockMaintenanceService>();

        // Milestone 6 — the daily QC monitoring due-date scan. A plain BackgroundService on a
        // 24-hour delay, matching every other scheduled job above; no scheduler package is
        // introduced for one sweep.
        services.AddHostedService<QcMonitoringScanBackgroundService>();
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
