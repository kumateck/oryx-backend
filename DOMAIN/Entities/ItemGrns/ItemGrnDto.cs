using DOMAIN.Entities.Items;
using DOMAIN.Entities.Procurement.Suppliers;

namespace DOMAIN.Entities.ItemGrns;

public class ItemGrnDto
{
    public Guid ItemId { get; set; }
    public ItemDto Item { get; set; }
    public Guid SupplierId { get; set; }
    public SupplierDto Supplier { get; set; }
    public string InvoiceNumber { get; set; } 
    public int QuantityReceived { get; set; }
}