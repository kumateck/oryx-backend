using System.Data;
using APP.IRepository;
using AutoMapper;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Payments;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class PaymentRepository(ApplicationDbContext context, IMapper mapper) : IPaymentRepository
{
    public async Task<Result<Guid>> RecordPayment(RecordPaymentRequest request, Guid userId)
    {
        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;

        if (request.Amount <= 0)
            return Error.Validation("Payment.Amount", "Payment amount must be greater than zero.");

        var reference = request.Reference?.Trim();
        if (string.IsNullOrWhiteSpace(reference))
            return Error.Validation("Payment.Reference", "Payment reference is required.");

        if (!await context.Currencies.AnyAsync(currency => currency.Id == request.CurrencyId))
            return Error.NotFound("Payment.Currency", "Currency not found.");

        if (await context.Payments.AnyAsync(payment =>
                payment.PayableType == request.PayableType
                && payment.PayableId == request.PayableId
                && payment.Reference == reference))
            return Error.Conflict("Payment.Duplicate", "This payment reference is already recorded for the payable.");

        var totalsResult = await GetPayableTotals(request.PayableType, request.PayableId);
        if (!totalsResult.IsSuccess)
            return totalsResult.Error;

        var balances = await PaymentBalanceQuery.GetAsync(
            context,
            request.PayableType,
            request.PayableId,
            totalsResult.Value
        );
        var balance = balances.SingleOrDefault(item => item.CurrencyId == request.CurrencyId);
        if (balance is null)
            return Error.Validation("Payment.CurrencyMismatch", "Payment currency must match a currency on the payable.");
        if (request.Amount > balance.OutstandingBalance)
            return Error.Validation("Payment.Overpayment", "Payment exceeds the outstanding balance.");

        var approval = await context
            .Approvals.AsNoTracking()
            .Include(item => item.ApprovalStages)
            .FirstOrDefaultAsync(item => item.ItemType == nameof(Payment));
        var configuredStages = approval?.ApprovalStages.OrderBy(stage => stage.Order).ToList() ?? [];
        // A missing/empty Payment approval workflow must not block recording a
        // payment entirely - that would mean nobody can record any payment until
        // an admin configures one. Auto-approve instead, same as every other
        // approval document with no configured workflow.
        var isAutoApproved = approval is null || configuredStages.Count == 0;

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = request.Amount,
            CurrencyId = request.CurrencyId,
            PaymentDate = request.PaymentDate,
            Method = request.Method,
            Reference = reference,
            Notes = request.Notes,
            RecordedById = userId,
            CreatedById = userId,
            PayableType = request.PayableType,
            PayableId = request.PayableId,
            Status = isAutoApproved ? PaymentStatus.Approved : PaymentStatus.Pending,
            Approved = isAutoApproved,
            Approvals = isAutoApproved
                ? []
                : configuredStages
                    .Select(stage => new PaymentApproval
                    {
                        Id = Guid.NewGuid(),
                        ApprovalId = approval!.Id,
                        Required = stage.Required,
                        Order = stage.Order,
                        UserId = stage.UserId,
                        RoleId = stage.RoleId,
                        ActivatedAt = stage.Order == configuredStages.Min(x => x.Order)
                            ? DateTime.UtcNow
                            : null,
                    })
                    .ToList(),
        };

        await context.Payments.AddAsync(payment);

        if (isAutoApproved)
        {
            var reason = approval is null
                ? "System auto-approved because no Payment approval workflow is configured."
                : "System auto-approved because the Payment approval workflow has no stages.";
            await context.ApprovalActionLogs.AddAsync(new ApprovalActionLog
            {
                ModelId = payment.Id,
                Status = ApprovalStatus.Approved,
                Comments = reason,
            });
        }

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Error.Conflict("Payment.Duplicate", "The payment was already recorded.");
        }

        if (transaction is not null)
            await transaction.CommitAsync();
        return payment.Id;
    }

    public async Task<Result> ReviewPayment(
        Guid paymentId,
        ReviewPaymentRequest request,
        Guid userId,
        List<Guid> roleIds
    )
    {
        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;

        if (request.Status is not (ApprovalStatus.Approved or ApprovalStatus.Rejected))
            return Error.Validation("Payment.ReviewStatus", "Review must approve or reject the payment.");

        var payment = await context
            .Payments.AsSplitQuery()
            .Include(item => item.Approvals)
            .FirstOrDefaultAsync(item => item.Id == paymentId);
        if (payment is null)
            return Error.NotFound("Payment.NotFound", "Payment not found.");
        if (payment.Status != PaymentStatus.Pending)
            return Error.Validation("Payment.AlreadyReviewed", "Payment is no longer pending review.");
        if (payment.RecordedById == userId)
            return Error.Validation("Payment.MakerChecker", "The recorder cannot approve or reject their own payment.");

        var current = payment.Approvals
            .Where(stage => stage.Status == ApprovalStatus.Pending && stage.ActivatedAt.HasValue)
            .OrderBy(stage => stage.Order)
            .FirstOrDefault();
        if (current is null)
            return Error.Validation("Payment.ApprovalStage", "No active payment approval stage was found.");
        if (current.UserId != userId && (!current.RoleId.HasValue || !roleIds.Contains(current.RoleId.Value)))
            return Error.Validation("Payment.Approver", "The current user is not assigned to this approval stage.");

        if (request.Status == ApprovalStatus.Approved)
        {
            var totalsResult = await GetPayableTotals(payment.PayableType, payment.PayableId);
            if (!totalsResult.IsSuccess)
                return totalsResult.Error;
            var balances = await PaymentBalanceQuery.GetAsync(context, payment.PayableType, payment.PayableId, totalsResult.Value);
            var balance = balances.SingleOrDefault(item => item.CurrencyId == payment.CurrencyId);
            if (balance is null || payment.Amount > balance.OutstandingBalance)
                return Error.Validation("Payment.Overpayment", "Outstanding balance changed; approving this payment would overpay the payable.");
        }

        current.Status = request.Status;
        current.ApprovalTime = DateTime.UtcNow;
        current.ApprovedById = userId;
        current.Comments = request.Comments;

        if (request.Status == ApprovalStatus.Rejected)
        {
            payment.Status = PaymentStatus.Rejected;
        }
        else
        {
            var next = payment.Approvals
                .Where(stage => stage.Status == ApprovalStatus.Pending && stage.Order > current.Order)
                .OrderBy(stage => stage.Order)
                .FirstOrDefault();
            if (next is null)
            {
                payment.Approved = true;
                payment.Status = PaymentStatus.Approved;
            }
            else
            {
                next.ActivatedAt = DateTime.UtcNow;
            }
        }

        await context.SaveChangesAsync();
        if (transaction is not null)
            await transaction.CommitAsync();
        return Result.Success();
    }

}
