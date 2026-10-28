using System.Reflection;
using API.Controllers;
using APP.Repository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public class ApprovalConfigurationRepairTests
{
    [Fact]
    public void Updates_require_the_existing_workflow_edit_permission()
    {
        var action = typeof(ApprovalController).GetMethod(nameof(ApprovalController.UpdateApproval));
        Assert.NotNull(action);
        Assert.Contains(action.GetCustomAttributes<AuthorizeAttribute>(), attribute => attribute.Policy == PermissionKeys.CanEditApprovalWorkflow);
    }

    [Fact]
    public async Task A_repair_updates_the_same_policy_and_keeps_existing_document_approvals_and_other_policies()
    {
        var actor = Guid.NewGuid();
        await using var context = Context(actor);
        var role = new Role { Id = Guid.NewGuid(), Name = "General Manager Production" };
        var legacy = Legacy();
        var active = new Approval { Id = Guid.NewGuid(), ItemType = "StockRequisition", ApprovalStages = [new() { Id = Guid.NewGuid(), Order = 1, Required = true, UserId = actor }] };
        var pending = new RequisitionApproval { Id = Guid.NewGuid(), ApprovalId = legacy.Id, RequisitionId = Guid.NewGuid(), Order = 0, Required = true, Status = ApprovalStatus.Pending };
        context.AddRange(new User { Id = actor, FirstName = "Test", LastName = "Operator" }, role, legacy, active, pending);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var result = await Repository(context).UpdateApproval(Request(role.Id), legacy.Id, actor);
        Assert.True(result.IsSuccess);
        context.ChangeTracker.Clear();
        var saved = await context.Approvals.SingleAsync(row => row.Id == legacy.Id);
        Assert.Equal("Stock", saved.ItemType);
        Assert.Equal(TimeSpan.Zero, saved.EscalationDuration);
        Assert.Equal(actor, saved.LastUpdatedById);
        Assert.NotNull(saved.UpdatedAt);
        var stage = Assert.Single(saved.ApprovalStages);
        Assert.Equal(role.Id, stage.RoleId);
        Assert.Null(stage.UserId);
        Assert.True(stage.Required);
        Assert.Equal(1, stage.Order);
        var unchanged = await context.Approvals.SingleAsync(row => row.Id == active.Id);
        Assert.Equal(actor, Assert.Single(unchanged.ApprovalStages).UserId);
        var documentStage = await context.RequisitionApprovals.IgnoreQueryFilters().IgnoreAutoIncludes().SingleAsync(row => row.Id == pending.Id);
        Assert.Equal(ApprovalStatus.Pending, documentStage.Status);
        Assert.True(documentStage.Required);
        Assert.Null(documentStage.RoleId);
        Assert.Null(documentStage.UserId);
        Assert.Equal(0, documentStage.Order);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    public async Task Missing_deleted_or_disabled_approvers_cannot_replace_the_stored_policy(int variant)
    {
        var actor = Guid.NewGuid();
        await using var context = Context(actor);
        var legacy = Legacy();
        var id = Guid.NewGuid();
        if (variant == 1) context.Roles.Add(new Role { Id = id, Name = "Deleted role", DeletedAt = DateTime.UtcNow });
        if (variant is 3 or 4) context.Users.Add(new User { Id = id, FirstName = "Unavailable", LastName = "User", DeletedAt = variant == 3 ? DateTime.UtcNow : null, IsDisabled = variant == 4 });
        context.Approvals.Add(legacy);
        await context.SaveChangesAsync();
        var request = Request(id);
        if (variant >= 2) request.ApprovalStages = [new() { UserId = id, Order = 1, Required = true }];
        var result = await Repository(context).UpdateApproval(request, legacy.Id, actor);
        Assert.False(result.IsSuccess);
        Assert.Equal("Approval.ApproverUnavailable", result.Error.Code);
        context.ChangeTracker.Clear();
        Assert.Equal(3, (await context.Approvals.SingleAsync()).ApprovalStages.Count);
    }

    private static CreateApprovalRequest Request(Guid role) => new() { ItemType = "Stock", EscalationDuration = TimeSpan.Zero, ApprovalStages = [new() { RoleId = role, Order = 1, Required = true }] };
    private static Approval Legacy() => new() { Id = Guid.NewGuid(), ItemType = "Stock", ApprovalStages = new[] { false, true, false }.Select(required => new ApprovalStage { Id = Guid.NewGuid(), Required = required, Order = 0 }).ToList() };
    private static ApplicationDbContext Context(Guid actor) => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new Actor(actor));
    private static ApprovalRepository Repository(ApplicationDbContext context) => new(context, new MapperConfiguration(mapping => {
        mapping.CreateMap<CreateApprovalRequest, Approval>(); mapping.CreateMap<CreateApprovalStageRequest, ApprovalStage>();
    }).CreateMapper(), null!, null!, NullLogger<ApprovalRepository>.Instance, null!, new NoOpProductionActivityStepEventPublisher());
    private sealed class Actor(Guid id) : ICurrentUserService
    {
        public Guid? UserId => id;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }
}
