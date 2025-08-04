using System.ComponentModel.DataAnnotations.Schema;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.LeaveRequests;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.ItemStockRequisitions;

public class ItemStockRequisition : BaseEntity
{
    public string RequisitionNo { get; set; }
    public DateTime RequisitionDate { get; set; }
    
    [ForeignKey(nameof(User))]
    public Guid RequestedById { get; set; }
    public User RequestedBy { get; set; }
    
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; }
    
    public string Justification { get; set; }
    public List<Item> Items { get; set; }
    public LeaveStatus Status { get; set; } = LeaveStatus.Pending;
}