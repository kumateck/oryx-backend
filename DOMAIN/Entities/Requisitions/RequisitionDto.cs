using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.Requisitions;

public class RequisitionDto : RequisitionListDto
{
    public List<RequisitionApprovalDto> Approvals { get; set; }
}

public class RequisitionListDto
{
    public MaterialDto Material { get; set; }
    public int Quantity { get; set; }
    public DateTime RequestedAt { get; set; }
    public UserDto RequestedBy { get; set; }
    public RequestStatus Status { get; set; }  
    public RequisitionType RequisitionType { get; set; }
    public string Comments { get; set; }
}

public class RequisitionItemDto 
{
    public RequisitionDto Requisition { get; set; }
    public MaterialDto Material { get; set; }
    public int Quantity { get; set; }
}

public class RequisitionApprovalDto
{
    public UserDto User { get; set; }
    public RoleDto Role { get; set; }
    public bool Required { get; set; }    
    public bool IsApproved { get; set; }              
    public DateTime? ApprovalTime { get; set; }      
    public string Comments { get; set; }
    public int Order { get; set; }
}