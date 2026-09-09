using APP.Repository;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Payments;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.PurchaseOrders.Request;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class BillingPaymentCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public class BillingSheetChargePaymentTests
{
    [Fact]
    public async Task MarkChargeAsPaid_CreatesPaymentAndUpdatesCharge()
    {
        await using var context = CreateContext();
        var seeded = await SeedBillingSheet(context, 35m);
        var userId = Guid.NewGuid();

        var result = await CreateRepository(context).MarkBillingSheetChargeAsPaid(
            CreateRequest((seeded.ChargeIds[0], " BANK-35 ")),
            userId
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(seeded.ChargeIds, result.Value.PaidChargeIds);
        var payment = Assert.Single(context.Payments);
        Assert.Equal(35m, payment.Amount);
        Assert.Equal(seeded.CurrencyId, payment.CurrencyId);
        Assert.Equal(seeded.BillingSheetId, payment.PayableId);
        Assert.Equal(PayableType.BillingSheet, payment.PayableType);
        Assert.Equal(seeded.ChargeIds[0], payment.BillingSheetChargeId);
        Assert.Equal("BANK-35", payment.Reference);
        var charge = await context.BillingSheetCharges.FindAsync(seeded.ChargeIds[0]);
        Assert.True(charge!.Paid);
        Assert.Equal(userId, charge.LastUpdatedById);
    }

    [Fact]
    public async Task MarkChargeAsPaid_KeepsChargeUnpaidUntilFinalApproval()
    {
        await using var context = CreateContext();
        var seeded = await SeedBillingSheet(context, 45m);
        var recorderId = Guid.NewGuid();
        var approverId = Guid.NewGuid();
        context.Approvals.Add(new Approval
        {
            Id = Guid.NewGuid(),
            ItemType = nameof(Payment),
            ApprovalStages =
            [
                new ApprovalStage
                {
                    Id = Guid.NewGuid(),
                    Order = 1,
                    Required = true,
                    UserId = approverId,
                },
            ],
        });
        await context.SaveChangesAsync();
        var repository = CreateRepository(context);

        var recorded = await repository.MarkBillingSheetChargeAsPaid(
            CreateRequest((seeded.ChargeIds[0], "BANK-PENDING")),
            recorderId
        );

        Assert.True(recorded.IsSuccess);
        Assert.Empty(recorded.Value.PaidChargeIds);
        Assert.Equal(seeded.ChargeIds, recorded.Value.PendingChargeIds);
        var payment = Assert.Single(context.Payments);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.False((await context.BillingSheetCharges.FindAsync(seeded.ChargeIds[0]))!.Paid);

        var reviewed = await new PaymentRepository(context, null!).ReviewPayment(
            payment.Id,
            new ReviewPaymentRequest { Status = ApprovalStatus.Approved },
            approverId,
            []
        );

        Assert.True(reviewed.IsSuccess);
        Assert.True((await context.BillingSheetCharges.FindAsync(seeded.ChargeIds[0]))!.Paid);
    }

    [Fact]
    public async Task MarkChargeAsPaid_RejectsPendingBillingSheet()
    {
        await using var context = CreateContext();
        var seeded = await SeedBillingSheet(context, 45m);
        var billingSheet = await context.BillingSheets.FindAsync(seeded.BillingSheetId);
        billingSheet!.Approved = false;
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).MarkBillingSheetChargeAsPaid(
            CreateRequest((seeded.ChargeIds[0], "BANK-BLOCKED")),
            Guid.NewGuid()
        );

        Assert.True(result.IsFailure);
        Assert.Equal("Approval.Required", result.Error.Code);
        Assert.Empty(context.Payments);
        Assert.False((await context.BillingSheetCharges.FindAsync(seeded.ChargeIds[0]))!.Paid);
    }

    [Fact]
    public async Task MarkChargeAsPaid_RejectsBatchWhenAnyChargeIsMissing()
    {
        await using var context = CreateContext();
        var seeded = await SeedBillingSheet(context, 20m);

        var result = await CreateRepository(context).MarkBillingSheetChargeAsPaid(
            CreateRequest((seeded.ChargeIds[0], "BANK-20"), (Guid.NewGuid(), "BANK-21")),
            Guid.NewGuid()
        );

        Assert.False(result.IsSuccess);
        Assert.Empty(context.Payments);
        Assert.False((await context.BillingSheetCharges.FindAsync(seeded.ChargeIds[0]))!.Paid);
    }

    [Fact]
    public async Task MarkChargeAsPaid_RequiresUniqueReferencesBeforeWriting()
    {
        await using var context = CreateContext();
        var seeded = await SeedBillingSheet(context, 10m, 15m);

        var result = await CreateRepository(context).MarkBillingSheetChargeAsPaid(
            CreateRequest(
                (seeded.ChargeIds[0], "BANK-DUPLICATE"),
                (seeded.ChargeIds[1], "bank-duplicate")
            ),
            Guid.NewGuid()
        );

        Assert.False(result.IsSuccess);
        Assert.Empty(context.Payments);
        Assert.All(context.BillingSheetCharges, charge => Assert.False(charge.Paid));
    }

    private static MarkBillingSheetChargePaymentsRequest CreateRequest(
        params (Guid Id, string Reference)[] charges
    ) => new()
    {
        PaymentDate = new DateTime(2026, 9, 3, 0, 0, 0, DateTimeKind.Utc),
        Method = PaymentMethod.BankTransfer,
        Charges =
        [
            .. charges.Select(charge => new MarkBillingSheetChargePayment
            {
                BillingSheetChargeId = charge.Id,
                Reference = charge.Reference,
            }),
        ],
    };

    private static async Task<SeededBillingSheet> SeedBillingSheet(
        ApplicationDbContext context,
        params decimal[] amounts
    )
    {
        var currencyId = Guid.NewGuid();
        var billingSheetId = Guid.NewGuid();
        var chargeIds = amounts.Select(_ => Guid.NewGuid()).ToList();
        context.Currencies.Add(new Currency { Id = currencyId, Name = "Cedi", Symbol = "GH₵" });
        context.BillingSheets.Add(new BillingSheet
        {
            Id = billingSheetId,
            Code = "BS-PAYMENT",
            InvoiceId = Guid.NewGuid(),
            Approved = true,
            Charges =
            [
                .. amounts.Select((amount, index) => new BillingSheetCharge
                {
                    Id = chargeIds[index],
                    CurrencyId = currencyId,
                    Amount = amount,
                }),
            ],
        });
        await context.SaveChangesAsync();
        return new SeededBillingSheet(billingSheetId, currencyId, chargeIds);
    }

    private static ProcurementRepository CreateRepository(ApplicationDbContext context) =>
        new(context, null!, null!, null!, null!, null!, new PaymentRepository(context, null!));

    private static ApplicationDbContext CreateContext() =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            new BillingPaymentCurrentUserService()
        );

    private sealed record SeededBillingSheet(
        Guid BillingSheetId,
        Guid CurrencyId,
        List<Guid> ChargeIds
    );
}
