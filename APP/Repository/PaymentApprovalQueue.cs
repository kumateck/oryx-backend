using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Payments;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Repository;

internal static class PaymentApprovalQueue
{
    internal static Task<List<Payment>> GetAsync(
        ApplicationDbContext context,
        Guid userId,
        IReadOnlyCollection<Guid> roleIds
    ) => context.Payments
        .AsNoTracking()
        .Include(payment => payment.Currency)
        .Include(payment => payment.RecordedBy)
            .ThenInclude(user => user.Department)
        .Where(payment =>
            payment.Status == PaymentStatus.Pending
            && payment.RecordedById != userId
            && payment.Approvals.Any(stage =>
                stage.ActivatedAt.HasValue
                && stage.Status == ApprovalStatus.Pending
                && (stage.UserId == userId
                    || stage.RoleId.HasValue && roleIds.Contains(stage.RoleId.Value))))
        .ToListAsync();
}
