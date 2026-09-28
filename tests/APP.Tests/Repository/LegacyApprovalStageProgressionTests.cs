using APP.Repository;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.Requisitions;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file sealed class StageTestUser : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public class LegacyApprovalStageProgressionTests
{
    [Fact]
    public async Task Order_approval_uses_second_role_and_activates_next_users_stage()
    {
        await using var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            new StageTestUser());
        var firstReviewer = Guid.NewGuid();
        var secondReviewer = Guid.NewGuid();
        var assignedRole = Guid.NewGuid();
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "Test supplier" };
        var source = new SourceRequisition
        {
            Id = Guid.NewGuid(), SupplierId = supplier.Id, Supplier = supplier,
        };
        var order = new PurchaseOrder
        {
            Id = Guid.NewGuid(), Code = "PO/26/02",
            SourceRequisitionId = source.Id, SourceRequisition = source,
            SupplierId = supplier.Id, Supplier = supplier,
            Approvals =
            [
                new PurchaseOrderApproval
                {
                    Id = Guid.NewGuid(), ApprovalId = Guid.NewGuid(),
                    Order = 1, Required = true, RoleId = assignedRole,
                    ActivatedAt = DateTime.UtcNow,
                },
                new PurchaseOrderApproval
                {
                    Id = Guid.NewGuid(), ApprovalId = Guid.NewGuid(),
                    Order = 2, Required = true, UserId = secondReviewer,
                },
            ],
        };
        db.AddRange(supplier, source, order);
        await db.SaveChangesAsync();
        var repository = new ApprovalRepository(
            db, null!, null!, null!, NullLogger<ApprovalRepository>.Instance,
            null!, new NoOpProductionActivityStepEventPublisher());

        var early = await repository.ApproveItem(
            nameof(PurchaseOrder), order.Id, secondReviewer, [], "Too early");
        Assert.False(early.IsSuccess);

        var first = await repository.ApproveItem(
            nameof(PurchaseOrder), order.Id, firstReviewer,
            [Guid.NewGuid(), assignedRole], "Stage one");
        Assert.True(first.IsSuccess, string.Join("; ", first.Errors.Select(error => error.Description)));
        Assert.NotNull(order.Approvals[1].ActivatedAt);
        Assert.False(order.Approved);

        var second = await repository.ApproveItem(
            nameof(PurchaseOrder), order.Id, secondReviewer, [], "Stage two");
        Assert.True(second.IsSuccess);
        Assert.True(order.Approved);
        Assert.Equal(2, db.ApprovalActionLogs.Count());
    }
}
