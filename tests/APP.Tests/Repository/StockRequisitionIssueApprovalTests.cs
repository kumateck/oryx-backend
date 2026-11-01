using APP.Repository;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Requisitions;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class FakeCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public class StockRequisitionIssueApprovalTests
{
    private static ApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            new FakeCurrentUserService());

    private static async Task<(StockRequisitionIssue Issue, Guid Issuer, Guid Supervisor)> SeedIssue(ApplicationDbContext context)
    {
        var issuer = Guid.NewGuid();
        var supervisor = Guid.NewGuid();
        var approval = new Approval { Id = Guid.NewGuid(), ItemType = StockRequisitionIssueApprovalHandler.ModelType };
        var issue = new StockRequisitionIssue { Id = Guid.NewGuid(), RequisitionId = Guid.NewGuid(), IssuedById = issuer, IssuedAt = DateTime.UtcNow };
        context.Approvals.Add(approval);
        context.StockRequisitionIssues.Add(issue);
        await context.SaveChangesAsync();
        await StockRequisitionIssueApprovalHandler.CreateAsync(context, issue.Id,
            [new ApprovalStage { Id = Guid.NewGuid(), ApprovalId = approval.Id, Order = 1, Required = true, UserId = supervisor }], approval);
        return (issue, issuer, supervisor);
    }

    [Fact]
    public async Task Issuer_CannotCheckTheirOwnIssue()
    {
        await using var context = CreateContext();
        var (issue, issuer, _) = await SeedIssue(context);

        var result = await StockRequisitionIssueApprovalHandler.ReviewAsync(context, issue.Id, issuer, [], null, ApprovalStatus.Approved);

        Assert.True(result.IsFailure);
        Assert.Equal("Approval.SelfReview", result.Error.Code);
        Assert.False((await context.StockRequisitionIssues.SingleAsync()).Approved);
    }

    [Fact]
    public async Task SupervisorApproval_ChecksTheIssue_AndBecomesTheChecker()
    {
        await using var context = CreateContext();
        var (issue, _, supervisor) = await SeedIssue(context);

        var result = await StockRequisitionIssueApprovalHandler.ReviewAsync(context, issue.Id, supervisor, [], "Quantities verified", ApprovalStatus.Approved);

        Assert.True(result.IsSuccess);
        var saved = await context.StockRequisitionIssues.Include(item => item.Approvals).SingleAsync();
        Assert.True(saved.Approved);
        Assert.Equal(supervisor, StockRequisitionIssueApprovalHandler.Checker(saved)?.ApprovedById);
        // A decided check cannot be decided again.
        var again = await StockRequisitionIssueApprovalHandler.ReviewAsync(context, issue.Id, supervisor, [], null, ApprovalStatus.Rejected);
        Assert.True(again.IsFailure);
    }

    [Fact]
    public async Task UncheckedIssue_HasNoChecker()
    {
        await using var context = CreateContext();
        var (issue, _, _) = await SeedIssue(context);
        var saved = await context.StockRequisitionIssues.Include(item => item.Approvals).SingleAsync(item => item.Id == issue.Id);
        Assert.Null(StockRequisitionIssueApprovalHandler.Checker(saved));
    }
}
