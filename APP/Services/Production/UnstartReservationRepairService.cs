using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;
using SHARED.Services.Identity;

namespace APP.Services.Production;

public sealed partial class UnstartReservationRepairService(ApplicationDbContext db, ICurrentUserService identity)
{
    private sealed record Plan(UnstartReservationPreviewDto Preview, List<MaterialBatchReservedQuantity> Reservations,
        Dictionary<Guid, ProductionUnstartRepair> Unstarts, string Snapshot);

    private async Task<Result<Guid>> Access(Guid materialId, MaterialKind kind, bool write)
    {
        if (kind is not (MaterialKind.Raw or MaterialKind.Package)) return Error.Validation("UnstartReservations.Kind", "Choose raw or packaging materials.");
        var grants = await CurrentPermissionSet.Load(db, identity.UserId);
        var view = kind == MaterialKind.Raw ? PermissionKeys.CanViewApprovedRawMaterials : PermissionKeys.CanViewApprovedPackagingMaterials;
        var repair = kind == MaterialKind.Raw ? PermissionKeys.CanRepairUnstartRawMaterialReservations : PermissionKeys.CanRepairUnstartPackagingMaterialReservations;
        var department = await db.Users.IgnoreQueryFilters().IgnoreAutoIncludes()
            .Where(user => user.Id == identity.UserId && user.DeletedAt == null && !user.IsDisabled).Select(user => user.DepartmentId).SingleOrDefaultAsync();
        if (!department.HasValue || !grants.Contains(view) || write && !grants.Contains(repair))
            return Error.Forbidden("UnstartReservations.Permission", "Your current department and permissions do not allow this reservation repair.");
        if (!await db.Materials.IgnoreQueryFilters().AnyAsync(material => material.Id == materialId && material.Kind == kind && material.DeletedAt == null))
            return Error.NotFound("UnstartReservations.Material", "Material not found for this variant.");
        return department.Value;
    }

    public async Task<Result<UnstartReservationPreviewDto>> Preview(Guid materialId, MaterialKind kind)
    {
        var access = await Access(materialId, kind, false);
        if (access.IsFailure) return access.Error;
        return (await Build(materialId, access.Value, kind)).Preview;
    }

    private async Task<Plan> Build(Guid materialId, Guid department, MaterialKind kind)
    {
        var reservations = await db.MaterialBatchReservedQuantities.IgnoreQueryFilters().IgnoreAutoIncludes()
            .Where(r => r.MaterialBatch.MaterialId == materialId && r.MaterialBatch.Material.Kind == kind
                && r.Warehouse.DepartmentId == department && r.ReleasedAt == null
                && db.ProductionUnstartRepairs.Any(unstart => unstart.BatchId == r.ProductionScheduleProductId && unstart.AppliedAt >= r.CreatedAt))
            .Include(r => r.MaterialBatch).ThenInclude(batch => batch.Material).Include(r => r.UoM).Include(r => r.Warehouse)
            .Include(r => r.ProductionScheduleProduct).ThenInclude(batch => batch.Product)
            .OrderBy(r => r.Id).Take(200).ToListAsync();
        var rows = new List<UnstartReservationRowDto>();
        var unstarts = new Dictionary<Guid, ProductionUnstartRepair>();
        foreach (var r in reservations)
        {
            var receipts = await db.ProductionUnstartRepairs.AsNoTracking().Where(item => item.BatchId == r.ProductionScheduleProductId && item.AppliedAt >= r.CreatedAt)
                .OrderByDescending(item => item.AppliedAt).ThenBy(item => item.Id).ToListAsync();
            var receipt = receipts.FirstOrDefault(item => ContainsReservation(item.BeforeJson, r.Id)) ?? receipts[0];
            unstarts[r.Id] = receipt;
            var row = new UnstartReservationRowDto { ReservationId = r.Id, UnstartRepairId = receipt.Id, UnstartedAt = receipt.AppliedAt,
                Product = r.ProductionScheduleProduct.Product.Name ?? "", ProductBatch = r.ProductionScheduleProduct.BatchNumber ?? "",
                MaterialBatch = r.MaterialBatch.BatchNumber ?? "", Quantity = r.Quantity, Unit = r.UoM?.Symbol ?? "" };
            if (!ContainsReservation(receipt.BeforeJson, r.Id))
                row.Blockers.Add("The original Unstart receipt does not identify this reservation. Investigate it before release.");
            if (!await db.ProductionActivities.IgnoreQueryFilters().AnyAsync(run => run.Id == receipt.ActivityId
                    && run.ProductionScheduleProductId == r.ProductionScheduleProductId && run.DeletedAt != null))
                row.Blockers.Add("The recorded production run is not archived.");
            if (await db.ProductionActivities.IgnoreQueryFilters().AnyAsync(run => run.ProductionScheduleProductId == r.ProductionScheduleProductId && run.DeletedAt == null))
                row.Blockers.Add("This batch has restarted. Its current reservations must be reviewed with the active run.");
            if (r.DeletedAt.HasValue && r.ProductionUnstartRepairId != receipt.Id)
                row.Blockers.Add("This reservation was consumed or retired outside the recorded Unstart.");
            if (r.Quantity <= 0 || r.MaterialBatch.DeletedAt.HasValue || r.MaterialBatch.Material.DeletedAt.HasValue
                || r.UoM is null || r.UoMId != r.MaterialBatch.UoMId || r.Warehouse.DeletedAt.HasValue
                || r.Warehouse.Type != WarehouseType.Production || r.ProductionScheduleProduct.Product.DepartmentId != department)
                row.Blockers.Add("The reservation has invalid stock, unit or department evidence.");
            var requests = db.Requisitions.IgnoreQueryFilters().Where(request => request.ProductionScheduleProductId == r.ProductionScheduleProductId);
            if (await requests.AnyAsync(request => request.Status == RequestStatus.Completed
                    || request.Items.Any(line => line.Status == RequestStatus.Completed || line.QuantityReceived != 0))
                || await db.StockRequisitionIssues.IgnoreQueryFilters().AnyAsync(issue => requests.Any(request => request.Id == issue.RequisitionId))
                || await db.BinCardInformation.IgnoreQueryFilters().AnyAsync(card => card.QuantityIssued != 0 && requests.Any(request => request.Id == card.RequisitionId)))
                row.Blockers.Add("Stock has already been issued or received for this production batch.");
            rows.Add(row);
        }
        var snapshot = JsonSerializer.Serialize(new { materialId, department, kind, actor = identity.UserId, rows,
            reservations = reservations.Select(r => new { r.Id, r.MaterialBatchId, r.ProductionScheduleProductId, r.WarehouseId,
                r.WarehouseLocationShelfId, r.UoMId, r.Quantity, r.CreatedAt, r.UpdatedAt, r.DeletedAt, r.LastDeletedById, r.ReleasedAt, r.ProductionUnstartRepairId }) });
        return new(new() { PreviewToken = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(snapshot))), Reservations = rows },
            reservations, unstarts, snapshot);
    }

    private static bool ContainsReservation(string snapshot, Guid id)
    {
        try
        {
            using var document = JsonDocument.Parse(snapshot);
            if (!document.RootElement.TryGetProperty("Reservations", out var rows) || rows.ValueKind != JsonValueKind.Array) return false;
            return rows.EnumerateArray().Any(row => row.ValueKind == JsonValueKind.Object && row.TryGetProperty("Id", out var value)
                && value.ValueKind == JsonValueKind.String && value.TryGetGuid(out var actual) && actual == id);
        }
        catch (JsonException) { return false; }
        catch (ArgumentException) { return false; }
    }
}
