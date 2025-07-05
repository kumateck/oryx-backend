using APP.IRepository;
using AutoMapper;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.ProductionSchedules.StockTransfers;
using DOMAIN.Entities.Reports;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class ReportRepository(ApplicationDbContext context, IMapper mapper, IMaterialRepository materialRepository) : IReportRepository
{
    public async Task<Result<ProductionReportDto>> GetProductionReport(ReportFilter filter, Guid departmentId)
    {
        var purchaseRequisitions = context.Requisitions
            .Where(r => r.RequisitionType == RequisitionType.Purchase)
            .AsQueryable();

        var sourceRequisitions = context.SourceRequisitions.AsQueryable();
        var productionSchedules = context.ProductionSchedules.AsQueryable();
        var incomingStockTransfers = context.StockTransferSources
            .Where(s => s.FromDepartmentId == departmentId)
            .AsQueryable();
        var outGoingStockTransfers = context.StockTransferSources
            .Where(s => s.ToDepartmentId == departmentId)
            .AsQueryable();

        if (filter.StartDate.HasValue)
        {
            var start = filter.StartDate.Value;
            purchaseRequisitions = purchaseRequisitions.Where(r => r.CreatedAt >= start);
            sourceRequisitions = sourceRequisitions.Where(r => r.CreatedAt >= start);
            productionSchedules = productionSchedules.Where(p => p.CreatedAt >= start);
            incomingStockTransfers = incomingStockTransfers.Where(s => s.CreatedAt >= start);
            outGoingStockTransfers = outGoingStockTransfers.Where(s => s.CreatedAt >= start);
        }

        if (filter.EndDate.HasValue)
        {
            var end = filter.EndDate.Value.AddDays(1);
            purchaseRequisitions = purchaseRequisitions.Where(r => r.CreatedAt < end);
            sourceRequisitions = sourceRequisitions.Where(r => r.CreatedAt < end);
            productionSchedules = productionSchedules.Where(p => p.CreatedAt < end);
            incomingStockTransfers = incomingStockTransfers.Where(s => s.CreatedAt < end);
            outGoingStockTransfers = outGoingStockTransfers.Where(s => s.CreatedAt < end);
        }

        var sourceRequisitionIds = await sourceRequisitions.Select(r => r.Id).ToListAsync();

        var purchaseOrderNumber = await context.PurchaseOrders
            .Where(p => sourceRequisitionIds.Contains(p.SourceRequisitionId))
            .CountAsync();

        return new ProductionReportDto
        {
            NumberOfPurchaseRequisitions = await purchaseRequisitions.CountAsync(),
            NumberOfNewPurchaseRequisitions = await purchaseRequisitions.CountAsync(p => p.Status == RequestStatus.New),
            NumberOfInProgressPurchaseRequisitions = await purchaseRequisitions.CountAsync(p => p.Status == RequestStatus.Pending),
            NumberOfCompletedPurchaseRequisitions = purchaseOrderNumber,
            
            NumberOfProductionSchedules = await productionSchedules.CountAsync(),
            NumberOfNewProductionSchedules = await productionSchedules.CountAsync(p => p.Status == ProductionStatus.New),
            NumberOfInProgressProductionSchedules = await productionSchedules.CountAsync(p => p.Status == ProductionStatus.InProgress),
            NumberOfCompletedProductionSchedules = await productionSchedules.CountAsync(p => p.Status == ProductionStatus.Completed),
            
            NumberOfIncomingStockTransfers = await incomingStockTransfers.CountAsync(),
            NumberOfIncomingPendingStockTransfers = await incomingStockTransfers.CountAsync(s => s.Status == StockTransferStatus.InProgress),
            NumberOfIncomingCompletedStockTransfers = await incomingStockTransfers.CountAsync(s => s.Status == StockTransferStatus.Approved),
            
            NumberOfOutgoingStockTransfers = await outGoingStockTransfers.CountAsync(),
            NumberOfOutgoingPendingStockTransfers = await outGoingStockTransfers.CountAsync(s => s.Status == StockTransferStatus.InProgress),
            NumberOfOutgoingCompletedStockTransfers = await outGoingStockTransfers.CountAsync(s => s.Status == StockTransferStatus.Approved),
        };
    }


    public async Task<Result<List<MaterialDto>>> GetMaterialsBelowMinimumStockLevel(Guid departmentId)
    {
        var materialDepartments = await context.MaterialDepartments
            .AsSplitQuery()
            .Include(md => md.Material)
            .Where(md => md.DepartmentId == departmentId)
            .ToListAsync();

        var rawWarehouse = await context.Warehouses
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.DepartmentId == departmentId && w.Type == WarehouseType.RawMaterialStorage);

        var packageWarehouse = await context.Warehouses
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.DepartmentId == departmentId && w.Type == WarehouseType.PackagedStorage);

        if (rawWarehouse == null || packageWarehouse == null)
            return Error.Failure("Department.Warehouse", "One or more required warehouses not found.");

        List<Material> materialsBelowMinStock = [];

        foreach (var materialDepartment in materialDepartments)
        {
            var material = materialDepartment.Material;
            var warehouseId = material.Kind == MaterialKind.Raw ? rawWarehouse.Id : packageWarehouse.Id;

            var stockResult = await materialRepository.GetMaterialStockInWarehouse(material.Id, warehouseId);

            if (stockResult.IsFailure) continue;

            var stock = stockResult.Value;

            if (stock < materialDepartment.MinimumStockLevel)
            {
                materialsBelowMinStock.Add(material);
            }
        }

        return mapper.Map<List<MaterialDto>>(materialsBelowMinStock);
    }
}