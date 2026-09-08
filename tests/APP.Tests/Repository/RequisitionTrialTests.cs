using APP.Mapper;
using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Requisitions.Request;
using DOMAIN.Entities.RndFormulations;
using DOMAIN.Entities.RndProjects;
using DOMAIN.Entities.RndTrialBatches;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SHARED;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class NoCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

// Warehouse rows carry a per-department visibility filter (ApplicationDbContext's
// ShouldNotFilterProducts) that only bypasses department-scoping for callers whose
// own department is NonProduction - which is exactly what an R&D staff member (or
// anyone else in a NonProduction department) issuing a trial requisition would be.
file class NonProductionCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => nameof(SHARED.DepartmentType.NonProduction);
}

public class RequisitionTrialTests
{
    private static ApplicationDbContext CreateContext() =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            new NoCurrentUserService()
        );

    private static ApplicationDbContext CreateNonProductionContext() =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            new NonProductionCurrentUserService()
        );

    private static IMapper CreateMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddAutoMapper(cfg => { }, typeof(OryxMapper));
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    private static ApprovalRepository CreateApprovalRepository(ApplicationDbContext context) =>
        new(
            context,
            CreateMapper(),
            null!,
            null!,
            NullLogger<ApprovalRepository>.Instance,
            null!,
            new NoOpProductionActivityStepEventPublisher()
        );

    private static RequisitionRepository CreateRepository(ApplicationDbContext context, IMapper mapper) =>
        new(
            context,
            mapper,
            null!,
            null!,
            null!,
            null!,
            new MaterialRepository(context, mapper),
            CreateApprovalRepository(context),
            null!,
            new NoOpProductionActivityStepEventPublisher()
        );

    private static async Task<(Department department, RndTrialBatch trialBatch)> SeedProjectAndTrialBatch(
        ApplicationDbContext context
    )
    {
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Code = "RND",
            Name = "Research & Development",
            Type = DepartmentType.NonProduction,
        };
        var project = new RndProject
        {
            Id = Guid.NewGuid(),
            Code = "RND-2026-0001",
            Title = "Test project",
            DepartmentId = department.Id,
            RequestedById = Guid.NewGuid(),
            Status = RndProjectStatus.InDevelopment,
        };
        var formulation = new RndFormulation
        {
            Id = Guid.NewGuid(),
            RndProjectId = project.Id,
            Version = 1,
            Status = RndFormulationStatus.Draft,
        };
        var trialBatch = new RndTrialBatch
        {
            Id = Guid.NewGuid(),
            RndProjectId = project.Id,
            RndFormulationId = formulation.Id,
            BatchCode = "TRB-2026-0001",
            ScaleType = RndTrialBatchScaleType.LabScale,
            BatchSize = 1,
            Status = RndTrialBatchStatus.Planned,
        };
        context.AddRange(department, project, formulation, trialBatch);
        await context.SaveChangesAsync();
        return (department, trialBatch);
    }

    [Fact]
    public async Task CreateRequisition_Trial_creates_single_requisition_without_production_schedule()
    {
        await using var context = CreateContext();
        var mapper = CreateMapper();
        var (department, trialBatch) = await SeedProjectAndTrialBatch(context);
        var user = new DOMAIN.Entities.Users.User { Id = Guid.NewGuid(), DepartmentId = department.Id };
        var material = new Material { Id = Guid.NewGuid(), Kind = MaterialKind.Raw };
        context.AddRange(user, material);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context, mapper);

        var result = await repository.CreateRequisition(
            new CreateRequisitionRequest
            {
                RequisitionType = RequisitionType.Trial,
                RndTrialBatchId = trialBatch.Id,
                Items = [new CreateRequisitionItemRequest { MaterialId = material.Id, Quantity = 5 }],
            },
            user.Id
        );

        Assert.True(result.IsSuccess);
        var requisition = await context.Requisitions.Include(r => r.Items).SingleAsync();
        Assert.Equal(RequisitionType.Trial, requisition.RequisitionType);
        Assert.Equal(trialBatch.Id, requisition.RndTrialBatchId);
        Assert.Null(requisition.ProductionScheduleProductId);
        Assert.StartsWith("TRQ/", requisition.Code);
        Assert.Single(requisition.Items);
    }

    [Fact]
    public async Task CreateRequisition_Trial_fails_without_trial_batch_id()
    {
        await using var context = CreateContext();
        var mapper = CreateMapper();
        var department = new Department { Id = Guid.NewGuid(), Name = "Research & Development" };
        var user = new DOMAIN.Entities.Users.User { Id = Guid.NewGuid(), DepartmentId = department.Id };
        context.AddRange(department, user);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context, mapper);

        var result = await repository.CreateRequisition(
            new CreateRequisitionRequest { RequisitionType = RequisitionType.Trial, Items = [] },
            user.Id
        );

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task IssueTrialRequisition_consumes_fefo_across_batches()
    {
        await using var context = CreateNonProductionContext();
        var mapper = CreateMapper();
        var (department, trialBatch) = await SeedProjectAndTrialBatch(context);
        var labWarehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            Name = "R&D Lab Warehouse",
            Type = WarehouseType.RawMaterialStorage,
            DepartmentId = department.Id,
        };
        var material = new Material { Id = Guid.NewGuid(), Kind = MaterialKind.Raw };
        var earlyBatch = new MaterialBatch
        {
            Id = Guid.NewGuid(),
            MaterialId = material.Id,
            TotalQuantity = 3,
            ExpiryDate = DateTime.UtcNow.AddMonths(3),
        };
        var laterBatch = new MaterialBatch
        {
            Id = Guid.NewGuid(),
            MaterialId = material.Id,
            TotalQuantity = 10,
            ExpiryDate = DateTime.UtcNow.AddMonths(12),
        };
        var requisition = new Requisition
        {
            Id = Guid.NewGuid(),
            Code = "TRQ/26/0001",
            RequisitionType = RequisitionType.Trial,
            RndTrialBatchId = trialBatch.Id,
            RequestedById = Guid.NewGuid(),
            DepartmentId = department.Id,
            Approved = true,
            Status = RequestStatus.New,
            Items =
            [
                new RequisitionItem
                {
                    Id = Guid.NewGuid(),
                    MaterialId = material.Id,
                    Quantity = 5,
                    Status = RequestStatus.New,
                },
            ],
        };
        context.AddRange(labWarehouse, material, requisition);
        context.MaterialBatches.AddRange(earlyBatch, laterBatch);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context, mapper);
        var result = await repository.IssueTrialRequisition(requisition.Id, Guid.NewGuid());

        Assert.True(result.IsSuccess);
        // Re-querying MaterialBatch through a second LINQ call proved unreliable under
        // the InMemory provider in this environment (a plain, predicate-free query
        // returned inconsistent counts across otherwise-identical runs). ConsumeMaterialAtLocation
        // mutates the same tracked instances this test already holds a reference to, so
        // asserting on those directly is both more robust and more precise here.
        Assert.Equal(3, earlyBatch.ConsumedQuantity);
        Assert.Equal(2, laterBatch.ConsumedQuantity);

        Assert.Equal(RequestStatus.Completed, requisition.Status);
        Assert.All(requisition.Items, item => Assert.Equal(RequestStatus.Completed, item.Status));
    }

    [Fact]
    public async Task IssueTrialRequisition_fails_cleanly_on_insufficient_stock()
    {
        await using var context = CreateNonProductionContext();
        var mapper = CreateMapper();
        var (department, trialBatch) = await SeedProjectAndTrialBatch(context);
        var labWarehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            Name = "R&D Lab Warehouse",
            Type = WarehouseType.RawMaterialStorage,
            DepartmentId = department.Id,
        };
        var material = new Material { Id = Guid.NewGuid(), Kind = MaterialKind.Raw };
        var batch = new MaterialBatch { Id = Guid.NewGuid(), MaterialId = material.Id, TotalQuantity = 1 };
        var requisition = new Requisition
        {
            Id = Guid.NewGuid(),
            Code = "TRQ/26/0002",
            RequisitionType = RequisitionType.Trial,
            RndTrialBatchId = trialBatch.Id,
            RequestedById = Guid.NewGuid(),
            DepartmentId = department.Id,
            Approved = true,
            Status = RequestStatus.New,
            Items =
            [
                new RequisitionItem
                {
                    Id = Guid.NewGuid(),
                    MaterialId = material.Id,
                    Quantity = 5,
                    Status = RequestStatus.New,
                },
            ],
        };
        context.AddRange(labWarehouse, material, requisition);
        context.MaterialBatches.Add(batch);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context, mapper);
        var result = await repository.IssueTrialRequisition(requisition.Id, Guid.NewGuid());

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task ApprovalRepository_AutoApprovesTrialRequisition_WhenWorkflowIsMissing()
    {
        await using var context = CreateContext();
        var requisition = new Requisition
        {
            Id = Guid.NewGuid(),
            Code = "TRQ/26/0003",
            RequisitionType = RequisitionType.Trial,
            RequestedById = Guid.NewGuid(),
            DepartmentId = Guid.NewGuid(),
            Status = RequestStatus.New,
            Items = [],
        };
        context.Requisitions.Add(requisition);
        await context.SaveChangesAsync();

        await CreateApprovalRepository(context).CreateInitialApprovalsAsync(
            "TrialRequisition",
            requisition.Id
        );

        Assert.True(requisition.Approved);
        Assert.Empty(context.RequisitionApprovals);
    }

    [Fact]
    public async Task ApprovalRepository_ApproveItem_ResolvesTrialRequisitionType_NotStock()
    {
        await using var context = CreateContext();
        var approverId = Guid.NewGuid();
        var requisition = new Requisition
        {
            Id = Guid.NewGuid(),
            Code = "TRQ/26/0004",
            RequisitionType = RequisitionType.Trial,
            RequestedById = Guid.NewGuid(),
            DepartmentId = Guid.NewGuid(),
            Status = RequestStatus.New,
            Items = [],
        };
        var approval = new Approval { Id = Guid.NewGuid(), ItemType = "TrialRequisition", ApprovalStages = [] };
        approval.ApprovalStages.Add(new ApprovalStage
        {
            Id = Guid.NewGuid(),
            ApprovalId = approval.Id,
            Approval = approval,
            Order = 1,
            Required = true,
            UserId = approverId,
        });
        context.AddRange(requisition, approval);
        await context.SaveChangesAsync();

        var approvalRepository = CreateApprovalRepository(context);
        await approvalRepository.CreateInitialApprovalsAsync("TrialRequisition", requisition.Id);

        var result = await approvalRepository.ApproveItem(
            "TrialRequisition",
            requisition.Id,
            approverId,
            [Guid.NewGuid()],
            "ok"
        );

        Assert.True(result.IsSuccess);
        Assert.True(requisition.Approved);
    }

    [Fact]
    public async Task ApprovalRepository_GetEntitiesRequiringApproval_IncludesPendingTrialRequisition()
    {
        await using var context = CreateContext();
        var approverId = Guid.NewGuid();
        // Requisition.DepartmentId is a required FK, and Department carries a global
        // soft-delete query filter - GetEntitiesRequiringApproval's Include(Department)
        // silently excludes the whole requisition row if DepartmentId doesn't resolve
        // to a real row (the same "required end of a filtered relationship" gotcha
        // documented for User in the RndProject tests).
        var department = new Department { Id = Guid.NewGuid(), Name = "Research & Development" };
        var requisition = new Requisition
        {
            Id = Guid.NewGuid(),
            Code = "TRQ/26/0005",
            RequisitionType = RequisitionType.Trial,
            RequestedById = Guid.NewGuid(),
            DepartmentId = department.Id,
            Status = RequestStatus.New,
            Items = [],
        };
        context.Departments.Add(department);
        var approval = new Approval { Id = Guid.NewGuid(), ItemType = "TrialRequisition", ApprovalStages = [] };
        approval.ApprovalStages.Add(new ApprovalStage
        {
            Id = Guid.NewGuid(),
            ApprovalId = approval.Id,
            Approval = approval,
            Order = 1,
            Required = true,
            UserId = approverId,
        });
        context.AddRange(requisition, approval);
        await context.SaveChangesAsync();

        var approvalRepository = CreateApprovalRepository(context);
        await approvalRepository.CreateInitialApprovalsAsync("TrialRequisition", requisition.Id);

        var pending = await approvalRepository.GetEntitiesRequiringApproval(approverId, [Guid.NewGuid()], null);

        var entity = Assert.Single(pending);
        Assert.Equal("TrialRequisition", entity.ModelType);
        Assert.Equal(requisition.Id, entity.Id);
        Assert.Equal(requisition.Code, entity.Code);
    }
}
