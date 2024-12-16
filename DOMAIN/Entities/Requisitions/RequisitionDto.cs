using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using SHARED;

namespace DOMAIN.Entities.Requisitions;

public class RequisitionDto 
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public RequisitionType RequisitionType { get; set; }
    public UserDto RequestedBy { get; set; }
    public List<RequisitionItemDto> Items { get; set; }
    public bool Approved { get; set; }
    public List<RequisitionApprovalDto> Approvals { get; set; }
    public DateTime? ExpectedDelivery { get; set; }
    public DateTime CreatedAt { get; set; }
    public RequestStatus Status { get; set; }  
    public string Comments { get; set; }
}

public class RequisitionItemDto 
{
    public MaterialDto Material { get; set; }
    public CollectionItemDto UoM { get; set; }
    public int Quantity { get; set; }
}

public class RequisitionApprovalDto
{
    public CollectionItemDto User { get; set; }
    public RoleDto Role { get; set; }
    public bool Required { get; set; }    
    public bool IsApproved { get; set; }              
    public DateTime? ApprovalTime { get; set; }      
    public string Comments { get; set; }
    public int Order { get; set; }
}