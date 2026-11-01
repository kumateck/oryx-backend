using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.Requisitions;

/// <summary>
/// The issue of a stock requisition: who released the stock from the store, and the warehouse
/// check of that issue. The check is its own approval document ("StockRequisitionIssue"), so its
/// approver signs the voucher as "Checked by".
/// </summary>
public class StockRequisitionIssue : BaseEntity, IRequireApproval
{
    public Guid RequisitionId { get; set; }
    public Requisition Requisition { get; set; }
    public Guid IssuedById { get; set; }
    public User IssuedBy { get; set; }
    public DateTime IssuedAt { get; set; }
    public bool Approved { get; set; }
    public bool Rejected { get; set; }
    public List<StockRequisitionIssueApproval> Approvals { get; set; } = [];
}

public class StockRequisitionIssueApproval : ResponsibleApprovalStage
{
    public Guid Id { get; set; }
    public Guid StockRequisitionIssueId { get; set; }
    public StockRequisitionIssue StockRequisitionIssue { get; set; }
    public Guid ApprovalId { get; set; }
    public Approval Approval { get; set; }
}

/// <summary>One signature line of a printed voucher: who signed, their stored signature image and when.</summary>
public class VoucherSignatoryDto
{
    public Guid? UserId { get; set; }
    public string Name { get; set; }
    public string Signature { get; set; }
    public DateTime? SignedAt { get; set; }
}

/// <summary>
/// The signature lines of a stock requisition voucher. Issued by is the store user who issued it,
/// Checked by the approver of the issue check, Received by the person who requested it.
/// </summary>
public class StockIssueSignatoriesDto
{
    public VoucherSignatoryDto IssuedBy { get; set; }
    public VoucherSignatoryDto CheckedBy { get; set; }
    public VoucherSignatoryDto ReceivedBy { get; set; }
    /// <summary>
    /// Checked, Rejected, Pending (waiting for an approver) or NotAssigned (no issue-check workflow
    /// was configured when it was issued); null while the requisition has not been issued.
    /// </summary>
    public string CheckStatus { get; set; }
}
