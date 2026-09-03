using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Payments;
using DOMAIN.Entities.PurchaseOrders;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class PaymentApprovalCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public class PaymentApprovalTests
{
    [Fact]
    public async Task ReviewPayment_RejectsRecorderAsApprover()
    {
        await using var context = CreateContext();
        var recorderId = Guid.NewGuid();
        var payment = CreatePayment(Guid.NewGuid(), Guid.NewGuid(), recorderId, recorderId, 10m);
        context.Payments.Add(payment);
        await context.SaveChangesAsync();
        var repository = new PaymentRepository(context, CreateMapper());

        var result = await repository.ReviewPayment(
            payment.Id,
            new ReviewPaymentRequest { Status = ApprovalStatus.Approved },
            recorderId,
            []
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.False(payment.Approved);
    }

    [Fact]
    public async Task ReviewPayment_FinalApprovalMakesPaymentFinanciallyEffective()
    {
        await using var context = CreateContext();
        var currencyId = Guid.NewGuid();
        var payableId = Guid.NewGuid();
        var approverId = Guid.NewGuid();
        var payment = CreatePayment(currencyId, payableId, Guid.NewGuid(), approverId, 30m);
        context.AddRange(
            new Currency { Id = currencyId, Name = "US Dollar", Symbol = "$" },
            new BillingSheet
            {
                Id = payableId, Code = "BS-APPROVAL", InvoiceId = Guid.NewGuid(),
                SupplierId = null,
                Charges = [new BillingSheetCharge { Id = Guid.NewGuid(), CurrencyId = currencyId, Amount = 100m }],
            },
            payment
        );
        await context.SaveChangesAsync();
        var repository = new PaymentRepository(context, CreateMapper());

        var result = await repository.ReviewPayment(
            payment.Id,
            new ReviewPaymentRequest { Status = ApprovalStatus.Approved },
            approverId,
            []
        );
        var balance = Assert.Single(await PaymentBalanceQuery.GetAsync(
            context, PayableType.BillingSheet, payableId,
            [new PaymentBalanceQuery.Total(currencyId, "US Dollar", "$", 100m)]));

        Assert.True(result.IsSuccess);
        Assert.True(payment.Approved);
        Assert.Equal(PaymentStatus.Approved, payment.Status);
        Assert.Equal(30m, balance.AmountPaid);
        Assert.Equal(70m, balance.OutstandingBalance);
    }

    [Fact]
    public async Task RecordPayment_AutoApproves_WhenNoWorkflowConfigured()
    {
        // Regression: RecordPayment used to hard-block with "Payment.ApprovalWorkflowMissing"
        // whenever no Payment approval workflow existed - meaning nobody could ever
        // record a payment until an admin configured one. It must auto-approve instead.
        await using var context = CreateContext();
        var currencyId = Guid.NewGuid();
        var payableId = Guid.NewGuid();
        context.AddRange(
            new Currency { Id = currencyId, Name = "US Dollar", Symbol = "$" },
            new BillingSheet
            {
                Id = payableId, Code = "BS-AUTO", InvoiceId = Guid.NewGuid(),
                SupplierId = null,
                Charges = [new BillingSheetCharge { Id = Guid.NewGuid(), CurrencyId = currencyId, Amount = 100m }],
            }
        );
        await context.SaveChangesAsync();
        var repository = new PaymentRepository(context, CreateMapper());

        var result = await repository.RecordPayment(
            new RecordPaymentRequest
            {
                Amount = 40m, CurrencyId = currencyId, PaymentDate = DateTime.UtcNow,
                Method = PaymentMethod.BankTransfer, Reference = "BANK-AUTO",
                PayableType = PayableType.BillingSheet, PayableId = payableId,
            },
            Guid.NewGuid()
        );

        Assert.True(result.IsSuccess);
        var payment = await context.Payments.FindAsync(result.Value);
        Assert.True(payment!.Approved);
        Assert.Equal(PaymentStatus.Approved, payment.Status);
        Assert.Empty(payment.Approvals);
        Assert.Single(context.ApprovalActionLogs);
    }

    [Fact]
    public async Task RecordPayment_CreatesConfiguredPendingApproval_WhenWorkflowExists()
    {
        await using var context = CreateContext();
        var currencyId = Guid.NewGuid();
        var payableId = Guid.NewGuid();
        var approverId = Guid.NewGuid();
        var approval = new Approval { Id = Guid.NewGuid(), ItemType = nameof(Payment), ApprovalStages = [] };
        approval.ApprovalStages.Add(new ApprovalStage
        {
            Id = Guid.NewGuid(), ApprovalId = approval.Id, Approval = approval,
            Order = 1, Required = true, UserId = approverId,
        });
        context.AddRange(
            approval,
            new Currency { Id = currencyId, Name = "US Dollar", Symbol = "$" },
            new BillingSheet
            {
                Id = payableId, Code = "BS-CONFIGURED", InvoiceId = Guid.NewGuid(),
                SupplierId = null,
                Charges = [new BillingSheetCharge { Id = Guid.NewGuid(), CurrencyId = currencyId, Amount = 100m }],
            }
        );
        await context.SaveChangesAsync();
        var repository = new PaymentRepository(context, CreateMapper());

        var result = await repository.RecordPayment(
            new RecordPaymentRequest
            {
                Amount = 25m, CurrencyId = currencyId, PaymentDate = DateTime.UtcNow,
                Method = PaymentMethod.Cash, Reference = "BANK-CONFIGURED",
                PayableType = PayableType.BillingSheet, PayableId = payableId,
            },
            Guid.NewGuid()
        );

        Assert.True(result.IsSuccess);
        var payment = await context.Payments.FindAsync(result.Value);
        Assert.False(payment!.Approved);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        var stage = Assert.Single(payment.Approvals);
        Assert.Equal(approverId, stage.UserId);
        Assert.Empty(context.ApprovalActionLogs);
    }

    private static Payment CreatePayment(
        Guid currencyId,
        Guid payableId,
        Guid recorderId,
        Guid approverId,
        decimal amount
    ) => new()
    {
        Id = Guid.NewGuid(), Amount = amount, CurrencyId = currencyId,
        PayableType = PayableType.BillingSheet, PayableId = payableId,
        RecordedById = recorderId, Reference = $"BANK-{Guid.NewGuid()}", Status = PaymentStatus.Pending,
        Approvals =
        [
            new PaymentApproval
            {
                Id = Guid.NewGuid(), ApprovalId = Guid.NewGuid(), UserId = approverId,
                Order = 1, Required = true, ActivatedAt = DateTime.UtcNow,
            },
        ],
    };

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new ApplicationDbContext(options, new PaymentApprovalCurrentUserService());
    }

    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(
            cfg => cfg.CreateMap<Currency, CurrencyDto>(), NullLoggerFactory.Instance);
        return config.CreateMapper();
    }
}
