using APP.Repository;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Forms;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class ResponseSubmissionCurrentUser : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public class ResponseApprovalSubmissionTests
{
    [Theory]
    [InlineData(0, false, false, "Response.ApprovalPending")]
    [InlineData(1, true, false, "Response.AlreadyApproved")]
    [InlineData(2, false, true, "Response.RevisionRequired")]
    public async Task ValidateAsync_PreventsAnotherCoaApprovalRound(
        int statusNumber,
        bool approved,
        bool rejected,
        string expectedCode)
    {
        await using var context = CreateContext();
        var response = SeedConfiguredResponse(context, approved, rejected);
        var approval = context.Approvals.Local.Single();
        context.ResponseApprovals.Add(new ResponseApproval
        {
            Id = Guid.NewGuid(),
            ResponseId = response.Id,
            ApprovalId = approval.Id,
            ApprovalRound = 1,
            Order = 1,
            Required = true,
            Status = (ApprovalStatus)statusNumber,
        });
        await context.SaveChangesAsync();

        var result = await ResponseApprovalSubmission.ValidateAsync(context, response.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(expectedCode, result.Error.Code);
    }

    [Fact]
    public async Task ValidateAsync_AllowsFirstCoaSubmission()
    {
        await using var context = CreateContext();
        var response = SeedConfiguredResponse(context, false, false);
        await context.SaveChangesAsync();

        var result = await ResponseApprovalSubmission.ValidateAsync(context, response.Id);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ValidateAsync_AllowsSubmission_WhenWorkflowIsMissing()
    {
        await using var context = CreateContext();
        var form = new Form { Id = Guid.NewGuid(), Name = "Finished product" };
        var response = new Response
        {
            Id = Guid.NewGuid(),
            FormId = form.Id,
            Form = form,
        };
        context.AddRange(form, response);
        await context.SaveChangesAsync();

        var result = await ResponseApprovalSubmission.ValidateAsync(context, response.Id);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ValidateAsync_AllowsSubmission_WhenWorkflowHasNoStages()
    {
        await using var context = CreateContext();
        var form = new Form { Id = Guid.NewGuid(), Name = "Finished product" };
        var response = new Response
        {
            Id = Guid.NewGuid(),
            FormId = form.Id,
            Form = form,
        };
        context.AddRange(
            form,
            response,
            new Approval
            {
                Id = Guid.NewGuid(),
                ItemType = nameof(Response),
                ApprovalStages = [],
            });
        await context.SaveChangesAsync();

        var result = await ResponseApprovalSubmission.ValidateAsync(context, response.Id);

        Assert.True(result.IsSuccess);
    }

    private static Response SeedConfiguredResponse(
        ApplicationDbContext context,
        bool approved,
        bool rejected)
    {
        var form = new Form { Id = Guid.NewGuid(), Name = "Finished product" };
        var response = new Response
        {
            Id = Guid.NewGuid(),
            FormId = form.Id,
            Form = form,
            Approved = approved,
            Rejected = rejected,
        };
        var approval = new Approval
        {
            Id = Guid.NewGuid(),
            ItemType = nameof(Response),
            ApprovalStages = [],
        };
        approval.ApprovalStages.Add(new ApprovalStage
        {
            Id = Guid.NewGuid(),
            ApprovalId = approval.Id,
            Approval = approval,
            Order = 1,
            Required = true,
            UserId = Guid.NewGuid(),
        });
        context.AddRange(form, response, approval);
        return response;
    }

    private static ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
        new ResponseSubmissionCurrentUser());
}
