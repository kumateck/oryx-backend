using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.ItemStockRequisitions;

public class ItemStockRequisition : BaseEntity
{
    [StringLength(1000)] public string Number { get; set; }
    public DateTime RequisitionDate { get; set; } = DateTime.UtcNow;

    public Guid RequestedById { get; set; }
    public User RequestedBy { get; set; }

    public Guid DepartmentId { get; set; }
    public Department Department { get; set; }

    [StringLength(100000)] public string Justification { get; set; }

    public int QuantityRequested { get; set; }
    public IssueItemStockRequisitionStatus Status { get; set; } = IssueItemStockRequisitionStatus.Pending;

    public ICollection<ItemStockRequisitionItem> RequisitionItems { get; set; } = new List<ItemStockRequisitionItem>();
}


public class IssueItemStockRequisition : BaseEntity
{
    public Guid ItemStockRequisitionId { get; set; }
    public ItemStockRequisition ItemStockRequisition { get; set; }

    /// <summary>
    /// The requisition line this quantity was issued against. Issues recorded before lines were tracked
    /// have none unless their requisition had a single line; such requisitions cannot be issued further.
    /// </summary>
    public Guid? ItemStockRequisitionItemId { get; set; }
    public ItemStockRequisitionItem ItemStockRequisitionItem { get; set; }

    public int QuantityIssued { get; set; }
}

public class IssueItemStockRequisitionDto : BaseDto
{
    public Guid ItemStockRequisitionId { get; set; }
    public ItemStockRequisitionDto ItemStockRequisition { get; set; }
    public int QuantityIssued { get; set; }
}

public enum IssueItemStockRequisitionStatus
{
    Pending,
    Partial,
    Completed
}