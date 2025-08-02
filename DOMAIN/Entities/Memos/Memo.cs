using DOMAIN.Entities.Base;
using DOMAIN.Entities.Items;
using SHARED;

namespace DOMAIN.Entities.Memos;

public class Memo : BaseEntity
{
    public string Code { get; set; }
    public string AmountInWords { get; set; }
    public string TermsOfPayment { get; set; }
    public string DeliveryMode { get; set; }
    public DateTime EstimatedDeliveryDate { get; set; }
    public List<MemoItem> Items { get; set; } = [];
}

public class MemoDto : BaseDto
{
    public string Code { get; set; }
    public string AmountInWords { get; set; }
    public string TermsOfPayment { get; set; }
    public string DeliveryMode { get; set; }
    public DateTime EstimatedDeliveryDate { get; set; }
    public List<MemoItemDto> Items { get; set; } = [];
    public decimal TotalValue => Items.Sum(i => i.ItemValue);
}

public class MemoItem : BaseEntity
{
    public Guid MemoId { get; set; }
    public Memo Memo { get; set; }
    public Guid VendorId { get; set; }
    //public Vendor Vendor { get; set; }
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
}