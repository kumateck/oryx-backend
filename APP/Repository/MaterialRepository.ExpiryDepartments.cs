using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Warehouses;
using SHARED;
using Microsoft.EntityFrameworkCore;
using DOMAIN.Entities.Materials;

namespace APP.Repository;

public partial class MaterialRepository
{
    private async Task<Result<Guid?>> ExpiryDepartment(Guid userId)
    {
        // Read the current account, not a client filter or a potentially stale token's department.
        var account = await context.Users.IgnoreQueryFilters().AsNoTracking()
            .Where(user => user.Id == userId && user.DeletedAt == null && !user.IsDisabled)
            .Select(user => new { user.DepartmentId }).SingleOrDefaultAsync();
        return account == null ? Error.Forbidden("Expiry.AccountUnavailable", "An active account is required to read expiry records.")
            : Result.Success<Guid?>(account.DepartmentId);
    }

    private IQueryable<MaterialBatch> ScopeExpiryRecords(IQueryable<MaterialBatch> query, Guid? departmentId, MaterialFilter filter)
    {
        if (!departmentId.HasValue) return query;
        var warehouseIds = filter.WarehouseIds ?? [];
        var unissued = ProductionMaterialAllocator.UnissuedReservations(context);
        return query.Where(batch => batch.ShelfMaterialBatches.Any(item => item.DeletedAt == null && item.Quantity > 0
                && item.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.Warehouse.DeletedAt == null
                && item.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.Warehouse.DepartmentId == departmentId
                && item.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.Warehouse.Department != null
                && item.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.Warehouse.Department.DeletedAt == null
                && (warehouseIds.Count == 0 || warehouseIds.Contains(item.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.WarehouseId)))
            || batch.ReservedQuantities.Any(item => !unissued.Any(hold => hold.Id == item.Id) && item.DeletedAt == null && item.ReleasedAt == null && item.Quantity > 0
                && item.Warehouse.DeletedAt == null && item.Warehouse.DepartmentId == departmentId
                && item.Warehouse.Department != null && item.Warehouse.Department.DeletedAt == null
                && (warehouseIds.Count == 0 || warehouseIds.Contains(item.WarehouseId))));
    }

    private MaterialBatchDto ExpiryReportRow(MaterialBatch batch, Guid? departmentId)
    {
        var dto = mapper.Map<MaterialBatchDto>(batch);
        if (departmentId.HasValue)
        {
            var shelves = batch.ShelfMaterialBatches.Where(item => item.DeletedAt == null && item.Quantity > 0
                && item.WarehouseLocationShelf?.WarehouseLocationRack?.WarehouseLocation?.Warehouse is { DeletedAt: null } warehouse
                && warehouse.DepartmentId == departmentId).Select(item => item.Id).ToHashSet();
            var reserved = batch.ReservedQuantities.Where(item => item.DeletedAt == null && item.ReleasedAt == null && item.Quantity > 0
                && item.Warehouse is { DeletedAt: null } warehouse && warehouse.DepartmentId == departmentId).Select(item => item.Id).ToHashSet();
            dto.Shelves = dto.Shelves.Where(item => shelves.Contains(item.Id)).ToList();
            dto.ReservedQuantities = dto.ReservedQuantities.Where(item => reserved.Contains(item.Id)).ToList();
            // In a department report these are current stock positions, not the original global receipt.
            dto.RemainingQuantity = dto.Shelves.Sum(item => item.Quantity);
            dto.ReservedQuantity = dto.ReservedQuantities.Sum(item => item.Quantity);
            dto.TotalQuantity = dto.RemainingQuantity + dto.ReservedQuantity;
            dto.QuantityAssigned = dto.RemainingQuantity;
            dto.QuantityUnassigned = 0;
            dto.ConsumedQuantity = 0;
            dto.QuantityDistributed = 0;
            dto.SampledQuantity = 0;
        }
        dto.Locations = GetCurrentLocations(dto);
        dto.StockDepartments = ExpiryStockDepartments(batch, departmentId);
        return dto;
    }

    private static List<CollectionItemDto> ExpiryStockDepartments(MaterialBatch batch, Guid? departmentId)
    {
        var shelves = batch.ShelfMaterialBatches
            .Where(item => item.DeletedAt == null && item.Quantity > 0)
            .Select(item => item.WarehouseLocationShelf?.WarehouseLocationRack?.WarehouseLocation?.Warehouse);
        var reserved = batch.ReservedQuantities
            .Where(item => item.DeletedAt == null && item.ReleasedAt == null && item.Quantity > 0)
            .Select(item => item.Warehouse);
        return shelves.Concat(reserved).OfType<Warehouse>()
            .Where(warehouse => warehouse.DeletedAt == null && warehouse.Department != null && warehouse.Department.DeletedAt == null)
            .Where(warehouse => !departmentId.HasValue || warehouse.DepartmentId == departmentId)
            .Select(warehouse => warehouse.Department)
            .DistinctBy(department => department.Id)
            .OrderBy(department => department.Name).ThenBy(department => department.Id)
            .Select(department => new CollectionItemDto { Id = department.Id, Name = department.Name, Code = department.Code })
            .ToList();
    }
}
