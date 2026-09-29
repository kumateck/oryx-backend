using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Services.Formulas;

public static class FormulaApprovalAssignment
{
    public static async Task<bool> AllowsAsync(ApplicationDbContext context,
        Guid actorId, bool approve, CancellationToken cancellationToken)
    {
        var stages = await context.ApprovalStages.AsNoTracking()
            .Where(item => item.Approval.ItemType == "FormulaRevision")
            .OrderBy(item => item.Order).ToListAsync(cancellationToken);
        if (stages.Count != 2) return false;
        var stage = approve ? stages[^1] : stages[0];
        if (stage.UserId == actorId) return true;
        if (!stage.RoleId.HasValue) return false;
        return await context.UserRoles.IgnoreQueryFilters().AsNoTracking()
            .AnyAsync(item => item.UserId == actorId && item.RoleId == stage.RoleId.Value,
                cancellationToken);
    }
}
