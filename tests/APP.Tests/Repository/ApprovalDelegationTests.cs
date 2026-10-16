using System.Reflection;
using APP.IRepository;
using APP.Services.Approvals;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.QcWorksheets;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public class ApprovalDelegationTests
{
    private static readonly DateTime Now = new(2026, 10, 15, 8, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Ama = Guid.NewGuid(), Kofi = Guid.NewGuid(), Esi = Guid.NewGuid(), Blocked = Guid.NewGuid(), QaRole = Guid.NewGuid();

    private static ApplicationDbContext Context()
    {
        var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new TestCurrentUser());
        context.Users.AddRange(
            new User { Id = Ama, FirstName = "Ama", LastName = "Owusu", Email = "ama@test" },
            new User { Id = Kofi, FirstName = "Kofi", LastName = "Boateng", Email = "kofi@test" },
            new User { Id = Esi, FirstName = "Esi", LastName = "Arthur", Email = "esi@test" },
            new User { Id = Blocked, FirstName = "Old", LastName = "Account", Email = "old@test", IsDisabled = true });
        context.UserRoles.Add(new IdentityUserRole<Guid> { UserId = Ama, RoleId = QaRole });
        context.SaveChanges();
        return context;
    }

    private static DelegateApproval Request(Guid to, double startDays, double endDays) =>
        new() { DelegateUserId = to, StartDate = Now.AddDays(startDays), EndDate = Now.AddDays(endDays), Reason = " Annual leave " };

    [Fact]
    public async Task RefusesDelegationsThatCannotBeTrusted()
    {
        await using var context = Context();
        var service = new ApprovalDelegationService(context);
        Assert.Equal("ApprovalDelegation.Self", (await service.Delegate(Ama, Request(Ama, 0, 5), Now)).Error.Code);
        Assert.Equal("ApprovalDelegation.Dates", (await service.Delegate(Ama, Request(Kofi, -5, -1), Now)).Error.Code);
        Assert.Equal("ApprovalDelegation.TooLong", (await service.Delegate(Ama, Request(Kofi, 0, 91), Now)).Error.Code);
        Assert.Equal("ApprovalDelegation.DelegateDisabled", (await service.Delegate(Ama, Request(Blocked, 0, 5), Now)).Error.Code);
        Assert.Equal("ApprovalDelegation.DelegateNotFound", (await service.Delegate(Ama, Request(Guid.NewGuid(), 0, 5), Now)).Error.Code);

        var created = await service.Delegate(Ama, Request(Kofi, -1, 5), Now);
        Assert.True(created.IsSuccess);
        var stored = await context.ApprovalDelegations.SingleAsync();
        // Started in the past means starting now; decisions already taken stay the delegator's.
        Assert.Equal(Now, stored.StartsAt);
        Assert.Equal("Annual leave", stored.Reason);
        Assert.Equal("ApprovalDelegation.Overlap", (await service.Delegate(Ama, Request(Esi, 4, 8), Now)).Error.Code);
        Assert.True((await service.Delegate(Ama, Request(Esi, 5, 8), Now)).IsSuccess);
    }

    [Fact]
    public async Task ActsOnlyWhileActiveWithTheDelegatorsRoles()
    {
        await using var context = Context();
        var service = new ApprovalDelegationService(context);
        var id = (await service.Delegate(Ama, Request(Kofi, 0, 5), Now)).Value;
        await service.Delegate(Esi, Request(Kofi, 2, 5), Now);

        var acting = Assert.Single(await service.ActiveDelegators(Kofi, Now.AddHours(1)));
        Assert.Equal(Ama, acting.UserId);
        Assert.Equal("Ama Owusu", acting.Name);
        Assert.Equal([QaRole], acting.RoleIds);
        Assert.Empty(await service.ActiveDelegators(Ama, Now.AddHours(1)));
        Assert.Empty(await service.ActiveDelegators(Kofi, Now.AddDays(6)));

        // Only the two people involved may end it, and only once.
        Assert.Equal("ApprovalDelegation.NotFound", (await service.Revoke(id, Esi, Now)).Error.Code);
        Assert.True((await service.Revoke(id, Kofi, Now.AddHours(2))).IsSuccess);
        Assert.Equal("ApprovalDelegation.Over", (await service.Revoke(id, Ama, Now.AddHours(3))).Error.Code);
        Assert.Empty(await service.ActiveDelegators(Kofi, Now.AddHours(3)));

        var mine = await service.GetMine(Kofi, Now.AddDays(3));
        Assert.Empty(mine.Given);
        Assert.Equal([ApprovalDelegationStatus.Active, ApprovalDelegationStatus.Revoked], mine.Received.Select(item => item.Status));
    }

    [Fact]
    public async Task ShowsTheDelegatorsQueueExceptSignedQcDocuments()
    {
        await using var context = Context();
        var service = new ApprovalDelegationService(context);
        await service.Delegate(Ama, Request(Kofi, 0, 5), Now);
        var shared = Guid.NewGuid();
        var queues = new Dictionary<Guid, List<ApprovalEntity>>
        {
            [Kofi] = [new ApprovalEntity { Id = shared, ModelType = "PurchaseOrder" }],
            [Ama] = [new ApprovalEntity { Id = shared, ModelType = "PurchaseOrder" }, new ApprovalEntity { Id = Guid.NewGuid(), ModelType = "LeaveRequest" },
                new ApprovalEntity { Id = Guid.NewGuid(), ModelType = QcWorksheetModelTypes.Coa }],
        };
        var pending = await service.Pending(FakeApprovals.For(queues), Kofi, [], null, Now.AddHours(1));
        Assert.Equal(2, pending.Count);
        // A document waiting for both is decided as the caller's own.
        Assert.Null(pending.Single(item => item.Entity.Id == shared).OnBehalfOf);
        var delegated = pending.Single(item => item.Entity.ModelType == "LeaveRequest");
        Assert.Equal(Ama, delegated.OnBehalfOf.UserId);
        Assert.Equal("Ama Owusu", delegated.Entity.OnBehalfOf.Name);
        Assert.Equal("Decided by Kofi Boateng on behalf of Ama Owusu (delegated). Fine", ApprovalDelegationService.OnBehalfComment("Kofi Boateng", delegated.OnBehalfOf, " Fine "));
    }

    /// <summary>Answers only the pending-queue query, from a table of queues per user.</summary>
    public class FakeApprovals : DispatchProxy
    {
        private Dictionary<Guid, List<ApprovalEntity>> queues = [];
        public static IApprovalRepository For(Dictionary<Guid, List<ApprovalEntity>> queues)
        {
            var proxy = Create<IApprovalRepository, FakeApprovals>();
            ((FakeApprovals)(object)proxy).queues = queues;
            return proxy;
        }
        protected override object Invoke(MethodInfo targetMethod, object[] args) =>
            targetMethod.Name == nameof(IApprovalRepository.GetEntitiesRequiringApproval)
                ? Task.FromResult(queues.GetValueOrDefault((Guid)args[0]!, []).Select(item => new ApprovalEntity { Id = item.Id, ModelType = item.ModelType }).ToList())
                : throw new NotSupportedException(targetMethod.Name);
    }

    private sealed class TestCurrentUser : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }
}
