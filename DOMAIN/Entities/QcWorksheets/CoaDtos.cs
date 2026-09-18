using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.QcWorksheets;

// ---------------------------------------------------------------------------
// Coa — header rendering
// ---------------------------------------------------------------------------

/// <summary>
/// The part of the header both certificate shapes print.
/// <para>
/// The two shapes are separate types rather than one type with nullable extras, because the
/// difference is a rendering difference and not a data-availability one: an Environmental
/// Monitoring Report does not have a blank Manufacturing Date, it has <b>no</b> Manufacturing
/// Date — there is no batch for one to belong to. A single flattened header carrying nulls would
/// invite a UI to print "Manufacturing Date: —" on a document that should never mention the
/// concept.
/// </para>
/// <para>
/// Polymorphic serialization is declared here so the derived shape survives being returned
/// through a base-typed property, with a <c>shape</c> discriminator the client can switch on.
/// </para>
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "shape")]
[JsonDerivedType(typeof(CertificateOfAnalysisHeaderDto), nameof(CoaCertificateShape.CertificateOfAnalysis))]
[JsonDerivedType(typeof(EnvironmentalMonitoringReportHeaderDto), nameof(CoaCertificateShape.EnvironmentalMonitoringReport))]
public abstract class CoaHeaderDto
{
    public string CertificateCode { get; set; }

    /// <summary>The pinned Specification version, never whichever version is Effective at read time.</summary>
    public string SpecificationCode { get; set; }

    public int SpecificationVersion { get; set; }

    /// <summary>Rendered "Rev N", exactly as the paper header prints it.</summary>
    public string SpecificationRevision { get; set; }

    public DateTime? SampleDate { get; set; }

    public DateTime? TestCompletionDate { get; set; }
}

/// <summary>
/// The Material/Product/Water shape: it releases a batch, so it names one and prints its
/// manufacturing and expiry dates.
/// </summary>
public class CertificateOfAnalysisHeaderDto : CoaHeaderDto
{
    public string ProductOrMaterialName { get; set; }

    /// <summary>
    /// From the round's Subject. Null on the rare multi-Subject Certificate of Analysis, where
    /// each row-group names its own batch instead.
    /// </summary>
    public string BatchNumber { get; set; }

    public DateTime? ManufacturingDate { get; set; }

    public DateTime? ExpiryDate { get; set; }
}

/// <summary>
/// The Routine Environmental shape. It reports on rooms rather than releasing a product, so it
/// declares no batch number and — deliberately — no Manufacturing or Expiry Date property at
/// all. Their absence here is the whole point of the type.
/// </summary>
public class EnvironmentalMonitoringReportHeaderDto : CoaHeaderDto
{
    public string AreaOrRoom { get; set; }
}

// ---------------------------------------------------------------------------
// Coa — reads
// ---------------------------------------------------------------------------

/// <summary>The list row: enough to find a certificate without opening it.</summary>
public class CoaSummaryDto : BaseDto
{
    public Guid TestRequestId { get; set; }
    public string CertificateCode { get; set; }
    public CoaCertificateShape CertificateShape { get; set; }
    public CoaStatus Status { get; set; }
    public int RevisionNumber { get; set; }
    public DateTime? IssuedAt { get; set; }
    public UserDto IssuedBy { get; set; }

    /// <summary>The product/material for a Certificate of Analysis, or the area for a Monitoring Report.</summary>
    public string Subject { get; set; }

    public string ArNumber { get; set; }
    public string SpecificationCode { get; set; }
    public int SpecificationVersion { get; set; }
    public bool OverallComplies { get; set; }

    /// <summary>The certificate this one replaced, if any.</summary>
    public Guid? SupersedesId { get; set; }

    /// <summary>
    /// The certificate that replaced this one, resolved from the far side of
    /// <see cref="SupersedesId"/>. A superseded document must be able to point its reader
    /// forward, which is why a persistent banner can link on from here.
    /// </summary>
    public Guid? SupersededById { get; set; }

    public string SupersededByCertificateCode { get; set; }
}

/// <summary>The rendered certificate: its header, and its rows in print order.</summary>
public class CoaDetailDto : CoaSummaryDto
{
    public CoaHeaderDto Header { get; set; }

    public string SupersedesCertificateCode { get; set; }

    /// <summary>Null on an original; always present on a revision.</summary>
    public string RevisionReason { get; set; }

    /// <summary>
    /// Rows grouped as the document prints them — by Subject, then by <c>GroupName</c>, each
    /// group's rows in <c>DisplayOrder</c>.
    /// </summary>
    public List<CoaSubjectGroupDto> Subjects { get; set; } = [];
}

/// <summary>
/// One Subject's section of a certificate. A Material/Product certificate has exactly one; a
/// Water or Environmental report has one per point or room, matching the real documents.
/// </summary>
public class CoaSubjectGroupDto
{
    public Guid TestRequestSubjectId { get; set; }
    public string SubjectRef { get; set; }
    public string SubjectLabel { get; set; }

    /// <summary>True only when every row in this Subject's section complies.</summary>
    public bool Complies { get; set; }

    public List<CoaRowGroupDto> Groups { get; set; } = [];
}

/// <summary>One <c>GroupName</c> heading — "CHEMICAL", "MICROBIAL" — and the rows beneath it.</summary>
public class CoaRowGroupDto
{
    public string GroupName { get; set; }
    public List<CoaRowDto> Rows { get; set; } = [];
}

/// <summary>One printed line. Every value here was snapshotted at generation time.</summary>
public class CoaRowDto
{
    public Guid Id { get; set; }
    public Guid TestRequestSubjectId { get; set; }
    public Guid SpecificationCharacteristicId { get; set; }

    /// <summary>Which worksheet the result came from — the original, or the retest a disposition accepted.</summary>
    public Guid? SourceWorksheetInstanceId { get; set; }

    public string DisplayLabel { get; set; }
    public string GroupName { get; set; }
    public int DisplayOrder { get; set; }
    public string AcceptanceCriteria { get; set; }
    public string ResultValue { get; set; }
    public bool Complies { get; set; }
}

// ---------------------------------------------------------------------------
// Coa — writes
// ---------------------------------------------------------------------------

/// <summary>
/// Reissues a certificate. The reason is mandatory: a certificate is only ever replaced for a
/// stated cause, and the stated cause is part of the record.
/// </summary>
public class ReviseCoaRequest
{
    [StringLength(1000)] public string Reason { get; set; }
}
