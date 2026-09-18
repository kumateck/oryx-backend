using APP.IRepository;
using APP.Mapper;
using APP.Repository;
using APP.Repository.QcWorksheets;
using APP.Services.QcWorksheets;
using AutoMapper;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Products.Equipments;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.QcWorksheets;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SHARED.Services.Identity;

namespace APP.Tests.QcWorksheets;

internal sealed class NoCurrentUser : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

/// <summary>
/// There is no request in a unit test. The mapper's avatar/signature resolvers handle a null
/// HttpContext, so this satisfies their constructor dependency without faking a request.
/// </summary>
internal sealed class NullHttpContextAccessor : IHttpContextAccessor
{
    public HttpContext HttpContext { get; set; }
}

/// <summary>
/// Resolves only what the QC approval path asks of it, so the real
/// <see cref="ApprovalRepository"/> can be exercised without standing up the whole app.
/// </summary>
internal sealed class StubServiceProvider(IQcReauthContext reauthContext) : IServiceProvider
{
    public object GetService(Type serviceType) =>
        serviceType == typeof(IQcReauthContext) ? reauthContext : null;
}

/// <summary>
/// Shared harness for the QC worksheet acceptance tests. Everything runs against the real
/// repositories, the real <see cref="ApprovalRepository"/> and the real
/// <see cref="QcSignatureService"/> — including genuine password hashing — so the tests
/// prove the production path rather than a parallel one.
/// </summary>
internal sealed class QcWorksheetTestContext : IDisposable
{
    internal const string CorrectPassword = "Correct-Horse-9!";

    internal ApplicationDbContext Db { get; }
    internal IMapper Mapper { get; }
    internal QcReauthContext Reauth { get; }
    internal UserManager<User> UserManager { get; }
    internal ApprovalRepository ApprovalRepository { get; }
    internal QcSignatureService SignatureService { get; }
    internal StandardTestProcedureRepository Stps { get; }
    internal WorksheetTemplateRepository Templates { get; }
    internal SpecificationRepository Specifications { get; }
    internal SamplingPointGroupRepository SamplingPointGroups { get; }
    internal TestRequestRepository TestRequests { get; }
    internal WorksheetInstanceRepository WorksheetInstances { get; }
    internal QcApprovalRepository Approvals { get; }
    internal QcOosDetectionService OosDetection { get; }
    internal OosCaseRepository OosCases { get; }

    internal User Approver { get; private set; }
    internal Role ApproverRole { get; private set; }

    internal QcWorksheetTestContext()
    {
        Db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            new NoCurrentUser());

        Mapper = CreateMapper();
        Reauth = new QcReauthContext();
        UserManager = CreateUserManager(Db);

        ApprovalRepository = new ApprovalRepository(
            Db,
            Mapper,
            UserManager,
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<ApprovalRepository>.Instance,
            new StubServiceProvider(Reauth),
            null!);

        SignatureService = new QcSignatureService(Db, UserManager, Reauth, ApprovalRepository);

        Stps = new StandardTestProcedureRepository(
            Db, Mapper, SignatureService, Reauth, ApprovalRepository, new StpDocxImportService());

        Templates = new WorksheetTemplateRepository(
            Db, Mapper, SignatureService, Reauth, ApprovalRepository);

        Specifications = new SpecificationRepository(
            Db, Mapper, SignatureService, Reauth, ApprovalRepository);

        SamplingPointGroups = new SamplingPointGroupRepository(Db, Mapper);

        TestRequests = new TestRequestRepository(Db, Mapper);

        OosDetection = new QcOosDetectionService(
            Db, ApprovalRepository, NullLogger<QcOosDetectionService>.Instance);

        WorksheetInstances = new WorksheetInstanceRepository(
            Db, Mapper, SignatureService, ApprovalRepository, OosDetection);

        OosCases = new OosCaseRepository(Db, Mapper, SignatureService, ApprovalRepository);

        Approvals = new QcApprovalRepository(Db, Mapper);
    }

    /// <summary>Creates the approver and a single-stage approval chain for the given model type.</summary>
    internal async Task<Guid> SeedApprovalChain(string modelType)
    {
        Approver ??= await SeedUser();
        ApproverRole ??= await SeedRole();

        var approval = new Approval
        {
            Id = Guid.NewGuid(),
            ItemType = modelType,
            CreatedAt = DateTime.UtcNow,
            ApprovalStages = []
        };

        Db.Approvals.Add(approval);
        Db.ApprovalStages.Add(new ApprovalStage
        {
            Id = Guid.NewGuid(),
            ApprovalId = approval.Id,
            Order = 1,
            Required = true,
            UserId = Approver.Id
        });

        await Db.SaveChangesAsync();
        return approval.Id;
    }

    /// <summary>
    /// Inserts an Effective worksheet template carrying the given field keys, so a
    /// Specification test can link to something real without running the whole template
    /// approval cycle it is not trying to prove.
    /// </summary>
    internal async Task<WorksheetTemplate> SeedEffectiveTemplate(
        string code,
        WorksheetCategory category,
        params string[] fieldKeys)
    {
        var sectionId = Guid.NewGuid();

        var template = new WorksheetTemplate
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = $"{code} worksheet",
            Category = category,
            Version = 1,
            Status = QcDocumentStatus.Effective,
            EffectiveDate = DateTime.UtcNow,
            Approved = true,
            CreatedAt = DateTime.UtcNow,
            Sections =
            [
                new WorksheetSection
                {
                    Id = sectionId,
                    Order = 1,
                    Name = "Results",
                    CreatedAt = DateTime.UtcNow,
                    Fields = fieldKeys.Select((key, index) => new WorksheetField
                    {
                        Id = Guid.NewGuid(),
                        WorksheetSectionId = sectionId,
                        Order = index + 1,
                        FieldKey = key,
                        Label = key,
                        Type = WorksheetFieldType.Result,
                        Mode = WorksheetFieldMode.Entry,
                        CreatedAt = DateTime.UtcNow
                    }).ToList()
                }
            ]
        };

        Db.QcWorksheetTemplates.Add(template);
        await Db.SaveChangesAsync();
        return template;
    }

    /// <summary>
    /// Adds an acceptance-criteria row binding a Specification to the worksheet field it
    /// judges. Milestone 4's detection resolves limits through exactly this pair.
    /// </summary>
    internal async Task<SpecificationCharacteristic> SeedCharacteristic(
        Specification specification,
        WorksheetTemplate template,
        string fieldKey,
        string acceptanceCriteria = null,
        string alertLimit = null,
        string actionLimit = null,
        Guid? samplingPointGroupId = null,
        string testName = null)
    {
        var characteristic = new SpecificationCharacteristic
        {
            Id = Guid.NewGuid(),
            SpecificationId = specification.Id,
            TestName = testName ?? fieldKey,

            // AcceptanceCriteria is required on this entity — every Characteristic states its
            // criteria, and a tiered one states Alert/Action limits on top. When a test cares
            // only about the tiered limits, the criteria text mirrors the Action limit, which
            // is what a real tiered Characteristic looks like. The evaluator prefers an
            // explicit ActionLimit regardless, so this never changes what is under test.
            AcceptanceCriteria = acceptanceCriteria ?? actionLimit ?? alertLimit ?? "Complies",
            AlertLimit = alertLimit,
            ActionLimit = actionLimit,
            SamplingPointGroupId = samplingPointGroupId,
            SourceWorksheetTemplateId = template.Id,
            SourceFieldKey = fieldKey,
            IncludeOnCoa = true,
            CreatedAt = DateTime.UtcNow
        };

        Db.QcSpecificationCharacteristics.Add(characteristic);
        await Db.SaveChangesAsync();
        return characteristic;
    }

    /// <summary>
    /// Sets the retest policy, which the seeded Specification defaults to SameSample. The
    /// FreshResample path is a different branch of the retest flow and has to be selectable.
    /// </summary>
    internal async Task SetRetestPolicy(Specification specification, QcRetestPolicy policy)
    {
        var tracked = await Db.QcSpecifications.SingleAsync(item => item.Id == specification.Id);
        tracked.RetestPolicy = policy;
        specification.RetestPolicy = policy;
        await Db.SaveChangesAsync();
    }

    /// <summary>
    /// A row in the live material batch table — the real, shared, system-of-record entity the
    /// OOS disposition quarantines and releases. Deliberately the production table, because a
    /// QC-only shadow status would prove nothing about the behaviour under test.
    /// </summary>
    internal async Task<MaterialBatch> SeedMaterialBatch(
        string batchNumber, BatchStatus status = BatchStatus.Testing)
    {
        // A real parent Material is required, not an arbitrary FK: MaterialBatch's global query
        // filter reads through the navigation (`!entity.Material.DeletedAt.HasValue`), so a
        // batch whose Material does not exist is invisible to every query in the application.
        var material = new Material
        {
            Id = Guid.NewGuid(),
            Code = $"MAT-{Guid.NewGuid().ToString()[..6]}",
            Name = "Seeded material",
            CreatedAt = DateTime.UtcNow
        };

        Db.Materials.Add(material);

        // A real UoM as well. MaterialBatch.UoM is auto-included and the relationship is
        // required, so a batch pointing at a UoM that does not exist is silently dropped from
        // any query that materializes the entity — which would make this seed a phantom.
        var uom = new UnitOfMeasure
        {
            Id = Guid.NewGuid(),
            Name = "Kilogram",
            Symbol = "kg",
            CreatedAt = DateTime.UtcNow
        };

        Db.UnitOfMeasures.Add(uom);

        var batch = new MaterialBatch
        {
            Id = Guid.NewGuid(),
            MaterialId = material.Id,
            UoMId = uom.Id,
            BatchNumber = batchNumber,
            Status = status,
            DateReceived = DateTime.UtcNow.AddDays(-7),
            CreatedAt = DateTime.UtcNow
        };

        Db.MaterialBatches.Add(batch);
        await Db.SaveChangesAsync();
        return batch;
    }

    /// <summary>The Product counterpart. Note BatchManufacturingStatus has no Quarantine or Available value.</summary>
    internal async Task<BatchManufacturingRecord> SeedBatchManufacturingRecord(
        string batchNumber, BatchManufacturingStatus status = BatchManufacturingStatus.Testing)
    {
        var record = new BatchManufacturingRecord
        {
            Id = Guid.NewGuid(),
            ProductionScheduleProductId = Guid.NewGuid(),
            BatchNumber = batchNumber,
            Status = status,
            CreatedAt = DateTime.UtcNow
        };

        Db.BatchManufacturingRecords.Add(record);
        await Db.SaveChangesAsync();
        return record;
    }

    internal async Task<SamplingPointGroup> SeedSamplingPointGroup(string name)
    {
        var group = new SamplingPointGroup
        {
            Id = Guid.NewGuid(),
            Name = name,
            CreatedAt = DateTime.UtcNow
        };

        Db.QcSamplingPointGroups.Add(group);
        await Db.SaveChangesAsync();
        return group;
    }

    /// <summary>
    /// Inserts a worksheet template carrying fields of the given types, for the execution
    /// tests, which need Instrument/Reagent/ReferencedResult fields rather than plain results.
    /// </summary>
    internal async Task<WorksheetTemplate> SeedTemplateWithFields(
        string code,
        WorksheetCategory category,
        params WorksheetField[] fields)
    {
        var sectionId = Guid.NewGuid();

        foreach (var field in fields)
        {
            field.Id = field.Id == Guid.Empty ? Guid.NewGuid() : field.Id;
            field.WorksheetSectionId = sectionId;
            field.CreatedAt = DateTime.UtcNow;
            field.Label ??= field.FieldKey;
        }

        var template = new WorksheetTemplate
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = $"{code} worksheet",
            Category = category,
            Version = 1,
            Status = QcDocumentStatus.Effective,
            EffectiveDate = DateTime.UtcNow,
            Approved = true,
            CreatedAt = DateTime.UtcNow,
            Sections =
            [
                new WorksheetSection
                {
                    Id = sectionId,
                    Order = 1,
                    Name = "Results",
                    CreatedAt = DateTime.UtcNow,
                    Fields = fields.ToList()
                }
            ]
        };

        Db.QcWorksheetTemplates.Add(template);
        await Db.SaveChangesAsync();
        return template;
    }

    /// <summary>
    /// Inserts an Effective specification linked to the given templates, each link pinned to
    /// the template row's own version — the same pin the Specification endpoints write.
    /// </summary>
    internal async Task<Specification> SeedEffectiveSpecification(
        SpecificationAppliesTo appliesTo,
        params (WorksheetTemplate Template, SpecificationAnalysisType AnalysisType)[] links)
    {
        var specification = new Specification
        {
            Id = Guid.NewGuid(),
            Code = $"QCD/SPEC/{Guid.NewGuid().ToString()[..8]}",
            Name = "Seeded specification",
            AppliesTo = appliesTo,
            Stage = appliesTo == SpecificationAppliesTo.Product ? SpecificationStage.Finished : null,
            RetestPolicy = QcRetestPolicy.SameSample,
            Version = 1,
            Status = QcDocumentStatus.Effective,
            EffectiveDate = DateTime.UtcNow,
            Approved = true,
            CreatedAt = DateTime.UtcNow,
            WorksheetLinks = links
                .Select(link => new SpecificationWorksheetLink
                {
                    Id = Guid.NewGuid(),
                    WorksheetTemplateId = link.Template.Id,
                    WorksheetTemplateVersion = link.Template.Version,
                    AnalysisType = link.AnalysisType,
                    CreatedAt = DateTime.UtcNow
                })
                .ToList()
        };

        Db.QcSpecifications.Add(specification);
        await Db.SaveChangesAsync();
        return specification;
    }

    /// <summary>
    /// A row in the existing QC equipment register — read-only master data this module gates
    /// against and never writes to.
    /// </summary>
    internal async Task<QcEquipment> SeedEquipment(string name, DateTime? calibrationDueDate)
    {
        var category = new QcEquipmentCategory
        {
            Id = Guid.NewGuid(),
            Name = "Balances",
            CreatedAt = DateTime.UtcNow
        };

        var equipment = new QcEquipment
        {
            Id = Guid.NewGuid(),
            EquipmentId = name,
            Name = name,
            QcEquipmentCategoryId = category.Id,
            CalibrationDueDate = calibrationDueDate,
            CreatedAt = DateTime.UtcNow
        };

        Db.QcEquipmentCategories.Add(category);
        Db.QcEquipments.Add(equipment);
        await Db.SaveChangesAsync();
        return equipment;
    }

    /// <summary>
    /// A row in the existing reagent catalog. It carries no batch or expiry of its own, which
    /// is exactly why a worksheet captures both per use.
    /// </summary>
    internal async Task<Reagent> SeedReagent(string name)
    {
        var reagent = new Reagent { Id = Guid.NewGuid(), Name = name, CreatedAt = DateTime.UtcNow };
        Db.Reagents.Add(reagent);
        await Db.SaveChangesAsync();
        return reagent;
    }

    internal async Task<User> SeedUser(string userName = "qc.approver")
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            Email = $"{userName}@example.test",
            FirstName = "Qc",
            LastName = "Approver",
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, CorrectPassword);

        Db.Users.Add(user);
        await Db.SaveChangesAsync();
        return user;
    }

    private async Task<Role> SeedRole()
    {
        var role = new Role { Id = Guid.NewGuid(), Name = "QC Manager", NormalizedName = "QC MANAGER" };
        Db.Roles.Add(role);
        await Db.SaveChangesAsync();
        return role;
    }

    private static IMapper CreateMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // UserDto's Avatar/Signature resolvers take an IHttpContextAccessor to build absolute
        // URLs. They are null-safe (`request.HttpContext?.Request.Host`), so a null accessor
        // is enough here — without one registered, mapping any loaded User navigation throws.
        services.AddSingleton<IHttpContextAccessor, NullHttpContextAccessor>();

        services.AddAutoMapper(cfg => { }, typeof(OryxMapper));
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    private static UserManager<User> CreateUserManager(ApplicationDbContext context) =>
        new(
            new UserStore<User, Role, ApplicationDbContext, Guid>(context),
            Options.Create(new IdentityOptions()),
            new PasswordHasher<User>(),
            [],
            [],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null,
            NullLogger<UserManager<User>>.Instance);

    public void Dispose()
    {
        UserManager.Dispose();
        Db.Dispose();
    }
}
