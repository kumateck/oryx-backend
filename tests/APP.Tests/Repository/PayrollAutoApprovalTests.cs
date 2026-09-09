using APP.Repository;
using DOMAIN.Entities.Payroll;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class PayrollApprovalCurrentUser : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public class PayrollAutoApprovalTests
{
    [Fact]
    public async Task MissingWorkflow_AutoApprovesPayrollRun()
    {
        await using var context = CreateContext();
        var run = new PayrollRun
        {
            Id = Guid.NewGuid(),
            PeriodStart = new DateTime(2026, 9, 1),
            PeriodEnd = new DateTime(2026, 9, 30),
            Status = PayrollRunStatus.PendingApproval,
        };
        context.PayrollRuns.Add(run);
        await context.SaveChangesAsync();

        await CreateRepository(context).CreateInitialApprovalsAsync(nameof(PayrollRun), run.Id);

        Assert.True(run.Approved);
        Assert.Equal(PayrollRunStatus.Approved, run.Status);
        Assert.Empty(context.PayrollRunApprovals);
        Assert.Single(context.ApprovalActionLogs);
    }

    private static ApprovalRepository CreateRepository(ApplicationDbContext context) =>
        new(
            context,
            null!,
            null!,
            null!,
            NullLogger<ApprovalRepository>.Instance,
            null!,
            new NoOpProductionActivityStepEventPublisher()
        );

    private static ApplicationDbContext CreateContext() =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            new PayrollApprovalCurrentUser()
        );
}
