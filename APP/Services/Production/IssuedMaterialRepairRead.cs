using DOMAIN.Entities.Requisitions;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Services.Production;

/// <summary>Overlay corrective documents without changing the signed requisition.</summary>
public static class IssuedMaterialRepairRead
{
    public static async Task Apply(ApplicationDbContext db, RequisitionDto requisition)
    {
        var adjustments = await db.IssuedMaterialRepairLines.AsNoTracking()
            .Where(line => db.IssuedMaterialRepairs.Any(repair => repair.Id == line.IssuedMaterialRepairId && repair.RequisitionId == requisition.Id))
            .GroupBy(line => line.RequisitionItemId).Select(group => new { Id = group.Key, Quantity = group.Sum(line => line.Adjustment) }).ToListAsync();
        foreach (var item in requisition.Items)
        {
            var adjustment = adjustments.SingleOrDefault(line => line.Id == item.Id);
            if (adjustment is null) continue;
            item.ApprovedQuantity = item.Quantity;
            item.Quantity += adjustment.Quantity;
        }
    }
}
