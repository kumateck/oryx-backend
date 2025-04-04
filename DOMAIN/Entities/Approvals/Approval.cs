using DOMAIN.Entities.Base;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.Approvals;

public class Approval : BaseEntity
{
    //[StringLength(1000)] public string ItemType { get; set; }
    public List<ApprovalStage> ApprovalStages { get; set; }
    public RequisitionType RequisitionType { get; set; }
}

public class ApprovalStage : BaseEntity
{
    public Guid ApprovalId { get; set; }
    public Approval Approval { get; set; }
    public Guid? UserId { get; set; }       
    public User User { get; set; }
    public Guid? RoleId { get; set; }        
    public Role Role { get; set; }
    public bool Required { get; set; }     
    public int Order { get; set; }
}