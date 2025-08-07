using System.ComponentModel.DataAnnotations.Schema;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.LeaveRequests;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.ItemStockRequisitions;

public class ItemStockRequisitionDto : BaseDto
{
    public string Number { get; set; }
    public DateTime RequisitionDate { get; set; }
    public UserDto RequestedBy { get; set; }
    public DepartmentDto Department { get; set; }
    public string Justification { get; set; }
    public LeaveStatus Status { get; set; }
}