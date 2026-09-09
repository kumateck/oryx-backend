using APP.Repository;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Requisitions.Request;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class RequisitionIssueCurrentUser : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public class RequisitionIssueApprovalTests
{
    [Fact]
    public async Task IssueStockRequisition_RejectsRequisitionAwaitingApproval()
    {
        await using var context = CreateContext();
        var requisition = CreateStockRequisition(approved: false);
        context.Requisitions.Add(requisition);
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).IssueStockRequisition(
            requisition.Id,
            Guid.NewGuid()
        );

        Assert.True(result.IsFailure);
        Assert.Equal("Requisition.ApprovalRequired", result.Error.Code);
        Assert.Equal(RequestStatus.New, requisition.Status);
    }

    [Fact]
    public async Task IssueStockRequisition_AllowsAutoApprovedRequisitionPastApprovalGuard()
    {
        await using var context = CreateContext();
        var requisition = CreateStockRequisition(approved: true);
        context.Requisitions.Add(requisition);
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).IssueStockRequisition(
            requisition.Id,
            Guid.NewGuid()
        );

        Assert.True(result.IsFailure);
        Assert.NotEqual("Requisition.ApprovalRequired", result.Error.Code);
    }

    [Fact]
    public async Task LegacyRequisitionApproval_CannotBypassConfiguredWorkflow()
    {
        await using var context = CreateContext();
        var requisition = CreateStockRequisition(approved: false);
        context.Requisitions.Add(requisition);
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).ApproveRequisition(
            new ApproveRequisitionRequest(),
            requisition.Id,
            Guid.NewGuid(),
            [Guid.NewGuid()]
        );

        Assert.True(result.IsFailure);
        Assert.Equal("Requisition.ApprovalWorkflowRequired", result.Error.Code);
        Assert.False(requisition.Approved);
    }

    private static Requisition CreateStockRequisition(bool approved) =>
        new()
        {
            Id = Guid.NewGuid(),
            Code = "SR-APPROVAL-001",
            RequestedById = Guid.NewGuid(),
            DepartmentId = Guid.NewGuid(),
            RequisitionType = RequisitionType.Stock,
            Status = approved ? RequestStatus.Pending : RequestStatus.New,
            Approved = approved,
            ProductionScheduleProductId = Guid.NewGuid(),
            Items = [],
        };

    private static RequisitionRepository CreateRepository(ApplicationDbContext context) =>
        new(
            context,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            new NoOpProductionActivityStepEventPublisher()
        );

    private static ApplicationDbContext CreateContext() =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            new RequisitionIssueCurrentUser()
        );
}
