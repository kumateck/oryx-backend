using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;

namespace DOMAIN.Entities.Requisitions;

public class Requisition : BaseEntity
{
    public Guid RequestedById { get; set; }
    public User RequestedBy { get; set; }
    public RequestStatus Status { get; set; }  
    public RequisitionType RequisitionType { get; set; }

    [StringLength(1000)] public string Comments { get; set; }
    public bool Approved { get; set; }
    public List<RequisitionApproval> Approvals { get; set; }
    public List<RequisitionItem> Items { get; set; }
}

public class RequisitionItem : BaseEntity
{
    public Guid RequisitionId { get; set; }
    public Requisition Requisition { get; set; }
    public Guid MaterialId { get; set; }
    public Material Material { get; set; }
    public int Quantity { get; set; }
}

public class RequisitionApproval : BaseEntity
{
    public Guid RequisitionId { get; set; }
    public Requisition Requisition { get; set; }
    public Guid? UserId { get; set; }         // Foreign key to the user who needs to approve (optional)
    public User User { get; set; }
    public Guid? RoleId { get; set; }         // Foreign key to the role that needs to approve (optional)
    public Role Role { get; set; }
    public bool Required { get; set; }     
    public bool Approved { get; set; }             
    public DateTime? ApprovalTime { get; set; }       
    [StringLength(1000)] public string Comments { get; set; }             
    public int Order { get; set; }
}

public enum RequestStatus
{
    Pending,
    Completed,
    Rejected
}

public enum RequisitionType
{
    Internal,
    External
}