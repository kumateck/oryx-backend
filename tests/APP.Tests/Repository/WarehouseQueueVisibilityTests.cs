using System.Reflection;
using API.Controllers;
using APP.Repository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SHARED;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public sealed class WarehouseQueueVisibilityTests
{
    private sealed class Actor : ICurrentUserService
    {
        public Guid? UserId { get; } = Guid.NewGuid();
        public Guid? DepartmentId { get; } = Guid.NewGuid();
        public string DepartmentType => "Production";
    }
    [Theory]
    [InlineData(nameof(StockIssueQueueController.Raw), PermissionKeys.CanViewRawMaterialRequisitions)]
    [InlineData(nameof(StockIssueQueueController.Packaging), PermissionKeys.CanViewPackagingMaterialRequisitions)]
    public void Queue_endpoints_have_one_specific_registered_view_permission(string method, string key)
    {
        Assert.Equal(key, Assert.Single(typeof(StockIssueQueueController).GetMethod(method)!.GetCustomAttributes<AuthorizeAttribute>()).Policy);
        Assert.Single(PermissionUtils.GeneratePermissions(), permission => permission.Key == key);
        Assert.Empty(typeof(StockIssueQueueController).GetMethod(method)!.GetCustomAttributes<AllowAnonymousAttribute>());
    }
    [Fact]
    public async Task Issue_queue_filters_before_pagination_orders_oldest_first_and_denies_the_wrong_kind()
    {
        var actor = new Actor();
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, actor);
        var role = Guid.NewGuid(); var otherDepartment = Guid.NewGuid();
        db.Departments.Add(new() { Id = actor.DepartmentId!.Value, Name = "Production" });
        db.Users.Add(new() { Id = actor.UserId!.Value, DepartmentId = actor.DepartmentId });
        db.Roles.Add(new() { Id = role, Name = "Store" }); db.UserRoles.Add(new() { RoleId = role, UserId = actor.UserId.Value });
        db.RoleClaims.Add(new() { Id = 1, RoleId = role, ClaimType = AppConstants.Permission, ClaimValue = PermissionKeys.CanViewRawMaterialRequisitions });
        db.PermissionTypes.Add(new() { Id = Guid.NewGuid(), RoleClaimId = 1, Key = PermissionKeys.CanViewRawMaterialRequisitions, Type = "Access" });
        var first = new Product { Id = Guid.NewGuid(), Name = "X'feron", Code = "NL-021", DepartmentId = actor.DepartmentId };
        var second = new Product { Id = Guid.NewGuid(), Name = "Metronidazole", Code = "NL-014", DepartmentId = actor.DepartmentId };
        db.Products.AddRange(first, second);
        foreach (var (code, product, department, age) in new[] { ("RM-NEW", first, actor.DepartmentId.Value, 1), ("RM-OLD", first, actor.DepartmentId.Value, 5),
            ("RM-OTHER", second, actor.DepartmentId.Value, 3), ("RM-PRIVATE", first, otherDepartment, 10), ("PM-OTHER", first, actor.DepartmentId.Value, 9) })
        {
            var schedule = new ProductionSchedule { Id = Guid.NewGuid(), DepartmentId = department, Code = "PS" };
            var batch = new ProductionScheduleProduct { Id = Guid.NewGuid(), ProductId = product.Id, ProductionScheduleId = schedule.Id };
            db.ProductionSchedules.Add(schedule); db.ProductionScheduleProducts.Add(batch);
            db.Requisitions.Add(new() { Id = Guid.NewGuid(), Code = code, DepartmentId = department, ProductionScheduleProductId = batch.Id,
                RequestedById = actor.UserId.Value, RequisitionType = RequisitionType.Stock, CreatedAt = DateTime.UtcNow.AddDays(-age), Status = RequestStatus.Pending });
        }
        await db.SaveChangesAsync();
        // Added rows receive server creation timestamps; establish the historical queue afterward.
        foreach (var request in db.Requisitions.Local)
            request.CreatedAt = DateTime.UtcNow.AddDays(request.Code switch { "RM-NEW" => -1, "RM-OLD" => -5, "RM-OTHER" => -3, _ => -10 });
        await db.SaveChangesAsync();
        var mapper = new MapperConfiguration(config => {
            config.CreateMap<User, CollectionItemDto>();
            config.CreateMap<Department, CollectionItemDto>();
            config.CreateMap<ProductionScheduleProduct, ProductionScheduleProductDto>();
            config.CreateMap<Product, ProductListDto>();
            config.CreateMap<ProductionSchedule, CollectionItemDto>();
            config.CreateMap<Requisition, RequisitionDto>();
        }).CreateMapper();
        var repository = new RequisitionRepository(db, mapper, null!, null!, null!, null!, null!, null!, null!, null!, identity: actor);
        var controller = new StockIssueQueueController(db, repository) { ControllerContext = new() { HttpContext = new DefaultHttpContext() } };
        controller.HttpContext.Items["Sub"] = actor.UserId.Value.ToString();
        var result = Assert.IsType<Ok<StockIssueQueueDto>>(await controller.Raw(pageSize: 1, productId: first.Id)).Value!;
        Assert.Equal("RM-OLD", Assert.Single(result.Requisitions.Data).Code);
        Assert.Equal(2, result.Requisitions.TotalRecordCount);
        Assert.Equal(2, result.Products.Count); // Available products are scoped to the full permitted queue, not the selected page.
        var next = Assert.IsType<Ok<StockIssueQueueDto>>(await controller.Raw(page: 2, pageSize: 1, productId: first.Id)).Value!;
        Assert.Equal("RM-NEW", Assert.Single(next.Requisitions.Data).Code);
        Assert.IsType<ForbidHttpResult>(await controller.Packaging());
    }

    [Fact]
    public async Task Approval_rows_identify_product_stage_and_material_without_fetching_each_detail()
    {
        var actor = new Actor();
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, actor);
        var product = new Product { Id = Guid.NewGuid(), Name = "Paracetamol syrup", Code = "NL-001" };
        var batch = new ProductionScheduleProduct { Id = Guid.NewGuid(), Product = product, ProductionSchedule = new() { Id = Guid.NewGuid() } };
        var bmr = new BatchManufacturingRecord { Id = Guid.NewGuid(), ProductionScheduleProduct = batch, BatchNumber = "L001K074" };
        var step = Guid.NewGuid();
        var material = new Material { Id = Guid.NewGuid(), Name = "Glibenclamide", Code = "RMG001" };
        var lot = new MaterialBatch { Id = Guid.NewGuid(), Material = material, BatchNumber = "GLB-M/028/0620" };
        var form = new Form { Id = Guid.NewGuid(), Name = "Certificate" };
        var productResponse = new Response { Form = form, Id = Guid.NewGuid(), BatchManufacturingRecord = bmr, ProductionActivityStepId = step };
        var materialResponse = new Response { Form = form, Id = Guid.NewGuid(), MaterialBatch = lot };
        db.AddRange(productResponse, materialResponse);
        db.AnalyticalTestRequests.Add(new() { Id = Guid.NewGuid(), BatchManufacturingRecordId = bmr.Id, ProductionActivityStepId = step, Stage = TestStage.Bulk });
        await db.SaveChangesAsync();
        var rows = new[] { new ApprovalEntity { Id = productResponse.Id, ModelType = nameof(Response) }, new ApprovalEntity { Id = materialResponse.Id, ModelType = nameof(Response) } };
        Assert.Equal(bmr.Id, (await db.Responses.IgnoreAutoIncludes().SingleAsync(item => item.Id == productResponse.Id)).BatchManufacturingRecordId);
        await ResponseApprovalIdentity.Apply(db, rows);
        Assert.Equal("NL-001", rows[0].ProductCode); Assert.Equal("Paracetamol syrup", rows[0].ProductName);
        Assert.Equal("L001K074", rows[0].BatchNumber); Assert.Equal(1, rows[0].ProductStage);
        Assert.Equal("RMG001", rows[1].MaterialCode); Assert.Equal("Glibenclamide", rows[1].MaterialName); Assert.Equal("GLB-M/028/0620", rows[1].BatchNumber);
    }
    [Fact]
    public async Task Coa_status_uses_current_rework_and_waits_for_missing_required_microbial_track()
    {
        var actor = new Actor();
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, actor);
        var form = new Form { Id = Guid.NewGuid(), Name = "Chemical test" };
        var ard = new DOMAIN.Entities.ProductAnalyticalRawData.ProductAnalyticalRawData { Id = Guid.NewGuid(), Form = form };
        db.ProductAnalyticalRawData.Add(ard);
        var bmr = Guid.NewGuid(); var step = Guid.NewGuid();
        db.Responses.AddRange(new Response { Id = Guid.NewGuid(), Form = form, BatchManufacturingRecordId = bmr, ProductionActivityStepId = step, Approved = true, ReworkSequence = 0 },
            new Response { Id = Guid.NewGuid(), Form = form, BatchManufacturingRecordId = bmr, ProductionActivityStepId = step, ReworkSequence = 1 },
            new Response { Id = Guid.NewGuid(), Form = form, BatchManufacturingRecordId = bmr, ProductionActivityStepId = Guid.NewGuid(), Approved = true });
        await db.SaveChangesAsync();
        var test = new AnalyticalTestRequest { Id = Guid.NewGuid(), ChemicalArdId = ard.Id, BatchManufacturingRecordId = bmr, ProductionActivityStepId = step };
        var dto = new AnalyticalTestRequestDto { Id = test.Id };
        await AnalyticalTestRequestCoaStatus.Apply(db, [test], [dto]);
        Assert.Equal("Not generated", dto.CoaStatus); // The old approved response and other stage cannot mask a new draft.
        (await db.Responses.IgnoreAutoIncludes().SingleAsync(response => response.ReworkSequence == 1)).CheckedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await AnalyticalTestRequestCoaStatus.Apply(db, [test], [dto]);
        Assert.Equal("Pending approval", dto.CoaStatus);
        test.MicrobialRequired = true;
        await AnalyticalTestRequestCoaStatus.Apply(db, [test], [dto]);
        Assert.Equal("Partially generated", dto.CoaStatus);
    }

    [Fact]
    public void Coa_state_is_separate_from_test_taken_and_requires_all_tracks()
    {
        Assert.Equal("Not generated", AnalyticalTestRequestCoaStatus.Status([]));
        Assert.Equal("Not generated", AnalyticalTestRequestCoaStatus.Status([new()]));
        Assert.Equal("Partially generated", AnalyticalTestRequestCoaStatus.Status([new() { CheckedAt = DateTime.UtcNow }, new()]));
        Assert.Equal("Pending approval", AnalyticalTestRequestCoaStatus.Status([new() { CheckedAt = DateTime.UtcNow }]));
        Assert.Equal("Approved", AnalyticalTestRequestCoaStatus.Status([new() { Approved = true }]));
        Assert.Equal("Revision required", AnalyticalTestRequestCoaStatus.Status([new() { Rejected = true }]));
    }
}
