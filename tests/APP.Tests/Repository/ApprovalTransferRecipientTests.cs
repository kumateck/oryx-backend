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
    private static readonly Guid Leaver = Guid.NewGuid(), Approver = Guid.NewGuid(), Clerk = Guid.NewGuid(), Disabled = Guid.NewGuid(), It = Guid.NewGuid();
    private static int nextClaim = 7200;

    private static ApplicationDbContext Context()
    {
        var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)).Options, new TestCurrentUser());
        context.Users.AddRange(
            new User { Id = Leaver, FirstName = "Ama", Email = "ama@test" },
            new User { Id = Approver, FirstName = "Kofi", Email = "kofi@test" },
            new User { Id = Clerk, FirstName = "Yaw", Email = "yaw@test" },
            new User { Id = Disabled, FirstName = "Old", Email = "old@test", IsDisabled = true },
            new User { Id = It, FirstName = "Esi", Email = "esi@test" });
        var role = new Role { Id = Guid.NewGuid(), Name = "approvers", NormalizedName = "APPROVERS", DisplayName = "Approvers" };
        context.Roles.Add(role);
        var claim = new IdentityRoleClaim<Guid> { Id = 7101, RoleId = role.Id, ClaimType = AppConstants.Permission, ClaimValue = PermissionKeys.CanViewPendingApprovals };
        context.RoleClaims.Add(claim);
        context.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), Key = PermissionKeys.CanViewPendingApprovals, RoleClaimId = claim.Id, Type = "Access" });
        foreach (var user in new[] { Approver, Disabled })
            context.UserRoles.Add(new IdentityUserRole<Guid> { UserId = user, RoleId = role.Id });
        context.SaveChanges();
        Grant(context, It, PermissionKeys.CanTransferAnyApprovalRights);
        Grant(context, Approver, PermissionKeys.CanTransferApprovalRights);
        return context;
    }

    private static void Grant(ApplicationDbContext context, Guid user, string key)
    {
        var role = new Role { Id = Guid.NewGuid(), Name = key, NormalizedName = key.ToUpperInvariant(), DisplayName = key };
        context.Roles.Add(role);
        var claim = new IdentityRoleClaim<Guid> { Id = ++nextClaim, RoleId = role.Id, ClaimType = AppConstants.Permission, ClaimValue = key };
        context.RoleClaims.Add(claim);
        context.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), Key = key, RoleClaimId = claim.Id, Type = "Access" });
        context.UserRoles.Add(new IdentityUserRole<Guid> { UserId = user, RoleId = role.Id });
        context.SaveChanges();
    }

    private static ApprovalRepository Repository(ApplicationDbContext context) =>
        new(context, null!, null!, null!, NullLogger<ApprovalRepository>.Instance, null!, new NoOpProductionActivityStepEventPublisher());

    [Fact]
    public async Task Refuses_a_receiver_who_could_not_decide_the_approvals()
    {
        await using var context = Context();
        var repository = Repository(context);
        async Task<string?> Transfer(Guid to) => (await repository.TransferApprovalRights(new TransferApprovalRequest { FromUserId = Leaver, ToUserId = to }, It)).Error?.Code;
        Assert.Equal("ApprovalTransfer.Self", await Transfer(Leaver));
        Assert.Equal("ApprovalTransfer.ReceiverNotFound", await Transfer(Guid.NewGuid()));
        Assert.Equal("ApprovalTransfer.ReceiverDisabled", await Transfer(Disabled));
        Assert.Equal("ApprovalTransfer.ReceiverCannotApprove", await Transfer(Clerk));
        // Nothing moved for any refused transfer.
        Assert.False(await context.ApprovalStages.AnyAsync(stage => stage.UserId == Clerk));
    }

    [Fact]
    public async Task A_person_hands_over_only_their_own_approvals_and_IT_hands_over_anyones()
    {
        await using var context = Context();
        var approval = new Approval { Id = Guid.NewGuid() };
        context.Approvals.Add(approval);
        context.ApprovalStages.AddRange(new ApprovalStage { Id = Guid.NewGuid(), ApprovalId = approval.Id, UserId = Leaver, Order = 1 },
            new ApprovalStage { Id = Guid.NewGuid(), ApprovalId = approval.Id, UserId = Approver, Order = 2 });
        await context.SaveChangesAsync();
        var repository = Repository(context);

        // A QA user may not hand over someone else's approvals.
        var refused = await repository.TransferApprovalRights(new TransferApprovalRequest { FromUserId = Leaver, ToUserId = Approver }, Approver);
        Assert.Equal("ApprovalTransfer.OwnOnly", refused.Error?.Code);
        Assert.True(await context.ApprovalStages.AnyAsync(stage => stage.UserId == Leaver));

        // IT lists everyone on an approval document, even with nothing waiting, and hands over the leaver's approvals.
        var onRecord = await repository.GetUsersOnApprovalRecord();
        Assert.Equal(new[] { Leaver, Approver }.Order(), onRecord.Select(user => user.Id).Order());
        Assert.True((await repository.TransferApprovalRights(new TransferApprovalRequest { FromUserId = Leaver, ToUserId = Approver }, It)).IsSuccess);
        Assert.False(await context.ApprovalStages.AnyAsync(stage => stage.UserId == Leaver));
    }

    private sealed class TestCurrentUser : ICurrentUserService
    {
        public Guid? UserId => Guid.Empty;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }
}
