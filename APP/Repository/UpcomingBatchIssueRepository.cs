using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.Products.Production;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

/// <summary>
/// Batches whose production has started (the BMR exists and their materials are reserved) but whose BMR is not issued yet.
/// The warehouse prepares the reserved lots before the requisition arrives. A reader in a department sees the batches with
/// stock reserved in that department's stores, and only those reservations.
/// </summary>
public sealed class UpcomingBatchIssueRepository(ApplicationDbContext context)
{
    public async Task<Result<UpcomingBatchIssuePage>> List(Guid actor, int page, int pageSize, string searchQuery)
    {
        var permissions = await CurrentPermissionSet.Load(context, actor);
        if (!permissions.Contains(PermissionKeys.CanViewUpcomingBatchIssues))
            return Error.Forbidden("UpcomingBatchIssues.Permission", "Your current permissions do not allow viewing batches about to be issued.");
        if (page < 1 || pageSize < 1 || pageSize > 100) return Error.Validation("UpcomingBatchIssues.Page", "Choose a valid page size between 1 and 100.");
        var account = await context.Users.IgnoreQueryFilters().AsNoTracking().Where(user => user.Id == actor && user.DeletedAt == null && !user.IsDisabled)
            .Select(user => new { user.DepartmentId }).SingleOrDefaultAsync();
        if (account is null) return Error.Forbidden("UpcomingBatchIssues.Account", "An active account is required.");
        var department = account.DepartmentId;

        // The product and warehouse filters scope to the signed-in user's own department, which would hide every production
        // department's batch from the warehouse; this report applies its own scope and deletion checks instead.
        var reservations = context.MaterialBatchReservedQuantities.IgnoreQueryFilters().AsNoTracking()
            .Where(hold => hold.DeletedAt == null && hold.ReleasedAt == null && hold.Quantity > 0 && hold.MaterialBatch.DeletedAt == null
                && hold.Warehouse.DeletedAt == null && (department == null || hold.Warehouse.DepartmentId == department));
        var records = context.BatchManufacturingRecords.IgnoreQueryFilters().AsNoTracking()
            .Where(record => record.DeletedAt == null && record.IssuedDate == null && !record.ProductionScheduleProduct.Cancelled
                && record.ProductionScheduleProduct.Product.DeletedAt == null
                && record.ProductionScheduleProduct.ProductionSchedule.DeletedAt == null
                && reservations.Any(hold => hold.ProductionScheduleProductId == record.ProductionScheduleProductId));
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var search = searchQuery.Trim().ToUpper();
            records = records.Where(record => record.ProductionScheduleProduct.Product.Name.ToUpper().Contains(search)
                || record.ProductionScheduleProduct.Product.Code.ToUpper().Contains(search)
                || record.ProductionScheduleProduct.ProductionSchedule.Code.ToUpper().Contains(search)
                || (record.BatchNumber != null && record.BatchNumber.ToUpper().Contains(search)));
        }

        var total = await records.CountAsync();
        // Oldest start first: the batch that has waited longest is the next to be issued.
        var batches = await records
            .OrderBy(record => record.ProductionScheduleProduct.ProductionActivity.StartedAt).ThenBy(record => record.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(record => new
            {
                record.Id, record.ProductionScheduleProductId, record.BatchNumber, record.BatchQuantity,
                ProductCode = record.ProductionScheduleProduct.Product.Code, ProductName = record.ProductionScheduleProduct.Product.Name,
                Unit = record.ProductionScheduleProduct.Product.BaseUoM.Symbol,
                ScheduleCode = record.ProductionScheduleProduct.ProductionSchedule.Code,
                StartedAt = (DateTime?)record.ProductionScheduleProduct.ProductionActivity.StartedAt ?? record.CreatedAt,
            }).ToListAsync();
        var ids = batches.Select(batch => batch.ProductionScheduleProductId).ToList();
        var materials = await reservations.Where(hold => ids.Contains(hold.ProductionScheduleProductId))
            .Select(hold => new
            {
                hold.ProductionScheduleProductId, hold.MaterialBatch.MaterialId, MaterialCode = hold.MaterialBatch.Material.Code,
                MaterialName = hold.MaterialBatch.Material.Name, hold.MaterialBatch.Material.Kind, LotNumber = hold.MaterialBatch.BatchNumber,
                Warehouse = hold.Warehouse.Name, Shelf = hold.WarehouseLocationShelf.Name, Rack = hold.WarehouseLocationShelf.WarehouseLocationRack.Name,
                Location = hold.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.Name, hold.Quantity, Unit = hold.UoM.Symbol,
            }).ToListAsync();

        var data = batches.Select(batch => new UpcomingBatchIssue(batch.Id, batch.ProductionScheduleProductId, batch.ProductCode, batch.ProductName,
            batch.ScheduleCode, batch.StartedAt, batch.BatchNumber, batch.BatchQuantity, batch.Unit,
            materials.Where(item => item.ProductionScheduleProductId == batch.ProductionScheduleProductId)
                .OrderBy(item => item.Kind).ThenBy(item => item.MaterialCode).ThenBy(item => item.LotNumber)
                .Select(item => new UpcomingBatchMaterial(item.MaterialId, item.MaterialCode, item.MaterialName, item.Kind, item.LotNumber, item.Warehouse,
                    string.Join(" · ", new[] { item.Location, item.Rack, item.Shelf }.Where(part => !string.IsNullOrWhiteSpace(part))), item.Quantity, item.Unit))
                .ToList())).ToList();
        return new UpcomingBatchIssuePage(data, total, page, (int)Math.Ceiling(total / (double)pageSize));
    }
}
