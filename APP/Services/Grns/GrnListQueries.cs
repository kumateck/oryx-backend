using APP.Extensions;
using DOMAIN.Entities.Grns;
using DOMAIN.Entities.Materials;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Services.Grns;

public static class GrnListQueries
{
    public sealed record BatchRow(Guid GrnId, GrnBatchSummaryDto Batch);

    public static IQueryable<Grn> Filter(ApplicationDbContext context, string search, MaterialKind? kind, Status? status)
    {
        // Keep global GRN department/deletion policy, batch filters, and original Any semantics.
        var query = context.Grns.IgnoreAutoIncludes().AsNoTracking();
        if (kind.HasValue)
            query = query.Where(grn => grn.MaterialBatches.Any(batch => batch.Material.Kind == kind));
        if (status.HasValue)
            query = query.Where(grn => grn.MaterialBatches.Any(batch => batch.Grn.Status == status));
        if (!string.IsNullOrEmpty(search))
            query = query.WhereSearch(search, grn => grn.GrnNumber, grn => grn.CarrierName);
        return query.TagWith("GRN list: filtered scope");
    }

    public static IQueryable<GrnSummaryDto> Page(IQueryable<Grn> query, int page, int pageSize)
        => query.OrderByDescending(grn => grn.CreatedAt).ThenByDescending(grn => grn.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(grn => new GrnSummaryDto {
                Id = grn.Id, CreatedAt = grn.CreatedAt, GrnNumber = grn.GrnNumber,
                CarrierName = grn.CarrierName, VehicleNumber = grn.VehicleNumber,
                Remarks = grn.Remarks, DeclarationNumber = grn.DeclarationNumber, Status = grn.Status
            }).TagWith("GRN list: page header scalars");

    public static IQueryable<BatchRow> Batches(ApplicationDbContext context, Guid[] grnIds)
        => context.MaterialBatches.IgnoreAutoIncludes().AsNoTracking()
            .Where(batch => batch.GrnId.HasValue && grnIds.Contains(batch.GrnId.Value))
            .OrderBy(batch => batch.Id)
            .Select(batch => new BatchRow(batch.GrnId.Value, new GrnBatchSummaryDto(batch.Id,
                batch.Checklist == null || batch.Checklist.Supplier == null ? null : batch.Checklist.Supplier.Name)))
            .TagWith("GRN list: page batch IDs and supplier names");
}
