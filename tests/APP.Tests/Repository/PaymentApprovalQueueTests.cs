using APP.Repository;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Payments;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public class PaymentApprovalQueueTests
{
    [Fact]
    public async Task Queue_ReturnsOnlyActiveAssignedPendingPaymentsFromOtherRecorders()
    {
        await using var context = CreateContext();
        var approverId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var currency = new Currency { Id = Guid.NewGuid(), Name = "Cedi" };
        context.Currencies.Add(currency);
        context.Users.Add(new User { Id = approverId });
        context.Roles.Add(new Role { Id = roleId, Name = "Finance approver" });
        AddPayment(context, currency.Id, Guid.NewGuid(), approverId, null, true, "USER-ACTIVE");
        AddPayment(context, currency.Id, Guid.NewGuid(), null, roleId, true, "ROLE-ACTIVE");
        AddPayment(context, currency.Id, Guid.NewGuid(), approverId, null, false, "FUTURE");
        AddPayment(context, currency.Id, approverId, approverId, null, true, "OWN");
        AddPayment(context, currency.Id, Guid.NewGuid(), Guid.NewGuid(), null, true, "OTHER");
        await context.SaveChangesAsync();

        Assert.Equal(5, await context.Payments.CountAsync());
        Assert.Equal(5, await context.PaymentApprovals.IgnoreQueryFilters().CountAsync());
        Assert.Equal(5, await context.PaymentApprovals.CountAsync());
        Assert.Equal(5, await context.Payments.CountAsync(payment => payment.Approvals.Any()));
        Assert.Equal(5, await context.Payments.CountAsync(payment => payment.Status == PaymentStatus.Pending));
        Assert.Equal(4, await context.Payments.CountAsync(payment => payment.RecordedById != approverId));
        Assert.Equal(4, await context.PaymentApprovals.CountAsync(stage => stage.ActivatedAt.HasValue));
        Assert.Equal(3, await context.PaymentApprovals.CountAsync(stage =>
            stage.ActivatedAt.HasValue
            && (stage.UserId == approverId || stage.RoleId == roleId)));

        var results = await PaymentApprovalQueue.GetAsync(context, approverId, [roleId]);

        Assert.Equal(["ROLE-ACTIVE", "USER-ACTIVE"], results.Select(item => item.Reference).Order());
    }

    private static void AddPayment(
        ApplicationDbContext context,
        Guid currencyId,
        Guid recorderId,
        Guid? userId,
        Guid? roleId,
        bool active,
        string reference
    )
    {
        if (context.Users.Local.All(user => user.Id != recorderId))
            context.Users.Add(new User { Id = recorderId });
        if (userId.HasValue && context.Users.Local.All(user => user.Id != userId.Value))
            context.Users.Add(new User { Id = userId.Value });
        var approval = new Approval
        {
            Id = Guid.NewGuid(),
            ItemType = nameof(Payment),
        };
        var payment = new Payment
        {
        Id = Guid.NewGuid(),
        Amount = 10,
        CurrencyId = currencyId,
        PaymentDate = DateTime.UtcNow,
        RecordedById = recorderId,
        Reference = reference,
        Status = PaymentStatus.Pending,
        };
        payment.Approvals.Add(new PaymentApproval
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            Payment = payment,
            ApprovalId = approval.Id,
            Approval = approval,
            Order = 1,
            Required = true,
            UserId = userId,
            RoleId = roleId,
            ActivatedAt = active ? DateTime.UtcNow : null,
        });
        context.AddRange(approval, payment);
    }

    private static ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
        new QueueCurrentUser()
    );

    private sealed class QueueCurrentUser : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }
}
