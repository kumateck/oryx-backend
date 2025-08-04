using DOMAIN.Entities.Base;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.Items.Requisitions;
using DOMAIN.Entities.VendorQuotations;
using SHARED;

namespace DOMAIN.Entities.Memos;

public class CreateMemoItem
{
    public Guid? VendorQuotationItemId { get; set; }
    public Guid? MarketRequisitionVendorId { get; set; }
    public Guid ItemId { get; set; }
    public Guid UoMId { get; set; }
    public decimal Quantity { get; set; }
}

public class Memo : BaseEntity
{
    public string Code { get; set; }
    public List<MemoItem> Items { get; set; } = [];
}

public class MemoDto : BaseDto
{
    public string Code { get; set; }
    public List<MemoItemDto> Items { get; set; } = [];
    public decimal TotalValue => Items.Sum(i => i.ItemValue);
}

public class MemoItem : BaseEntity
{
    public Guid MemoId { get; set; }
    public Memo Memo { get; set; }
    public Guid? VendorQuotationItemId { get; set; }
    public VendorQuotationItem VendorQuotationItem { get; set; }
    public Guid? MarketRequisitionVendorId { get; set; }
    public MarketRequisitionVendor MarketRequisitionVendor { get; set; }
    public Guid ItemId { get; set; }
    public Item Item { get; set; }
    public Guid UoMId { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public decimal Quantity { get; set; }
    public decimal PricePerUnit { get; set; }
    public DateTime? PurchasedAt { get; set; }
}

public class MemoItemDto : BaseDto
{
    public CollectionItemDto Memo { get; set; }
    public CollectionItemDto Vendor { get; set; }
    public CollectionItemDto Item { get; set; }
    public UnitOfMeasureDto UoM { get; set; }
    public decimal Quantity { get; set; }
    public decimal PricePerUnit { get; set; }
    public decimal ItemValue => Quantity * PricePerUnit;
    public CollectionItemDto TermsOfPayment { get; set; }
    public string DeliveryMode { get; set; }
    public DateTime EstimatedDeliveryDate { get; set; }
}