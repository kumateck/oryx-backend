using APP.Repository;
using APP.Utils;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

/// <summary>Approval rights go only to an active person who can open the pending approvals page.</summary>
public class ApprovalTransferRecipientTests
{
    private static readonly Guid Leaver = Guid.NewGuid(), Approver = Guid.NewGuid(), Clerk = Guid.NewGuid(), Disabled = Guid.NewGuid();

    private static ApplicationDbContext Context()
    {
        var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new TestCurrentUser());
        context.Users.AddRange(
            new User { Id = Leaver, FirstName = "Ama", Email = "ama@test" },
            new User { Id = Approver, FirstName = "Kofi", Email = "kofi@test" },
            new User { Id = Clerk, FirstName = "Yaw", Email = "yaw@test" },
            new User { Id = Disabled, FirstName = "Old", Email = "old@test", IsDisabled = true });
        var role = new Role { Id = Guid.NewGuid(), Name = "approvers", NormalizedName = "APPROVERS", DisplayName = "Approvers" };
        context.Roles.Add(role);
        var claim = new IdentityRoleClaim<Guid> { Id = 7101, RoleId = role.Id, ClaimType = AppConstants.Permission, ClaimValue = PermissionKeys.CanViewPendingApprovals };
        context.RoleClaims.Add(claim);
        context.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), Key = PermissionKeys.CanViewPendingApprovals, RoleClaimId = claim.Id, Type = "Access" });
        foreach (var user in new[] { Approver, Disabled })
            context.UserRoles.Add(new IdentityUserRole<Guid> { UserId = user, RoleId = role.Id });
        context.SaveChanges();
        return context;
    }

    [Fact]
    public async Task Refuses_a_receiver_who_could_not_decide_the_approvals()
    {
        await using var context = Context();
        var repository = new ApprovalRepository(context, null!, null!, null!, NullLogger<ApprovalRepository>.Instance, null!, new NoOpProductionActivityStepEventPublisher());
        async Task<string?> Transfer(Guid to) => (await repository.TransferApprovalRights(new TransferApprovalRequest { FromUserId = Leaver, ToUserId = to })).Error?.Code;
        Assert.Equal("ApprovalTransfer.Self", await Transfer(Leaver));
        Assert.Equal("ApprovalTransfer.ReceiverNotFound", await Transfer(Guid.NewGuid()));
        Assert.Equal("ApprovalTransfer.ReceiverDisabled", await Transfer(Disabled));
        Assert.Equal("ApprovalTransfer.ReceiverCannotApprove", await Transfer(Clerk));
        // Nothing moved for any refused transfer.
        Assert.False(await context.ApprovalStages.AnyAsync(stage => stage.UserId == Clerk));
    }

    private sealed class TestCurrentUser : ICurrentUserService
    {
        public Guid? UserId => Guid.Empty;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }
}
