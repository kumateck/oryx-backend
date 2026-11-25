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
    /// <summary>Raised on every draft save, so two people saving the same draft cannot overwrite each other.</summary>
    public int DraftVersion { get; set; }
    /// <summary>Reason recorded when a revision is deleted.</summary>
    [StringLength(1000)] public string DeletionReason { get; set; }
    public List<ApprovedVendorListLine> Lines { get; set; } = [];
    public List<ApprovedVendorListApproval> Approvals { get; set; } = [];
}

public enum ApprovedVendorListStatus
{
    /// <summary>Submitted and fixed: in review or awaiting approval.</summary>
    Pending = 0,
    /// <summary>The current approved list. At most one at a time.</summary>
    Approved = 1,
    /// <summary>An approved list replaced by a later approved revision.</summary>
    Superseded = 2,
    Rejected = 3,
    /// <summary>
    /// Being prepared: saved as often as needed and not yet a revision of record. It restricts nothing, and
    /// the manufacturers and suppliers it names as new are created only when it is submitted.
    /// </summary>
    Draft = 4,
}

/// <summary>A material approved from one manufacturer through one supplier, for a period given in months.</summary>
public class ApprovedVendorListLine : BaseEntity
{
    public Guid ApprovedVendorListId { get; set; }
    public ApprovedVendorList ApprovedVendorList { get; set; }
    public int Order { get; set; }
    public Guid MaterialId { get; set; }
    public Material Material { get; set; }
    /// <summary>Null only on a draft line that names a new manufacturer.</summary>
    public Guid? ManufacturerId { get; set; }
    public Manufacturer Manufacturer { get; set; }
    /// <summary>Null only on a draft line that names a new supplier.</summary>
    public Guid? SupplierId { get; set; }
    public Supplier Supplier { get; set; }
    /// <summary>A manufacturer named on a draft, created when the draft is submitted.</summary>
    [StringLength(100)] public string NewManufacturerName { get; set; }
    [StringLength(1000)] public string NewManufacturerAddress { get; set; }
    /// <summary>A supplier named on a draft, created when the draft is submitted.</summary>
    [StringLength(100)] public string NewSupplierName { get; set; }
    [StringLength(1000)] public string NewSupplierAddress { get; set; }
    public SupplierType? NewSupplierType { get; set; }
    /// <summary>Quantity per pack, in kilograms.</summary>
    public decimal QuantityPerPack { get; set; }
    /// <summary>The month of approval, stored as its first day.</summary>
    public DateTime ApprovalDate { get; set; }
    /// <summary>The validity month, stored as its first day. Procurement can no longer use the line from that day.</summary>
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
