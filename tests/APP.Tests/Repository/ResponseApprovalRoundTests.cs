using APP.Repository;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Forms;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class ResponseRoundCurrentUser : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public class ResponseApprovalRoundTests
{
    [Fact]
    public async Task StartAsync_PreservesApprovedHistoryAndStartsNewPendingRound()
    {
        await using var context = CreateContext();
        var response = SeedResponse(context, approved: true);
        var approval = CreateApproval();
        context.ResponseApprovals.Add(new ResponseApproval
        {
            Id = Guid.NewGuid(), ResponseId = response.Id, ApprovalId = approval.Id,
            ApprovalRound = 1, Order = 1, Required = true,
            Status = ApprovalStatus.Approved, ApprovalTime = DateTime.UtcNow,
        });
        await context.SaveChangesAsync();

        var started = await ResponseApprovalRoundManager.StartAsync(
            context, response.Id, approval.ApprovalStages, approval);
        var rows = await context.ResponseApprovals
            .Where(item => item.ResponseId == response.Id)
            .OrderBy(item => item.ApprovalRound).ToListAsync();

        Assert.True(started);
        Assert.Equal(2, rows.Count);
        Assert.Equal(ApprovalStatus.Approved, rows[0].Status);
        Assert.Equal(1, rows[0].ApprovalRound);
        Assert.Equal(ApprovalStatus.Pending, rows[1].Status);
        Assert.Equal(2, rows[1].ApprovalRound);
        Assert.NotNull(rows[1].ActivatedAt);
        Assert.False(response.Approved);
        Assert.False(response.Rejected);
    }

    [Fact]
    public async Task StartAsync_DoesNotDuplicateCurrentPendingRound()
    {
        await using var context = CreateContext();
        var response = SeedResponse(context);
        var approval = CreateApproval();
        await context.SaveChangesAsync();

        Assert.True(await ResponseApprovalRoundManager.StartAsync(
            context, response.Id, approval.ApprovalStages, approval));
        Assert.False(await ResponseApprovalRoundManager.StartAsync(
            context, response.Id, approval.ApprovalStages, approval));
        Assert.Single(await context.ResponseApprovals
            .Where(item => item.ResponseId == response.Id).ToListAsync());
    }

    [Fact]
    public async Task StartAsync_ScopesApprovalsToStageSpecificResponse()
    {
        await using var context = CreateContext();
        var batchId = Guid.NewGuid();
        var first = SeedResponse(context, batchId: batchId, stepId: Guid.NewGuid());
        var second = SeedResponse(context, batchId: batchId, stepId: Guid.NewGuid());
        var approval = CreateApproval();
        await context.SaveChangesAsync();

        await ResponseApprovalRoundManager.StartAsync(
            context, first.Id, approval.ApprovalStages, approval);
        await ResponseApprovalRoundManager.StartAsync(
            context, second.Id, approval.ApprovalStages, approval);

        Assert.Single(await context.ResponseApprovals
            .Where(item => item.ResponseId == first.Id).ToListAsync());
        Assert.Single(await context.ResponseApprovals
            .Where(item => item.ResponseId == second.Id).ToListAsync());
    }

    private static Response SeedResponse(
        ApplicationDbContext context,
        bool approved = false,
        Guid? batchId = null,
        Guid? stepId = null)
    {
        var form = new Form { Id = Guid.NewGuid(), Name = "Stage test form" };
        var response = new Response
        {
            Id = Guid.NewGuid(), FormId = form.Id, Form = form,
            BatchManufacturingRecordId = batchId,
            ProductionActivityStepId = stepId,
            Approved = approved,
        };
        context.AddRange(form, response);
        return response;
    }

    private static Approval CreateApproval()
    {
        var approval = new Approval
        {
            Id = Guid.NewGuid(), ItemType = nameof(Response), ApprovalStages = [],
        };
        approval.ApprovalStages.Add(new ApprovalStage
        {
            Id = Guid.NewGuid(), ApprovalId = approval.Id, Order = 1,
            Required = true, UserId = Guid.NewGuid(),
        });
        return approval;
    }

    private static ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
        new ResponseRoundCurrentUser());
}
