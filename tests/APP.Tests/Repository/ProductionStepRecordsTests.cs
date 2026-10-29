using APP.Mapper;
using APP.Repository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.ProductionSchedules.Packing;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Routes;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SHARED;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public class ProductionStepRecordsTests
{
    [Fact]
    public async Task Completed_step_reads_actual_test_stage_people_and_all_exact_action_records_without_writes()
    {
        await using var f = await Fixture.Create();
        f.Step.Status = ProductionStatus.Completed;
        f.Step.CompletedAt = DateTime.UtcNow;
        var bmr = f.Bmr();
        var test = new AnalyticalTestRequest
        {
            Id = Guid.NewGuid(), ProductionActivityStepId = f.Step.Id, ProductionScheduleProductId = f.Batch.Id,
            BatchManufacturingRecord = bmr, BatchManufacturingRecordId = bmr.Id, Stage = TestStage.Finished,
            State = new ProductState { Id = Guid.NewGuid(), Name = "Liquid" }, ArNumber = "AR-4862",
            Status = AnalyticalTestStatus.Released, CreatedBy = f.Actor, SampledBy = f.Actor,
            SampledAt = DateTime.UtcNow.AddDays(-2), ReleasedBy = f.Actor, ReleasedAt = DateTime.UtcNow,
            TestedBy = f.Actor, TestedAt = DateTime.UtcNow.AddDays(-1),
            Assignees = [new() { Id = Guid.NewGuid(), User = f.Actor, UserId = f.Actor.Id }]
        };
        f.Context.AddRange(test,
            new FinalPacking { Id = Guid.NewGuid(), ProductionScheduleProductId = f.Batch.Id,
                ProductionActivityStepId = f.Step.Id, TotalQuantityPacked = 450, CreatedBy = f.Actor },
            new FinishedGoodsTransferNote { Id = Guid.NewGuid(), ProductionActivityStepId = f.Step.Id,
                BatchManufacturingRecordId = bmr.Id, BatchManufacturingRecord = bmr,
                TransferNoteNumber = "FGT-4862", TotalQuantity = 450, QuantityReceived = 400, CreatedBy = f.Actor });
        await f.Save();
        var result = await f.Read();
        Assert.True(result.IsSuccess, string.Join(";", result.Errors.Select(error => error.Description)));
        var analysis = Assert.Single(result.Value.AnalyticalTests);
        Assert.Equal(TestStage.Finished, analysis.Request.Stage);
        Assert.Equal("Eunice", analysis.Request.ReleasedBy.FirstName);
        Assert.Single(analysis.Request.Assignees);
        Assert.Equal(f.Actor.Id, analysis.TestedBy.Id);
        Assert.Equal("AR-4862", analysis.Request.ArNumber);
        Assert.Equal(450, Assert.Single(result.Value.FinalPackings).TotalQuantityPacked);
        Assert.Equal(400, Assert.Single(result.Value.Transfers).QuantityReceived);
        Assert.Single(result.Value.BatchRecords);
        Assert.Empty(result.Value.BatchScopedRecordIds);
        Assert.Empty(f.Context.ChangeTracker.Entries());
        Assert.Equal(ProductionStatus.Completed, (await f.Context.ProductionActivitySteps.SingleAsync()).Status);
    }

    [Fact]
    public async Task Other_steps_and_batches_are_never_attributed_to_the_selected_step()
    {
        await using var f = await Fixture.Create();
        var other = new ProductionActivityStep { Id = Guid.NewGuid(), ProductionActivityId = f.Activity.Id,
            Operation = new Operation { Id = Guid.NewGuid(), Name = "Other" } };
        f.Context.Add(other);
        f.Context.FinalPackings.Add(new FinalPacking { Id = Guid.NewGuid(), ProductionScheduleProductId = f.Batch.Id, ProductionActivityStepId = other.Id });
        f.Context.AnalyticalTestRequests.Add(new AnalyticalTestRequest { Id = Guid.NewGuid(), ProductionScheduleProductId = Guid.NewGuid(), ProductionActivityStepId = f.Step.Id });
        f.Context.Requisitions.Add(new Requisition { Id = Guid.NewGuid(), ProductionScheduleProductId = f.Batch.Id,
            ProductionActivityStepId = other.Id, RequestedById = f.Actor.Id, DepartmentId = f.Actor.DepartmentId.Value,
            Items = [], Approvals = [] });
        f.Instruction(OperationAction.FinalPackingAndPartialReturn);
        f.Instruction(OperationAction.StockRequisitionOrIssue);
        await f.Save();
        var result = await f.Read();
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.FinalPackings);
        Assert.Empty(result.Value.Requisitions);
        Assert.Empty(result.Value.AnalyticalTests);
    }

    [Fact]
    public async Task Legacy_batch_records_extra_material_and_returns_are_labelled_batch_context_on_cancelled_runs()
    {
        await using var f = await Fixture.Create();
        f.Batch.Cancelled = true;
        f.Instruction(OperationAction.AdditionalStockRequestOrIssue);
        f.Instruction(OperationAction.FullReturn);
        f.Instruction(OperationAction.FinalPackingAndPartialReturn);
        var material = new Material { Id = Guid.NewGuid(), Name = "Bottle", Code = "PM-1" };
        var extra = new ProductionExtraPacking { Id = Guid.NewGuid(), ProductionScheduleProductId = f.Batch.Id,
            Material = material, MaterialId = material.Id, Quantity = 11.000123m, Status = ProductionExtraPackingStatus.Approved,
            IssuedBy = f.Actor, IssuedAt = DateTime.UtcNow,
            UoM = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Piece", Symbol = "pcs" } };
        var packing = new FinalPacking { Id = Guid.NewGuid(), ProductionScheduleProductId = f.Batch.Id };
        var returned = new MaterialReturnNote { Id = Guid.NewGuid(), ProductionScheduleProductId = f.Batch.Id,
            FullReturns = [new() { Id = Guid.NewGuid(), MaterialBatchReservedQuantity = new()
                { Id = Guid.NewGuid(), MaterialBatch = new() { Id = Guid.NewGuid(), Material = material }, Quantity = 10 } }] };
        f.Context.AddRange(extra, packing, returned);
        await f.Save();
        var result = await f.Read();
        Assert.True(result.IsSuccess);
        Assert.Equal(11.000123m, Assert.Single(result.Value.ExtraMaterialRequests).Quantity);
        Assert.Equal(f.Actor.Id, result.Value.ExtraMaterialRequests[0].IssuedBy.Id);
        Assert.Single(result.Value.MaterialReturns);
        Assert.Contains(extra.Id, result.Value.BatchScopedRecordIds);
        Assert.Contains(packing.Id, result.Value.BatchScopedRecordIds);
        Assert.Contains(returned.Id, result.Value.BatchScopedRecordIds);
    }

    [Theory]
    [InlineData("permission")]
    [InlineData("disabled")]
    [InlineData("department")]
    [InlineData("activity")]
    [InlineData("deleted")]
    public async Task Missing_grants_disabled_users_wrong_scope_and_deleted_steps_are_denied(string cause)
    {
        await using var f = await Fixture.Create();
        if (cause == "permission") f.Context.PermissionTypes.RemoveRange(f.Context.PermissionTypes);
        if (cause == "disabled") f.Actor.IsDisabled = true;
        if (cause == "department") { f.Actor.Department = new Department { Id = Guid.NewGuid(), Name = "Other", Type = DepartmentType.Production }; f.Actor.DepartmentId = f.Actor.Department.Id; f.Context.Departments.Add(f.Actor.Department); }
        if (cause == "deleted") f.Step.DeletedAt = DateTime.UtcNow;
        await f.Save();
        var result = await f.Read(cause == "activity" ? Guid.NewGuid() : f.Activity.Id);
        Assert.True(result.IsFailure);
    }

    private sealed class CurrentActor : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => "NonProduction";
    }

    private sealed class Fixture : IAsyncDisposable
    {
        public ApplicationDbContext Context { get; }
        private ServiceProvider Services { get; }
        public User Actor { get; }
        public ProductionScheduleProduct Batch { get; }
        public ProductionActivity Activity { get; }
        public ProductionActivityStep Step { get; }
        private Fixture()
        {
            Context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new CurrentActor());
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(Context);
            var http = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
            http.HttpContext.Request.Host = new HostString("oryx.test");
            services.AddSingleton<IHttpContextAccessor>(http);
            services.AddAutoMapper(configuration => { }, typeof(OryxMapper));
            Services = services.BuildServiceProvider();
            var department = new Department { Id = Guid.NewGuid(), Name = "Syrup", Type = DepartmentType.Production };
            Actor = new User { Id = Guid.NewGuid(), FirstName = "Eunice", LastName = "Adams", Department = department, DepartmentId = department.Id };
            var product = new Product { Id = Guid.NewGuid(), Name = "Coldrilif", Department = department, DepartmentId = department.Id };
            var schedule = new ProductionSchedule { Id = Guid.NewGuid(), Code = "PS031" };
            Batch = new ProductionScheduleProduct { Id = Guid.NewGuid(), Product = product, ProductId = product.Id,
                ProductionSchedule = schedule, ProductionScheduleId = schedule.Id, BatchNumber = "4862" };
            Activity = new ProductionActivity { Id = Guid.NewGuid(), ProductionScheduleProduct = Batch, ProductionScheduleProductId = Batch.Id };
            Step = new ProductionActivityStep { Id = Guid.NewGuid(), ProductionActivity = Activity, ProductionActivityId = Activity.Id,
                Operation = new Operation { Id = Guid.NewGuid(), Name = "Intermediate" } };
            Context.AddRange(Actor, Step);
        }
        public static async Task<Fixture> Create()
        {
            var f = new Fixture();
            var role = new Role { Id = Guid.NewGuid(), Name = "Reader" };
            f.Context.Add(role);
            f.Context.UserRoles.Add(new IdentityUserRole<Guid> { UserId = f.Actor.Id, RoleId = role.Id });
            var claim = new IdentityRoleClaim<Guid> { RoleId = role.Id, ClaimType = AppConstants.Permission, ClaimValue = PermissionKeys.CanViewPlannedProducts };
            f.Context.RoleClaims.Add(claim);
            await f.Context.SaveChangesAsync();
            f.Context.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), RoleClaimId = claim.Id, Key = PermissionKeys.CanViewPlannedProducts, Type = "View" });
            await f.Context.SaveChangesAsync();
            return f;
        }
        public void Instruction(OperationAction action) => Context.ProductionActivityStepUsers.Add(new ProductionActivityStepUser
            { Id = Guid.NewGuid(), ProductionActivityStepId = Step.Id, User = Actor, UserId = Actor.Id, Action = action });
        public BatchManufacturingRecord Bmr()
        {
            var record = new BatchManufacturingRecord { Id = Guid.NewGuid(), ProductionScheduleProductId = Batch.Id,
                ProductionActivityStepId = Step.Id, BatchNumber = "4862", CreatedBy = Actor };
            Context.Add(record);
            return record;
        }
        public async Task Save() { await Context.SaveChangesAsync(); Context.ChangeTracker.Clear(); }
        public Task<Result<ProductionStepRecordsDto>> Read(Guid? activityId = null) => ProductionStepRecordsQuery.Read(Context,
            Services.GetRequiredService<IMapper>(), activityId ?? Activity.Id, Step.Id, Actor.Id, CancellationToken.None);
        public async ValueTask DisposeAsync() { await Context.DisposeAsync(); await Services.DisposeAsync(); }
    }
}
