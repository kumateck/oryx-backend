using APP.IRepository;
using DOMAIN.Entities.Procurement.Suppliers;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SHARED;

namespace APP.Repository;

public partial class SupplierRelationshipRepository(
    ApplicationDbContext context,
    ILogger<SupplierRelationshipRepository> logger
) : ISupplierRelationshipRepository
{
    private async Task<Result<Supplier>> RequireSupplier(Guid supplierId)
    {
        var supplier = await context.Suppliers.FirstOrDefaultAsync(item => item.Id == supplierId);
        return supplier is null
            ? Error.NotFound("Supplier.NotFound", "Supplier not found.")
            : supplier;
    }

    private static Result ValidatePeriod(DateTime start, DateTime end)
        => end.Date < start.Date
            ? Error.Validation("Supplier.Period", "Period end cannot be before period start.")
            : Result.Success();

    private static void SoftDelete(DOMAIN.Entities.Base.BaseEntity entity, Guid userId)
    {
        entity.DeletedAt = DateTime.UtcNow;
        entity.LastDeletedById = userId;
    }
}
