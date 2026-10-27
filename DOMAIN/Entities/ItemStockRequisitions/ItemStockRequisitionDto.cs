using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.ItemStockRequisitions;

public class ItemStockRequisitionDto : BaseDto
{
    public string Number { get; set; }
    public DateTime RequisitionDate { get; set; }
    public UserDto RequestedBy { get; set; }
    public DepartmentDto Department { get; set; }
    public string Justification { get; set; }
    public IssueItemStockRequisitionStatus Status { get; set; }
    /// <summary>The store the requisition draws on; every line is from this store.</summary>
    public Store? Store { get; set; }
    /// <summary>False when earlier issues were recorded without their line, so remaining quantities are unknown.</summary>
    public bool IssueHistoryAttributed { get; set; } = true;
    public ICollection<ItemStockRequisitionItemDto> RequisitionItems { get; set; }
}