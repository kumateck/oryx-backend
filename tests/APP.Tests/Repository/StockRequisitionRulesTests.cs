using APP.Services.Production;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Routes;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.ProductionSchedules.Packing;
using DOMAIN.Entities.Requisitions;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public class StockRequisitionRulesTests
{
    private static readonly Guid Batch = Guid.NewGuid();

    private sealed class CurrentUser : ICurrentUserService
    { public Guid? UserId => null; public Guid? DepartmentId => null; public string DepartmentType => string.Empty; }

    private static ApplicationDbContext Context() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new CurrentUser());

    private static Requisition Stock(string prefix, Guid? stepId = null) => new()
    {
        Id = Guid.NewGuid(), Code = $"{prefix}/N/26/001", RequisitionType = RequisitionType.Stock,
        ProductionScheduleProductId = Batch, ProductionActivityStepId = stepId,
    };

    private static ProductionActivityStep Step(params OperationAction[] actions) => new()
    {
        Id = Guid.NewGuid(),
        Operation = new Operation { Id = Guid.NewGuid(), Name = "Operation" },
        ResponsibleUsers = actions.Select(action => new ProductionActivityStepUser
        {
            Id = Guid.NewGuid(), Action = action,
            User = new User { Id = Guid.NewGuid(), UserName = $"user-{Guid.NewGuid():N}", FirstName = "Test", LastName = "User" },
        }).ToList(),
    };

    private static async Task<string?> Raise(ApplicationDbContext context, bool raw, bool packaging)
    {
        var result = await StockRequisitionRules.CanRaise(context, Batch, raw, packaging);
        return result.IsSuccess ? null : result.Errors.Single().Code;
    }

    [Fact]
    public async Task Packaging_waits_for_the_raw_requisition()
    {
        await using var context = Context();

        Assert.Equal("StockRequisition.RawFirst", await Raise(context, raw: false, packaging: true));
        Assert.Null(await Raise(context, raw: true, packaging: false));
        Assert.Null(await Raise(context, raw: true, packaging: true));
    }

    [Fact]
    public async Task Each_kind_is_requisitioned_once_and_packaging_any_time_before_final_packing()
    {
        await using var context = Context();
        context.Requisitions.Add(Stock(StockRequisitionRules.RawPrefix));
        await context.SaveChangesAsync();

        Assert.Equal("StockRequisition.RawAlreadyRaised", await Raise(context, raw: true, packaging: false));
        Assert.Null(await Raise(context, raw: false, packaging: true));

        context.Requisitions.Add(Stock(StockRequisitionRules.PackagingPrefix));
        await context.SaveChangesAsync();
        Assert.Equal("StockRequisition.PackagingAlreadyRaised", await Raise(context, raw: false, packaging: true));
    }

    [Fact]
    public async Task Packaging_cannot_be_requisitioned_after_final_packing()
    {
        await using var context = Context();
        context.Requisitions.Add(Stock(StockRequisitionRules.RawPrefix));
        context.FinalPackings.Add(new FinalPacking { Id = Guid.NewGuid(), ProductionScheduleProductId = Batch });
        await context.SaveChangesAsync();

        Assert.Equal("StockRequisition.FinalPackingRecorded", await Raise(context, raw: false, packaging: true));
    }

    [Fact]
    public async Task Extra_material_needs_the_main_requisition_of_its_kind()
    {
        await using var context = Context();
        context.Requisitions.Add(Stock(StockRequisitionRules.RawPrefix));
        await context.SaveChangesAsync();

        Assert.True((await StockRequisitionRules.CanRequestExtra(context, Batch, [MaterialKind.Raw])).IsSuccess);
        var packaging = await StockRequisitionRules.CanRequestExtra(context, Batch, [MaterialKind.Raw, MaterialKind.Package]);
        Assert.Equal("ExtraPacking.MainRequisitionMissing", packaging.Errors.Single().Code);

        context.Requisitions.Add(Stock(StockRequisitionRules.PackagingPrefix));
        await context.SaveChangesAsync();
        Assert.True((await StockRequisitionRules.CanRequestExtra(context, Batch, [MaterialKind.Package])).IsSuccess);
    }

    [Fact]
    public async Task Requisitions_move_only_the_batch_stock_requisition_step()
    {
        await using var context = Context();
        var stockStep = Step(OperationAction.StockRequisitionOrIssue);
        var unassignedRawStep = Step();
        var mixingStep = Step(OperationAction.BmrAndBprRequisitionOrIssue);
        context.AddRange(stockStep, unassignedRawStep, mixingStep, Stock(StockRequisitionRules.RawPrefix, unassignedRawStep.Id));
        await context.SaveChangesAsync();

        Assert.True(await StockRequisitionRules.DrivesStep(context, stockStep.Id));
        // The step the raw requisition came from keeps moving with it, even without a stock requisition assignee.
        Assert.True(await StockRequisitionRules.DrivesStep(context, unassignedRawStep.Id));
        // Packaging requested later from an ordinary step leaves it to its own completion.
        Assert.False(await StockRequisitionRules.DrivesStep(context, mixingStep.Id));
    }
}
