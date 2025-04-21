using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using SHARED;

namespace DOMAIN.Entities.Approvals;

public class Approval : BaseEntity
{
    [StringLength(100)] public string ItemType { get; set; }
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
    public Guid? ApprovedById { get; set; }
    public User ApprovedBy { get; set; }
    [StringLength(1000)] public string Comments { get; set; } 
}

public class ApprovalEntity
{
    public Guid Id { get; set; } 
    public string Code { get; set; }       
    public string ModelType { get; set; } 
    public DepartmentDto  Department { get; set; }
    public List<ApprovalLog>  ApprovalLogs { get; set; }
    public DateTime CreatedAt { get; set; } 
}

public class ApprovalRequestBody
{
    public string Comments { get; set; }
}


public class ApprovalLog
{
    public CollectionItemDto User { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string Comments { get; set; }
}

