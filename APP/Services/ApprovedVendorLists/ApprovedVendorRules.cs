using DOMAIN.Entities.ApprovedVendorLists;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.ApprovedVendorLists;

/// <summary>
/// What the current approved vendor list allows procurement to use. Owner's rules (2026-11-19):
/// a material that has no line on the current approved list is unrestricted; a listed material may only
/// be bought through a supplier, and from a manufacturer, on one of its lines whose validity month has not
/// passed. A lapsed or missing line blocks that supplier or manufacturer for that material only.
/// </summary>
public static class ApprovedVendorRules
{
    /// <summary>The first day after the validity month: a line is lapsed from this day on.</summary>
    public static DateTime LapsesOn(DateTime validityMonth) => Month(validityMonth).AddMonths(1);

    public static bool IsLapsed(DateTime validityMonth, DateTime today) => today.Date >= LapsesOn(validityMonth);

    /// <summary>The first day of a date's month (UTC midnight).</summary>
    public static DateTime Month(DateTime value) => new(value.Year, value.Month, 1, 0, 0, 0, DateTimeKind.Utc);

    private sealed record Line(Guid MaterialId, string MaterialName, string MaterialCode, Guid ManufacturerId, Guid SupplierId, DateTime ValidityDate);

    private static async Task<List<Line>> CurrentLines(ApplicationDbContext context, IReadOnlyCollection<Guid> materialIds) =>
        await context.ApprovedVendorListLines.AsNoTracking()
            .Where(line => materialIds.Contains(line.MaterialId)
                && line.ApprovedVendorList.Status == ApprovedVendorListStatus.Approved
                && line.ApprovedVendorList.DeletedAt == null)
            .Select(line => new Line(line.MaterialId, line.Material.Name, line.Material.Code, line.ManufacturerId, line.SupplierId, line.ValidityDate))
            .ToListAsync();

    /// <summary>The eligibility of each material, for screens that hide or disable choices.</summary>
    public static async Task<List<ApprovedVendorEligibilityDto>> Eligibility(ApplicationDbContext context,
        IReadOnlyCollection<Guid> materialIds, DateTime today)
    {
        var ids = materialIds.Distinct().ToList();
        var lines = await CurrentLines(context, ids);
        return ids.Select(id =>
        {
            var own = lines.Where(line => line.MaterialId == id).ToList();
            return new ApprovedVendorEligibilityDto
            {
                MaterialId = id,
                Restricted = own.Count > 0,
                Approved = own.Where(line => !IsLapsed(line.ValidityDate, today))
                    .Select(line => new ApprovedVendorPairDto { ManufacturerId = line.ManufacturerId, SupplierId = line.SupplierId, ValidityDate = line.ValidityDate })
                    .ToList(),
            };
        }).ToList();
    }

    /// <summary>Refuses any (material, supplier) pair the current list does not allow.</summary>
    public static async Task<Result> CheckSuppliers(ApplicationDbContext context,
        IEnumerable<(Guid MaterialId, Guid SupplierId)> pairs, DateTime today)
    {
        var wanted = pairs.Distinct().ToList();
        if (wanted.Count == 0) return Result.Success();
        var lines = await CurrentLines(context, wanted.Select(pair => pair.MaterialId).Distinct().ToList());
        foreach (var (materialId, supplierId) in wanted)
        {
            var own = lines.Where(line => line.MaterialId == materialId).ToList();
            if (own.Count == 0) continue;
            var matching = own.Where(line => line.SupplierId == supplierId).ToList();
            if (matching.Any(line => !IsLapsed(line.ValidityDate, today))) continue;
            var supplier = await Name(context.Suppliers.Where(item => item.Id == supplierId).Select(item => item.Name));
            return matching.Count == 0
                ? Error.Validation("ApprovedVendorList.SupplierNotApproved",
                    $"{supplier} is not on the approved vendor list for {Label(own[0])}. Ask QA to add it to a new revision of the list.")
                : Error.Validation("ApprovedVendorList.SupplierLapsed",
                    $"{supplier}'s approval for {Label(own[0])} lapsed at the end of {matching.Max(line => line.ValidityDate):MMMM yyyy}. Ask QA to renew it in a new revision of the list.");
        }
        return Result.Success();
    }

    /// <summary>
    /// Refuses any manufacturer the current list does not allow for the material through the supplier.
    /// A null supplier means the supplier is not known here: any active line for the manufacturer will do.
    /// </summary>
    public static async Task<Result> CheckManufacturers(ApplicationDbContext context,
        IEnumerable<(Guid MaterialId, Guid ManufacturerId, Guid? SupplierId)> choices, DateTime today)
    {
        var wanted = choices.Distinct().ToList();
        if (wanted.Count == 0) return Result.Success();
        var lines = await CurrentLines(context, wanted.Select(choice => choice.MaterialId).Distinct().ToList());
        foreach (var (materialId, manufacturerId, supplierId) in wanted)
        {
            var own = lines.Where(line => line.MaterialId == materialId).ToList();
            if (own.Count == 0) continue;
            var matching = own.Where(line => line.ManufacturerId == manufacturerId
                && (!supplierId.HasValue || line.SupplierId == supplierId.Value)).ToList();
            if (matching.Any(line => !IsLapsed(line.ValidityDate, today))) continue;
            var manufacturer = await Name(context.Manufacturers.Where(item => item.Id == manufacturerId).Select(item => item.Name));
            var through = supplierId.HasValue
                ? " through " + await Name(context.Suppliers.Where(item => item.Id == supplierId.Value).Select(item => item.Name))
                : "";
            return matching.Count == 0
                ? Error.Validation("ApprovedVendorList.ManufacturerNotApproved",
                    $"{manufacturer}{through} is not on the approved vendor list for {Label(own[0])}. Ask QA to add it to a new revision of the list.")
                : Error.Validation("ApprovedVendorList.ManufacturerLapsed",
                    $"{manufacturer}{through}: approval for {Label(own[0])} lapsed at the end of {matching.Max(line => line.ValidityDate):MMMM yyyy}. Ask QA to renew it in a new revision of the list.");
        }
        return Result.Success();
    }

    /// <summary>
    /// A purchase order invoice names manufacturers but not materials: a manufacturer is allowed when some
    /// material on the order is unrestricted, or the list allows it for one of the order's materials through
    /// the order's supplier.
    /// </summary>
    public static async Task<Result> CheckOrderManufacturers(ApplicationDbContext context, Guid purchaseOrderId,
        IEnumerable<Guid> manufacturerIds, DateTime today)
    {
        var wanted = manufacturerIds.Distinct().ToList();
        if (wanted.Count == 0) return Result.Success();
        var order = await context.PurchaseOrders.AsNoTracking().Where(item => item.Id == purchaseOrderId)
            .Select(item => new { item.SupplierId, Materials = item.Items.Select(line => line.MaterialId).ToList() })
            .FirstOrDefaultAsync();
        if (order is null) return Result.Success();
        var materials = order.Materials.Distinct().ToList();
        var lines = await CurrentLines(context, materials);
        if (materials.Any(material => lines.All(line => line.MaterialId != material))) return Result.Success();
        foreach (var manufacturerId in wanted)
        {
            if (lines.Any(line => line.ManufacturerId == manufacturerId && line.SupplierId == order.SupplierId
                && !IsLapsed(line.ValidityDate, today))) continue;
            var manufacturer = await Name(context.Manufacturers.Where(item => item.Id == manufacturerId).Select(item => item.Name));
            var supplier = await Name(context.Suppliers.Where(item => item.Id == order.SupplierId).Select(item => item.Name));
            return Error.Validation("ApprovedVendorList.ManufacturerNotApproved",
                $"{manufacturer} through {supplier} is not currently on the approved vendor list for any material on this purchase order. Ask QA to add or renew it in a new revision of the list.");
        }
        return Result.Success();
    }

    /// <summary>Shipment invoice lines: each manufacturer must be allowed for the material through its order's supplier.</summary>
    public static async Task<Result> CheckShipmentManufacturers(ApplicationDbContext context,
        IEnumerable<(Guid MaterialId, Guid ManufacturerId, Guid PurchaseOrderId)> lines, DateTime today)
    {
        var wanted = lines.ToList();
        if (wanted.Count == 0) return Result.Success();
        var orderIds = wanted.Select(line => line.PurchaseOrderId).Distinct().ToList();
        var suppliers = await context.PurchaseOrders.AsNoTracking().Where(order => orderIds.Contains(order.Id))
            .ToDictionaryAsync(order => order.Id, order => order.SupplierId);
        return await CheckManufacturers(context,
            wanted.Select(line => (line.MaterialId, line.ManufacturerId,
                suppliers.TryGetValue(line.PurchaseOrderId, out var supplier) ? supplier : (Guid?)null)), today);
    }

    private static string Label(Line line) => string.IsNullOrWhiteSpace(line.MaterialCode)
        ? line.MaterialName
        : $"{line.MaterialName} ({line.MaterialCode})";

    private static async Task<string> Name(IQueryable<string> query) =>
        await query.IgnoreQueryFilters().FirstOrDefaultAsync() ?? "This supplier or manufacturer";
}
