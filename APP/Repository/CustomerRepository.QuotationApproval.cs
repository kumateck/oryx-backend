using System.Data;
using DOMAIN.Entities.Customers;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class CustomerRepository
{
    public async Task<Result> SendQuotation(Guid quotationId, Guid userId)
    {
        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        var quotation = await context.CustomerQuotations.Include(item => item.Approvals)
            .FirstOrDefaultAsync(item => item.Id == quotationId);
        if (quotation is null) return Error.NotFound("CustomerQuotation.NotFound", "Quotation not found.");
        if (quotation.Status != CustomerQuotationStatus.Draft)
            return Error.Conflict("CustomerQuotation.Status", "Only a draft quotation can be sent.");
        if (quotation.ValidUntil < DateTime.UtcNow)
            return Error.Validation("CustomerQuotation.Expired", "An expired quotation cannot be sent.");
        if (await context.Approvals.CountAsync(item =>
                item.ItemType == nameof(CustomerQuotation)) > 1)
            return Error.Conflict(
                "CustomerQuotation.ApprovalAmbiguous",
                "Multiple quotation approval configurations exist.");

        quotation.Status = CustomerQuotationStatus.Sent;
        quotation.UpdatedAt = DateTime.UtcNow;
        quotation.LastUpdatedById = userId;
        await context.SaveChangesAsync();
        await approvalRepository.CreateInitialApprovalsAsync(nameof(CustomerQuotation), quotation.Id);
        if (transaction is not null) await transaction.CommitAsync();
        return Result.Success();
    }
}
