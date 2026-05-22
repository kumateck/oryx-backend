using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Warehouses;

namespace DOMAIN.Entities.StockAdjustments;

public class StockAdjustment : BaseEntity, IRequireApproval
{
    [Required]
    [StringLength(50)]
    public string AdjustmentNumber { get; set; }

    public DateTime AdjustmentDate { get; set; }

    public StockAdjustmentTarget TargetType { get; set; }

    public bool Approved { get; set; }

    public List<StockAdjustmentLine> Lines { get; set; } = [];

    public List<StockAdjustmentApproval> Approvals { get; set; } = [];
}

public class StockAdjustmentApproval : ResponsibleApprovalStage
{
    public Guid Id { get; set; }
    public Guid StockAdjustmentId { get; set; }
    public StockAdjustment StockAdjustment { get; set; }
    public Guid ApprovalId { get; set; }
    public Approval Approval { get; set; }
}

public enum StockAdjustmentTarget
{
    Item = 0,
    Material = 1,
    Product = 2,
}
