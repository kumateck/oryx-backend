using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.Requisitions;

public class Requisition : BaseEntity
{
    [StringLength(255)] public string Code { get; set; }
    public Guid RequestedById { get; set; }
    public User RequestedBy { get; set; }
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; }
    public RequestStatus Status { get; set; }  
    public RequisitionType RequisitionType { get; set; }
    [StringLength(1000)] public string Comments { get; set; }
    public bool Approved { get; set; }
    public DateTime? ExpectedDelivery { get; set; }
    public Guid? ProductId { get; set; }
    public Product Product { get; set; }
    public Guid? ProductionScheduleId { get; set; }
    public ProductionSchedule ProductionSchedule { get; set; }
    public Guid? ProductionActivityStepId { get; set; }
    public ProductionActivityStep ProductionActivityStep { get; set; }
    public List<RequisitionApproval> Approvals { get; set; }
    public List<RequisitionItem> Items { get; set; }
}

public class RequisitionItem : BaseEntity
{
    public Guid RequisitionId { get; set; }
    public Requisition Requisition { get; set; }
    public Guid MaterialId { get; set; }
    public Material Material { get; set; }
    public Guid? UomId { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public decimal Quantity { get; set; }
    public decimal QuantityReceived { get; set; }
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
    Sourced,
    Completed,
    Rejected
}

public enum RequisitionType
{
    Stock,
    Purchase
}