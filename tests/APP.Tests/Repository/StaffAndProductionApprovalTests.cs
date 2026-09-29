using APP.Repository;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.ProductionOrders;
using DOMAIN.Entities.StaffRequisitions;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file sealed class ApprovalTestUser : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public class StaffAndProductionApprovalTests
{
    private static ApplicationDbContext Context() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
        new ApprovalTestUser());

    [Fact]
    public void Legacy_stage_lookup_accepts_any_assigned_role_and_skips_other_users_stages()
    {
        var reviewer = Guid.NewGuid();
        var otherRole = Guid.NewGuid();
        var assignedRole = Guid.NewGuid();
        var repository = new ApprovalRepository(
            null!, null!, null!, null!, NullLogger<ApprovalRepository>.Instance,
            null!, null!);
        var stages = new List<ResponsibleApprovalStage>
        {
            new() { Order = 1, Required = false, UserId = Guid.NewGuid(), ActivatedAt = DateTime.UtcNow },
            new() { Order = 2, Required = true, RoleId = assignedRole, ActivatedAt = DateTime.UtcNow },
        };

        var current = repository.GetCurrentApprovalStage(
            stages, reviewer, new List<Guid> { otherRole, assignedRole });

        Assert.Single(current);
        Assert.Equal(assignedRole, current[0].RoleId);
    }

    [Fact]
    public async Task Staff_review_activates_next_stage_then_finishes_with_audit_trail()
    {
        await using var db = Context();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var request = new StaffRequisition
        {
            Id = Guid.NewGuid(), DepartmentId = Guid.NewGuid(),
            DesignationId = Guid.NewGuid(),
            Approvals =
            [
                new StaffRequisitionApproval
                {
                    Id = Guid.NewGuid(), ApprovalId = Guid.NewGuid(),
                    Order = 1, Required = true, UserId = first,
                    ActivatedAt = DateTime.UtcNow,
                },
                new StaffRequisitionApproval
                {
                    Id = Guid.NewGuid(), ApprovalId = Guid.NewGuid(),
                    Order = 2, Required = true, UserId = second,
                }
            ]
        };
        db.StaffRequisitions.Add(request);
        await db.SaveChangesAsync();

        var early = await StaffAndProductionApprovalHandler.ReviewStaffAsync(
            db, request.Id, second, [], "too early", ApprovalStatus.Approved);
        Assert.False(early.IsSuccess);

        var firstResult = await StaffAndProductionApprovalHandler.ReviewStaffAsync(
            db, request.Id, first, [], "Reviewed", ApprovalStatus.Approved);
        Assert.True(firstResult.IsSuccess);
        Assert.NotNull(request.Approvals[1].ActivatedAt);
        Assert.False(request.Approved);

        var final = await StaffAndProductionApprovalHandler.ReviewStaffAsync(
            db, request.Id, second, [], "Approved", ApprovalStatus.Approved);
        Assert.True(final.IsSuccess);
        Assert.True(request.Approved);
        Assert.Equal(StaffRequisitionStatus.Approved, request.StaffRequisitionStatus);
        Assert.Equal(2, db.ApprovalActionLogs.Count());
    }

    [Fact]
    public async Task Production_rejection_stops_order_and_records_reviewer()
    {
        await using var db = Context();
        var reviewer = Guid.NewGuid();
        var order = new ProductionOrder
        {
            Id = Guid.NewGuid(), Code = "PO/26/001", CustomerId = Guid.NewGuid(),
            Approvals =
            [
                new ProductionOrderApprovals
                {
                    Id = Guid.NewGuid(), ApprovalId = Guid.NewGuid(),
                    Order = 1, Required = true, UserId = reviewer,
                    ActivatedAt = DateTime.UtcNow,
                }
            ]
        };
        db.ProductionOrders.Add(order);
        await db.SaveChangesAsync();

        var result = await StaffAndProductionApprovalHandler.ReviewProductionAsync(
            db, order.Id, reviewer, [], "Capacity unavailable", ApprovalStatus.Rejected);

        Assert.True(result.IsSuccess);
        Assert.False(order.Approved);
        Assert.Equal(ApprovalStatus.Rejected, order.Approvals[0].Status);
        Assert.Equal(reviewer, order.Approvals[0].ApprovedById);
        Assert.Single(db.ApprovalActionLogs);
    }
}
