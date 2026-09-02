using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Payments;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class PaymentRepository
{
    public async Task<Result<PaymentDto>> GetPayment(Guid paymentId)
    {
        var payment = await context.Payments.AsNoTracking().AsSplitQuery()
            .Include(item => item.Currency)
            .Include(item => item.RecordedBy)
            .Include(item => item.Approvals)
            .FirstOrDefaultAsync(item => item.Id == paymentId);

        if (payment is null)
            return Error.NotFound("Payment.NotFound", "Payment not found.");

        return new PaymentDto
        {
            Id = payment.Id,
            CreatedAt = payment.CreatedAt,
            Amount = payment.Amount,
            Currency = mapper.Map<CurrencyDto>(payment.Currency),
            PaymentDate = payment.PaymentDate,
            Method = payment.Method,
            Reference = payment.Reference,
            Notes = payment.Notes,
            RecordedBy = mapper.Map<DOMAIN.Entities.Users.UserDto>(payment.RecordedBy),
            PayableType = payment.PayableType,
            PayableId = payment.PayableId,
            Approved = payment.Approved,
            Status = payment.Status,
            Approvals = payment.Approvals.OrderBy(stage => stage.Order).Select(stage =>
                new PaymentApprovalDto
                {
                    Id = stage.Id,
                    Order = stage.Order,
                    Required = stage.Required,
                    Status = stage.Status,
                    ApprovalTime = stage.ApprovalTime,
                    Comments = stage.Comments,
                }).ToList(),
        };
    }
}
