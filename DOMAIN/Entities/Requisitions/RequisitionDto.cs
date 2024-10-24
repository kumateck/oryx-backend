using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Roles;
using SHARED;

namespace DOMAIN.Entities.Requisitions;

public class RequisitionDto : RequisitionListDto
{
    public List<RequisitionApprovalDto> Approvals { get; set; }
}

public class RequisitionListDto
{
    public RequisitionType RequisitionType { get; set; }
    public CollectionItemDto RequestedBy { get; set; }
    public List<RequisitionItemDto> Items { get; set; }
    public bool Approved { get; set; }
    public List<RequisitionApprovalDto> Approvals { get; set; }
    public DateTime RequestedAt { get; set; }
    public RequestStatus Status { get; set; }  
    public string Comments { get; set; }
}

public class RequisitionItemDto 
{
    public MaterialDto Material { get; set; }
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