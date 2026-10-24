using DOMAIN.Entities.Procurement.Distribution;
using DOMAIN.Entities.Shipments;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.Procurement;

public sealed class MaterialDistributionPageReader(ApplicationDbContext context,
    Action<MaterialDistributionSection, List<ShipmentInvoiceItemDto>> allocate)
{
    public async Task<Result<MaterialDistributionPageDto>> Read(Guid shipmentId)
    {
        try
        {
            var shipment = await context.ShipmentDocuments.IgnoreAutoIncludes().AsNoTracking()
                .Where(document => document.Id == shipmentId)
                .Select(document => new { document.ShipmentInvoiceId })
                .TagWith("Material distribution page: shipment invoice reference").FirstOrDefaultAsync();
            if (shipment == null)
                return Error.NotFound("ShipmentDoc.NotFound", "Shipment document not found");
            if (!shipment.ShipmentInvoiceId.HasValue)
                return Error.NotFound("ShipmentInvoice.NotFound", "Shipment invoice not found");

            var lines = await MaterialDistributionPageQueries.InvoiceLines(context, shipment.ShipmentInvoiceId.Value).ToListAsync();
            var materialIds = lines.Select(line => line.Material.Id).Distinct().ToArray();
            if (materialIds.Length == 0) return new MaterialDistributionPageDto([]);
            var requests = (await MaterialDistributionPageQueries.Requests(context, shipmentId, materialIds).ToListAsync())
                .ToLookup(row => row.MaterialId, row => row.Share);
            var sections = new List<DistributionPageSection>();
            foreach (var group in lines.GroupBy(line => line.Material.Id))
            {
                var shares = requests[group.Key].ToList();
                if (shares.Count == 0)
                    return Error.Validation("Shipment.Document", "No requisitions found for this shipment doc");
                var section = new MaterialDistributionSection {
                    Material = group.First().Material, UoM = group.First().UoM,
                    TotalQuantity = group.Sum(line => line.ReceivedQuantity), Items = shares
                };
                allocate(section, group.ToList());
                sections.Add(MaterialDistributionPageResponse.From(section));
            }
            return new MaterialDistributionPageDto(sections);
        }
        catch (Exception ex)
        {
            return Error.Failure("500", ex.Message);
        }
    }
}
