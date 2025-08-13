using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.LeaveRequests;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.ItemStockRequisitions;

public class ItemStockRequisition : BaseEntity
{
    [StringLength(1000)]public string Number { get; set; }
    public DateTime RequisitionDate { get; set; }
    
    public Guid RequestedById { get; set; }
    public User RequestedBy { get; set; }
    
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } 
    
    [StringLength(100000)] public string Justification { get; set; }
    
    public int QuantityRequested { get; set; }
    public LeaveStatus Status { get; set; } = LeaveStatus.Pending;
    
    public ICollection<ItemStockRequisitionItem> RequisitionItems { get; set; } = new List<ItemStockRequisitionItem>();
}