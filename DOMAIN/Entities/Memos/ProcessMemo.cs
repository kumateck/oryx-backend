namespace DOMAIN.Entities.Memos;

public class ProcessMemo
{
    public List<CreateMemoItem> Items { get; set; } = [];
}

public class ProcessOpenMarketMemo
{
    public Guid MarketRequisitionVendorId { get; set; }
    public Guid SourceInventoryRequisitionId { get; set; }
    public List<CreateMemoItem> Items { get; set; } = [];
}