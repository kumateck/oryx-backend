using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Procurement.Manufacturers;
using DOMAIN.Entities.Procurement.Suppliers;

namespace DOMAIN.Entities.ApprovedVendorLists;

/// <summary>
/// One revision of QA's Approved Manufacturer List for Raw Materials (Annexure QAD/SOP/007/A03-06). It is
/// prepared, checked through the document review and approved through the approval engine. Approving a
/// revision supersedes the one it was revised from; only the current approved revision restricts procurement.
/// </summary>
public class ApprovedVendorList : BaseEntity
{
    [StringLength(100)] public string AnnexureNumber { get; set; }
    public int RevisionNumber { get; set; }
    /// <summary>The revision number this one supersedes, as printed under "Supersedes".</summary>
    public int? SupersedesRevision { get; set; }
    /// <summary>The approved revision this one was revised from. Null for the first list entered.</summary>
    public Guid? PreviousRevisionId { get; set; }
    public ApprovedVendorList PreviousRevision { get; set; }
    public ApprovedVendorListStatus Status { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? SupersededAt { get; set; }
    public List<ApprovedVendorListLine> Lines { get; set; } = [];
    public List<ApprovedVendorListApproval> Approvals { get; set; } = [];
}

public enum ApprovedVendorListStatus
{
    /// <summary>Prepared: in review or awaiting approval.</summary>
    Pending = 0,
    /// <summary>The current approved list. At most one at a time.</summary>
    Approved = 1,
    /// <summary>An approved list replaced by a later approved revision.</summary>
    Superseded = 2,
    Rejected = 3,
}

/// <summary>A material approved from one manufacturer through one supplier, for a period given in months.</summary>
public class ApprovedVendorListLine : BaseEntity
{
    public Guid ApprovedVendorListId { get; set; }
    public ApprovedVendorList ApprovedVendorList { get; set; }
    public int Order { get; set; }
    public Guid MaterialId { get; set; }
    public Material Material { get; set; }
    public Guid ManufacturerId { get; set; }
    public Manufacturer Manufacturer { get; set; }
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; }
    /// <summary>Quantity per pack, in kilograms.</summary>
    public decimal QuantityPerPack { get; set; }
    /// <summary>The month of approval, stored as its first day.</summary>
    public DateTime ApprovalDate { get; set; }
    /// <summary>The last month of validity, stored as its first day. The line is valid through that month's last day.</summary>
    public DateTime ValidityDate { get; set; }
}

public class ApprovedVendorListApproval : ResponsibleApprovalStage
{
    public Guid Id { get; set; }
    public Guid ApprovedVendorListId { get; set; }
    public ApprovedVendorList ApprovedVendorList { get; set; }
    public Guid ApprovalId { get; set; }
    public Approval Approval { get; set; }
}
