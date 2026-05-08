using DOMAIN.Entities.Base;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.Procurement.Suppliers;

namespace DOMAIN.Entities.ItemGrns;

public class ItemGrn : BaseEntity
{
    public Guid ItemId { get; set; }
    public Item Item { get; set; }
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; }
    public string InvoiceNumber { get; set; } 
    public int QuantityReceived { get; set; }
}