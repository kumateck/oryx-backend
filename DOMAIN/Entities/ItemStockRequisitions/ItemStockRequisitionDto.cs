using System.ComponentModel.DataAnnotations.Schema;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.LeaveRequests;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.ItemStockRequisitions;

public class ItemStockRequisitionDto : BaseDto
{
    public string Code { get; set; }
    public DateTime RequisitionDate { get; set; }
    
    [ForeignKey(nameof(User))]
    public Guid RequestedById { get; set; }
    public UserDto RequestedBy { get; set; }
    
    public Guid DepartmentId { get; set; }
    public DepartmentDto Department { get; set; }
    
    public string Justification { get; set; }
    public List<ItemDto> Items { get; set; }
    public LeaveStatus Status { get; set; }
}