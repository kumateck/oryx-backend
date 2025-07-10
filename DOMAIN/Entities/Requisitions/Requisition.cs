using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.Requisitions;

public class Requisition : BaseEntity, IRequireApproval
{
    [StringLength(255)] public string Code { get; set; }
    public Guid RequestedById { get; set; }
    public User RequestedBy { get; set; }
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; }
    public RequisitionType RequisitionType { get; set; }
    public RequestStatus Status { get; set; }  
    [StringLength(1000)] public string Comments { get; set; }
    public DateTime? ExpectedDelivery { get; set; }
    public Guid? ProductId { get; set; }
    public Product Product { get; set; }
    public Guid? ProductionScheduleId { get; set; }
    public ProductionSchedule ProductionSchedule { get; set; }
    public Guid? ProductionActivityStepId { get; set; }
    public ProductionActivityStep ProductionActivityStep { get; set; }
    public List<RequisitionApproval> Approvals { get; set; }
    public List<RequisitionItem> Items { get; set; }
    public bool Approved { get; set; }
}

public class RequisitionItem : BaseEntity
{
    public Guid RequisitionId { get; set; }
    public Requisition Requisition { get; set; }
    public Guid MaterialId { get; set; }
    public Material Material { get; set; }
    public Guid? UoMId { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public decimal Quantity { get; set; }
    public decimal QuantityReceived { get; set; }
    public RequestStatus Status { get; set; }  
}

public class RequisitionApproval : ResponsibleApprovalStage
{
    public Guid Id { get; set; }
    public Guid RequisitionId { get; set; }
    public Requisition Requisition { get; set; }
    public Guid ApprovalId { get; set; }
    public Approval Approval { get; set; }
}

public enum RequestStatus
{
    New,
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