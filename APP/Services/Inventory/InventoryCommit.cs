using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.Inventory;

public static class InventoryCommit
{
    public static async Task<Result> Save(ApplicationDbContext context)
    {
        try { await context.SaveChangesAsync(); return Result.Success(); }
        catch (DbUpdateConcurrencyException)
        {
            context.ChangeTracker.Clear();
            return Error.Conflict("Inventory.Changed", "The receipt or inventory changed during this operation. Review current records before another attempt.");
        }
    }
}
