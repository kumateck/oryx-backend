using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.Requisitions;

public class CompletedRequisition : BaseEntity
{
    public Guid RequisitionId { get; set; }
    public Requisition Requisition { get; set; }
    public Guid RequestedById { get; set; }
    public User RequestedBy { get; set; }
    public RequestStatus Status { get; set; }  
    public RequisitionType RequisitionType { get; set; }

    [StringLength(1000)] public string Comments { get; set; }
    public bool Approved { get; set; }
    public List<CompletedRequisitionItem> Items { get; set; }
}

public class CompletedRequisitionItem : BaseEntity
{
    public Guid CompletedRequisitionId { get; set; }
    public CompletedRequisition CompletedRequisition { get; set; }
    public Guid MaterialId { get; set; }
    public Material Material { get; set; }
    public int Quantity { get; set; }
}