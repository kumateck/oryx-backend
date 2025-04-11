using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.Approvals;

public class Approval : BaseEntity
{
    [StringLength(1000)] public string ItemType { get; set; }
    public List<ApprovalStage> ApprovalStages { get; set; }
}

public class ApprovalStage :  CurrentApprovalStage
{
    public Guid Id { get; set; }
    public Guid ApprovalId { get; set; }
    public Approval Approval { get; set; }
    public int Order { get; set; }
    public bool Required { get; set; }     
}

public class CurrentApprovalStage
{
    public Guid? UserId { get; set; }
    public User User { get; set; }
    public Guid? RoleId { get; set; }
    public Role Role { get; set; }
}

public class ResponsibleApprovalStage : CurrentApprovalStage
{ 
    public bool Required { get; set; }     
    public int Order { get; set; }
    public bool Approved { get; set; }             
    public DateTime? ApprovalTime { get; set; }       
    [StringLength(1000)] public string Comments { get; set; } 
}

public class ApprovalEntity
{
    public Guid Id { get; set; } 
    public string Code { get; set; }       
    public string ModelType { get; set; } 
    public DateTime CreatedAt { get; set; } 
}

public class ApprovalRequestBody
{
    public string Comments { get; set; }
}

