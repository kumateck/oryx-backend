using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.ProductionSchedules;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SHARED;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public class BmrBatchNumberTests
{
    private static readonly Guid DepartmentId = Guid.NewGuid();

    [Theory]
    [InlineData("BATCH-1")]
    [InlineData(" batch-1 ")]
    public async Task A_number_is_unique_across_products_and_departments(string requested)
    {
        await using var db = Context();
        await Existing(db, "BATCH-1", otherDepartment: true);
        var result = await BmrBatchNumberRules.Validate(db, requested);
        Assert.Equal("Bmr.DuplicateBatchNumber", result.Error.Code);
        Assert.Contains("already exists", result.Error.Description);
    }

    [Fact]
    public async Task A_record_can_keep_its_own_number_but_cannot_take_another_records_number()
    {
        await using var db = Context();
        var first = await Existing(db, "BATCH-1");
        await Existing(db, "BATCH-2");
        Assert.True((await BmrBatchNumberRules.Validate(db, " batch-1 ", first.Id)).IsSuccess);
        Assert.Equal("Bmr.DuplicateBatchNumber", (await BmrBatchNumberRules.Validate(db, "BATCH-2", first.Id)).Error.Code);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(false, true, true)]
    public async Task Deletion_cancellation_or_partial_return_alone_cannot_release_a_number(bool deleted, bool cancelled, bool partial)
    {
        await using var db = Context();
        var old = await Existing(db, "BATCH-1");
        old.DeletedAt = deleted ? DateTime.UtcNow : null;
        old.ProductionScheduleProduct.Cancelled = cancelled;
        db.MaterialReturnNotes.Add(new() { ProductionScheduleProductId = old.ProductionScheduleProductId, BatchNumber = "BATCH-1",
            PartialReturns = partial ? [new()] : [] });
        await db.SaveChangesAsync();
        Assert.Equal("Bmr.DuplicateBatchNumber", (await BmrBatchNumberRules.Validate(db, "BATCH-1")).Error.Code);
    }

    [Fact]
    public async Task Activated_full_return_releases_only_the_retired_records_number_and_preserves_history()
    {
        await using var db = Context();
        var old = await Existing(db, "BATCH-1");
        old.DeletedAt = DateTime.UtcNow;
        old.ProductionScheduleProduct.Cancelled = true;
        db.MaterialReturnNotes.Add(new() { ProductionScheduleProductId = old.ProductionScheduleProductId,
            BatchNumber = "BATCH-1", Status = MaterialReturnStatus.Pending, FullReturns = [new()] });
        await db.SaveChangesAsync();
        Assert.True((await BmrBatchNumberRules.Validate(db, " batch-1 ")).IsSuccess);
        await Existing(db, "BATCH-1");
        Assert.Equal("Bmr.DuplicateBatchNumber", (await BmrBatchNumberRules.Validate(db, "BATCH-1")).Error.Code);
        Assert.Equal(2, await db.BatchManufacturingRecords.IgnoreQueryFilters().CountAsync());
        Assert.Single(await db.MaterialReturnNotes.IgnoreQueryFilters().ToListAsync());
    }

    [Fact]
    public async Task Duplicate_save_does_not_change_the_BMR_BPR_schedule_or_step()
    {
        await using var db = Context();
        await Existing(db, "BATCH-1");
        var current = await Existing(db, "BATCH-2");
        var bpr = new BatchPackagingRecord { ProductionScheduleProductId = current.ProductionScheduleProductId,
            ProductionActivityStepId = current.ProductionActivityStepId, BatchNumber = "BATCH-2" };
        db.BatchPackagingRecords.Add(bpr);
        await db.SaveChangesAsync();
        var repository = Repository(db);
        var duplicate = await repository.UpdateBatchManufacturingRecord(new() { BatchNumber = " batch-1 ", BatchQuantity = 10 }, current.Id);
        Assert.Equal("Bmr.DuplicateBatchNumber", duplicate.Error.Code);
        db.ChangeTracker.Clear();
        Assert.Equal("BATCH-2", (await db.BatchManufacturingRecords.SingleAsync(record => record.Id == current.Id)).BatchNumber);
        Assert.Equal("BATCH-2", (await db.BatchPackagingRecords.SingleAsync()).BatchNumber);
        Assert.Equal("BATCH-2", (await db.ProductionScheduleProducts.SingleAsync(batch => batch.Id == current.ProductionScheduleProductId)).BatchNumber);
        Assert.Null((await db.ProductionActivitySteps.SingleAsync(step => step.Id == current.ProductionActivityStepId)).StartedAt);

        var saved = await repository.UpdateBatchManufacturingRecord(new() { BatchNumber = " BATCH-3 ", BatchQuantity = 10 }, current.Id);
        Assert.True(saved.IsSuccess);
        Assert.Equal("BATCH-3", (await db.BatchManufacturingRecords.SingleAsync(record => record.Id == current.Id)).BatchNumber);
        Assert.Equal("BATCH-3", (await db.BatchPackagingRecords.SingleAsync()).BatchNumber);
        Assert.Equal("BATCH-3", (await db.ProductionScheduleProducts.SingleAsync(batch => batch.Id == current.ProductionScheduleProductId)).BatchNumber);
    }

    [Fact]
    public async Task Create_refuses_duplicates_before_inserting_a_record()
    {
        await using var db = Context();
        await Existing(db, "BATCH-1");
        var result = await Repository(db).CreateBatchManufacturingRecord(new() { BatchNumber = "batch-1", ProductionScheduleProductId = Guid.NewGuid() });
        Assert.Equal("Bmr.DuplicateBatchNumber", result.Error.Code);
        Assert.Single(await db.BatchManufacturingRecords.ToListAsync());
        Assert.Equal("Bmr.BatchNumberRequired", (await BmrBatchNumberRules.Validate(db, " ")).Error.Code);
    }

    [Fact]
    public async Task A_retired_BMR_cannot_be_issued_even_after_its_number_is_reused()
    {
        await using var db = Context();
        var old = await Existing(db, "BATCH-1");
        old.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        var result = await Repository(db).IssueBatchManufacturingRecord(old.Id, Guid.NewGuid());
        Assert.Equal("Bmr.Retired", result.Error.Code);
        Assert.Null(old.IssuedDate);
    }

    private static async Task<BatchManufacturingRecord> Existing(ApplicationDbContext db, string number, bool otherDepartment = false)
    {
        var departmentId = otherDepartment ? Guid.NewGuid() : DepartmentId;
        var department = await db.Departments.IgnoreQueryFilters().SingleOrDefaultAsync(department => department.Id == departmentId)
            ?? new Department { Id = departmentId, Name = "Production", Type = DepartmentType.Production };
        var product = new Product { Name = "Product", Department = department };
        var batch = new ProductionScheduleProduct { Product = product, BatchNumber = number,
            ProductionSchedule = new() { DepartmentId = department.Id } };
        var step = new ProductionActivityStep { Operation = new Operation { Name = "Issue BMR" }, ProductionActivity = new() { ProductionScheduleProduct = batch } };
        var record = new BatchManufacturingRecord { BatchNumber = number, ProductionScheduleProduct = batch, ProductionActivityStep = step };
        db.Add(record);
        await db.SaveChangesAsync();
        return record;
    }

    private static ProductionScheduleRepository Repository(ApplicationDbContext db)
    {
        var services = new ServiceCollection(); services.AddLogging(); services.AddHttpContextAccessor(); services.AddSingleton(db);
        services.AddAutoMapper(configuration => { }, typeof(APP.Mapper.OryxMapper));
        return new(db, services.BuildServiceProvider().GetRequiredService<IMapper>(), null!, null!, null!, null!, new NoOpProductionActivityStepEventPublisher());
    }
    private static ApplicationDbContext Context() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new CurrentUser());
    private sealed class CurrentUser : ICurrentUserService
    { public Guid? UserId => null; public Guid? DepartmentId => BmrBatchNumberTests.DepartmentId; public string DepartmentType => "Production"; }
}
