using DOMAIN.Entities.Requisitions;

namespace DOMAIN.Entities.Approvals;

public class CreateApprovalRequest
{
    public RequisitionType RequisitionType { get; set; }
    public List<CreateApprovalStageRequest> ApprovalStages { get; set; }
}

public class CreateApprovalStageRequest 
{
    public Guid? UserId { get; set; }       
    public Guid? RoleId { get; set; }        
    public bool Required { get; set; }     
    public int Order { get; set; }
}