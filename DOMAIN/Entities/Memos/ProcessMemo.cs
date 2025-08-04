namespace DOMAIN.Entities.Memos;

public class ProcessMemo
{
    public Guid VendorId { get; set; }
    public Guid SourceInventoryRequisitionId { get; set; }
    public List<CreateMemoItem> Items { get; set; } = [];
}