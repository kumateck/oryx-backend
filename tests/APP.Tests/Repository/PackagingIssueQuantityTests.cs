using APP.Repository;
using APP.Utils;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Requisitions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SHARED;
using Xunit;

namespace APP.Tests.Repository;

public class PackagingIssueQuantityTests
{
    private static RequisitionItem Line(MaterialKind kind, decimal quantity) =>
        new() { Quantity = quantity, Material = new Material { Name = "Labels", Kind = kind } };

    [Fact]
    public void Only_packaging_may_differ_and_any_difference_needs_a_reason()
    {
        Assert.Null(PackagingIssueAdjustment.Problem(Line(MaterialKind.Package, 18.75m), 18.75m, null));
        Assert.Null(PackagingIssueAdjustment.Problem(Line(MaterialKind.Package, 18.75m), 20m, "Sealed packs of 10"));
        Assert.Null(PackagingIssueAdjustment.Problem(Line(MaterialKind.Package, 18.75m), 18m, "Packs of 9 only"));
        Assert.NotNull(PackagingIssueAdjustment.Problem(Line(MaterialKind.Package, 18.75m), 20m, null));
        Assert.NotNull(PackagingIssueAdjustment.Problem(Line(MaterialKind.Package, 18.75m), 18m, "  ok "));
        Assert.NotNull(PackagingIssueAdjustment.Problem(Line(MaterialKind.Package, 18.75m), 0m, "Nothing at all"));
        Assert.NotNull(PackagingIssueAdjustment.Problem(Line(MaterialKind.Raw, 18.75m), 20m, "Sealed packs of 10"));
    }

    [Fact]
    public void The_difference_falls_on_the_last_batch_in_picking_order()
    {
        var soon = new MaterialBatchReservedQuantity { Id = Guid.NewGuid(), MaterialBatch = new MaterialBatch { ExpiryDate = DateTime.UtcNow.AddMonths(6) } };
        var later = new MaterialBatchReservedQuantity { Id = Guid.NewGuid(), MaterialBatch = new MaterialBatch { ExpiryDate = DateTime.UtcNow.AddYears(2) } };
        var returned = new MaterialBatchReservedQuantity { Id = Guid.NewGuid(), MaterialBatch = new MaterialBatch { ExpiryDate = DateTime.UtcNow.AddYears(3), ReturnDate = DateTime.UtcNow } };
        Assert.Equal([returned, soon, later], PackagingIssueAdjustment.PickingOrder([later, soon, returned]));
    }

    [Fact]
    public void The_endpoint_requires_the_packaging_issue_quantity_key()
    {
        var method = typeof(API.Controllers.RequisitionController).GetMethod(nameof(API.Controllers.RequisitionController.AdjustIssuedQuantity))!;
        Assert.Contains(method.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>(),
            attribute => attribute.Policy == PermissionKeys.CanAdjustPackagingMaterialIssueQuantity);
        Assert.Empty(method.GetCustomAttributes(typeof(AllowAnonymousAttribute), true));
    }

    private static async Task<(IssuedMaterialRepairFixture Fixture, Guid Over, Guid Under)> Seeded(bool grant = true)
    {
        var fixture = new IssuedMaterialRepairFixture();
        await fixture.Seed(IssuedMaterialRepairKind.Packaging, stock: 100, issueStock: false);
        await using var db = fixture.Open();
        if (grant)
        {
            var claim = new IdentityRoleClaim<Guid> { Id = 99, RoleId = fixture.RoleId, ClaimType = AppConstants.Permission, ClaimValue = PermissionKeys.CanAdjustPackagingMaterialIssueQuantity };
            db.RoleClaims.Add(claim);
            db.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), RoleClaimId = 99, Key = PermissionKeys.CanAdjustPackagingMaterialIssueQuantity, Type = "Action" });
        }
        // The fixture reserves 22.5 (PMF015) and 95 (PMF093) of two 100-unit lots for this batch.
        var reservations = await db.MaterialBatchReservedQuantities.Include(row => row.MaterialBatch).ThenInclude(row => row.Material).ToListAsync();
        var lines = reservations.Select(row => new RequisitionItem
        { Id = Guid.NewGuid(), MaterialId = row.MaterialBatch.MaterialId, UoMId = fixture.UnitId, Quantity = row.Quantity, Status = RequestStatus.Pending }).ToList();
        db.Requisitions.Add(new Requisition { Id = fixture.RequisitionId, Code = "PM/SYNTHETIC/ISSUE", DepartmentId = fixture.Actor.DepartmentId.GetValueOrDefault(),
            ProductionScheduleProductId = fixture.BatchId, RequestedById = fixture.Actor.UserId.GetValueOrDefault(), RequisitionType = RequisitionType.Stock,
            Approved = true, Status = RequestStatus.Pending, Items = lines });
        await db.SaveChangesAsync();
        var over = lines.Single(line => line.Quantity == 22.5m).Id;
        var under = lines.Single(line => line.Quantity == 95m).Id;
        return (fixture, over, under);
    }

    [RepairPostgresFact]
    public async Task A_supervisor_issues_over_and_under_with_reasons_and_the_stock_follows()
    {
        var (fixture, over, under) = await Seeded();
        await using (fixture)
        {
            var actor = fixture.Actor.UserId.GetValueOrDefault();
            await using (var db = fixture.Open())
            {
                var repository = new RequisitionRepository(db, null!, null!, null!, null!, null!, null!, null!, null!, new NoOpProductionActivityStepEventPublisher());
                // No reason: refused. Then 30 for 22.5 (sealed packs of 10) and 90 for 95.
                Assert.Equal("Stock.IssuedQuantity", (await repository.AdjustIssuedQuantity(fixture.RequisitionId, over, new() { Quantity = 30 }, actor)).Error.Code);
                Assert.True((await repository.AdjustIssuedQuantity(fixture.RequisitionId, over, new() { Quantity = 30, Reason = "Sealed packs of 10" }, actor)).IsSuccess);
                Assert.True((await repository.AdjustIssuedQuantity(fixture.RequisitionId, under, new() { Quantity = 90, Reason = "Only whole packs of 9 left" }, actor)).IsSuccess);
                // More than the lot's free stock is refused.
                Assert.Equal("Stock.IssuedQuantity", (await repository.AdjustIssuedQuantity(fixture.RequisitionId, over, new() { Quantity = 500, Reason = "Far too much" }, actor)).Error.Code);
            }
            await using (var db = fixture.Open())
            {
                var line = await db.RequisitionItems.SingleAsync(item => item.Id == over);
                Assert.Equal((30m, "Sealed packs of 10", actor), (line.IssuedQuantity!.Value, line.IssueAdjustmentReason, line.IssueAdjustedById!.Value));
                var repository = new RequisitionRepository(db, null!, null!, null!, null!, null!, null!, null!, null!, new NoOpProductionActivityStepEventPublisher());
                var issued = await repository.IssueStockRequisition(fixture.RequisitionId, actor);
                Assert.True(issued.IsSuccess, issued.Error?.Description);
            }
            await using (var db = fixture.Open())
            {
                var lines = await db.RequisitionItems.Where(item => item.RequisitionId == fixture.RequisitionId).ToListAsync();
                foreach (var (id, issuedQuantity) in new[] { (over, 30m), (under, 90m) })
                {
                    var material = lines.Single(item => item.Id == id).MaterialId;
                    // What production holds, what left the shelf and what the bin card says all match what was handed over.
                    Assert.Equal(issuedQuantity, await db.MaterialBatchReservedQuantities.Where(row => row.MaterialBatch.MaterialId == material).SumAsync(row => row.Quantity));
                    Assert.Equal(100m - issuedQuantity, await db.ShelfMaterialBatches.Where(row => row.MaterialBatch.MaterialId == material).SumAsync(row => row.Quantity));
                    Assert.Equal(issuedQuantity, await db.BinCardInformation.Where(row => row.MaterialBatch.MaterialId == material).SumAsync(row => row.QuantityIssued));
                }
                Assert.All(lines, line => Assert.Equal(RequestStatus.Completed, line.Status));
            }
        }
    }

    [RepairPostgresFact]
    public async Task Without_the_key_nothing_is_changed()
    {
        var (fixture, over, _) = await Seeded(grant: false);
        await using (fixture)
        {
            await using var db = fixture.Open();
            var repository = new RequisitionRepository(db, null!, null!, null!, null!, null!, null!, null!, null!, new NoOpProductionActivityStepEventPublisher());
            var refused = await repository.AdjustIssuedQuantity(fixture.RequisitionId, over, new() { Quantity = 30, Reason = "Sealed packs of 10" }, fixture.Actor.UserId.GetValueOrDefault());
            Assert.Equal(ErrorType.Forbidden, refused.Error.Type);
            Assert.Null((await db.RequisitionItems.AsNoTracking().SingleAsync(item => item.Id == over)).IssuedQuantity);
        }
    }
}
