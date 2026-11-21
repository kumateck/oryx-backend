using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Products;

namespace DOMAIN.Entities.Costing;

/// <summary>An immutable approved division price sheet; approval and effective date both govern its use.</summary>
public class CostingPriceList : BaseEntity
{
    public Division Division { get; set; }
    [StringLength(100)] public string DocumentNumber { get; set; }
    public int RevisionNumber { get; set; }
    public Guid? PreviousRevisionId { get; set; }
    public CostingPriceList PreviousRevision { get; set; }
    public DateTime EffectiveFrom { get; set; }
    [StringLength(2000)] public string RevisionReason { get; set; }
    public CostingPriceListStatus Status { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public int Version { get; set; }
    public List<CostingPriceListLine> Lines { get; set; } = [];
    public List<CostingPriceListApproval> Approvals { get; set; } = [];
}

public enum CostingPriceListStatus { Pending = 0, Approved = 1, Rejected = 3, Draft = 4 }

public class CostingPriceListLine : BaseEntity
{
    public Guid CostingPriceListId { get; set; }
    public CostingPriceList CostingPriceList { get; set; }
    public Guid ProductId { get; set; }
    public Guid ProductPackingId { get; set; }
    [StringLength(10000)] public string PackingStyle { get; set; }
    public Guid? DepartmentId { get; set; }
    [StringLength(255)] public string ProductCode { get; set; }
    [StringLength(255)] public string ProductName { get; set; }
    public decimal? Price { get; set; }
    [StringLength(100)] public string PriceUoM { get; set; }
    public Guid? CurrencyId { get; set; }
    [StringLength(255)] public string CurrencyName { get; set; }
    [StringLength(100)] public string CurrencySymbol { get; set; }
}

public class CostingPriceListApproval : ResponsibleApprovalStage
{
    public Guid Id { get; set; }
    public Guid CostingPriceListId { get; set; }
    public CostingPriceList CostingPriceList { get; set; }
    public Guid ApprovalId { get; set; }
    public Approval Approval { get; set; }
}
