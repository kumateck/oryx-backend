using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.Reviews;

namespace DOMAIN.Entities.ApprovedVendorLists;

/// <summary>A list's header and lines, for the first list and for corrections while it is in review.</summary>
public class SaveApprovedVendorListRequest
{
    [Required, StringLength(100)] public string AnnexureNumber { get; set; }
    [Range(0, 10000)] public int RevisionNumber { get; set; }
    /// <summary>Entered only on the first list; a revision always supersedes the revision it came from.</summary>
    public int? SupersedesRevision { get; set; }
    /// <summary>The review's version the caller last saw, so a correction cannot overwrite a newer state.</summary>
    public int? ReviewVersion { get; set; }
    public List<SaveApprovedVendorListLineRequest> Lines { get; set; } = [];
}

/// <summary>A new revision of the current approved list.</summary>
public class ReviseApprovedVendorListRequest
{
    [Required, StringLength(100)] public string AnnexureNumber { get; set; }
    [Range(0, 10000)] public int RevisionNumber { get; set; }
    public List<SaveApprovedVendorListLineRequest> Lines { get; set; } = [];
}

/// <summary>
/// One line. Choose an existing manufacturer and supplier, or name a new one: a new name creates the
/// record. A manufacturer or supplier not yet mapped to the material is mapped to it on save.
/// </summary>
public class SaveApprovedVendorListLineRequest
{
    public Guid MaterialId { get; set; }
    public Guid? ManufacturerId { get; set; }
    public NewApprovedManufacturerRequest NewManufacturer { get; set; }
    public Guid? SupplierId { get; set; }
    public NewApprovedSupplierRequest NewSupplier { get; set; }
    /// <summary>Kilograms per pack.</summary>
    public decimal QuantityPerPack { get; set; }
    /// <summary>Month of approval (any day of the month; the month is kept).</summary>
    public DateTime ApprovalDate { get; set; }
    /// <summary>Last month of validity (any day of the month; the month is kept).</summary>
    public DateTime ValidityDate { get; set; }
}

public class NewApprovedManufacturerRequest
{
    [Required, StringLength(100)] public string Name { get; set; }
    [StringLength(1000)] public string Address { get; set; }
}

public class NewApprovedSupplierRequest
{
    [Required, StringLength(100)] public string Name { get; set; }
    [StringLength(1000)] public string Address { get; set; }
    public SupplierType Type { get; set; }
}

public class ApprovedVendorListSummaryDto
{
    public Guid Id { get; set; }
    public string AnnexureNumber { get; set; }
    public int RevisionNumber { get; set; }
    public int? SupersedesRevision { get; set; }
    public ApprovedVendorListStatus Status { get; set; }
    /// <summary>Where a pending list is: AwaitingReview, Returned, or awaiting approval (null review status).</summary>
    public DocumentReviewStatus? ReviewStatus { get; set; }
    public int LineCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string PreparedBy { get; set; }
}

public class ApprovedVendorListDto : ApprovedVendorListSummaryDto
{
    public Guid? PreviousRevisionId { get; set; }
    public DateTime? SupersededAt { get; set; }
    /// <summary>The review's version, sent back with a correction.</summary>
    public int? ReviewVersion { get; set; }
    public List<ApprovedVendorListLineDto> Lines { get; set; } = [];
    public ApprovedVendorListSignatoryDto PreparedBySignatory { get; set; }
    public ApprovedVendorListSignatoryDto CheckedBy { get; set; }
    public ApprovedVendorListSignatoryDto ApprovedBy { get; set; }
}

public class ApprovedVendorListLineDto
{
    public Guid Id { get; set; }
    public int Order { get; set; }
    public Guid MaterialId { get; set; }
    public string MaterialCode { get; set; }
    public string MaterialName { get; set; }
    public Guid ManufacturerId { get; set; }
    public string ManufacturerName { get; set; }
    public string ManufacturerAddress { get; set; }
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; }
    public decimal QuantityPerPack { get; set; }
    public DateTime ApprovalDate { get; set; }
    public DateTime ValidityDate { get; set; }
    /// <summary>True when the validity month has passed.</summary>
    public bool Lapsed { get; set; }
}

/// <summary>A signature line on the list: who, their designation, their signature and when.</summary>
public class ApprovedVendorListSignatoryDto
{
    public Guid UserId { get; set; }
    public string Name { get; set; }
    public string Designation { get; set; }
    public string Signature { get; set; }
    public DateTime? SignedAt { get; set; }
}

/// <summary>
/// What the current approved list allows for one material, for procurement screens to hide or disable
/// choices. An unrestricted material (not on the list) allows every supplier and manufacturer.
/// </summary>
public class ApprovedVendorEligibilityDto
{
    public Guid MaterialId { get; set; }
    public bool Restricted { get; set; }
    public List<ApprovedVendorPairDto> Approved { get; set; } = [];
}

public class ApprovedVendorPairDto
{
    public Guid ManufacturerId { get; set; }
    public Guid SupplierId { get; set; }
    public DateTime ValidityDate { get; set; }
}

/// <summary>Materials whose eligibility is asked for.</summary>
public class ApprovedVendorEligibilityRequest
{
    public List<Guid> MaterialIds { get; set; } = [];
}

/// <summary>A material's mapped manufacturers and, for each, the suppliers mapped to supply it.</summary>
public class ApprovedVendorMappingDto
{
    public Guid ManufacturerId { get; set; }
    public string ManufacturerName { get; set; }
    public string ManufacturerAddress { get; set; }
    /// <summary>The single quantity per pack recorded on the manufacturer's material mapping, when there is one.</summary>
    public decimal? QuantityPerPack { get; set; }
    public List<ApprovedVendorMappedSupplierDto> Suppliers { get; set; } = [];
}

public class ApprovedVendorMappedSupplierDto
{
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; }
}

/** What the list's lines can name: raw materials, manufacturers and suppliers. */
public class ApprovedVendorOptionsDto
{
    public List<ApprovedVendorOptionDto> Materials { get; set; } = [];
    public List<ApprovedVendorOptionDto> Manufacturers { get; set; } = [];
    public List<ApprovedVendorOptionDto> Suppliers { get; set; } = [];
}

public class ApprovedVendorOptionDto
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
}
