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

    // Issues are now checked on their document review (tests/APP.Tests/Reviews). These stages remain only for
    // issues checked before that, whose voucher still names the approver who checked them.
    [Fact]
    public async Task An_issue_checked_through_its_old_stages_names_that_approver_as_its_checker()
    {
        await using var context = CreateContext();
        var (issue, _, supervisor) = await SeedIssue(context);
        var saved = await context.StockRequisitionIssues.Include(item => item.Approvals).SingleAsync();
        var stage = saved.Approvals.Single();
        stage.Status = ApprovalStatus.Approved; stage.ApprovedById = supervisor; stage.ApprovalTime = DateTime.UtcNow;
        saved.Approved = true;
        await context.SaveChangesAsync();
        Assert.Equal(supervisor, StockRequisitionIssueApprovalHandler.Checker(saved)?.ApprovedById);
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
