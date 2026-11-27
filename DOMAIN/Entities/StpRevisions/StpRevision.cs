using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.StpRevisions;

/// <summary>
/// One revision of a standard test procedure as structured content: the header the client prints in the running
/// header, sections 1.0 to 4.0, the reagents block and the 5.N tests with their 5.N.M steps. A revision belongs to the
/// STP number (one number may cover several materials or products). It is prepared as a draft, checked through the
/// document review and approved through the approval engine; approving it supersedes the revision it was revised from.
/// </summary>
public class StpRevision : BaseEntity
{
    /// <summary>Raw material, packaging material or product STP. Chooses the permission keys.</summary>
    public StpRevisionKind Kind { get; set; }
    [StringLength(100)] public string StpNumber { get; set; }
    public int RevisionNumber { get; set; }
    /// <summary>The revision number this one supersedes, as printed under "Supersedes".</summary>
    public int? SupersedesRevision { get; set; }
    /// <summary>The approved revision this one was revised from. Null for the first revision entered.</summary>
    public Guid? RevisedFromId { get; set; }
    public StpRevision RevisedFrom { get; set; }
    public StpRevisionStatus Status { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? SupersededAt { get; set; }
    /// <summary>Raised on every save, so two people saving the same revision cannot overwrite each other.</summary>
    public int Version { get; set; }
    [StringLength(2000)] public string ReasonForChange { get; set; }

    [StringLength(500)] public string Title { get; set; }
    [StringLength(200)] public string Area { get; set; }
    [StringLength(200)] public string Reference { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public DateTime? ReviewDate { get; set; }
    public DateTime? IssueDate { get; set; }

    /// <summary>Where the content came from: an uploaded file name, a document version, or a previous revision.</summary>
    [StringLength(500)] public string Source { get; set; }
    /// <summary>Reason recorded when a revision is deleted.</summary>
    [StringLength(1000)] public string DeletionReason { get; set; }

    public List<StpRevisionSection> Sections { get; set; } = [];
    public List<StpRevisionTest> Tests { get; set; } = [];
    public List<StpRevisionApproval> Approvals { get; set; } = [];
}

public enum StpRevisionKind
{
    RawMaterial = 0,
    PackagingMaterial = 1,
    Product = 2,
}

public enum StpRevisionStatus
{
    /// <summary>Submitted and fixed: in review or awaiting approval.</summary>
    Pending = 0,
    /// <summary>The current approved revision of the STP number. At most one at a time.</summary>
    Approved = 1,
    /// <summary>An approved revision replaced by a later approved revision.</summary>
    Superseded = 2,
    Rejected = 3,
    /// <summary>Being prepared: saved as often as needed and not yet a revision of record.</summary>
    Draft = 4,
}

/// <summary>A numbered prose section (1.0 to 4.0), the reagents block, or the notes that open 5.0 Procedure.</summary>
public class StpRevisionSection : BaseEntity
{
    public Guid StpRevisionId { get; set; }
    public StpRevision StpRevision { get; set; }
    public StpSectionKind Kind { get; set; }
    public int Order { get; set; }
    /// <summary>The section's text. For reagents, one reagent per line.</summary>
    public string Text { get; set; }
}

public enum StpSectionKind
{
    Purpose = 0,
    Scope = 1,
    Responsibility = 2,
    Accountability = 3,
    /// <summary>The reagents listed at the start of 5.0 Procedure.</summary>
    Reagents = 4,
    /// <summary>Unnumbered text between the reagents and the first test (buffer preparation, "refer to SOP" notes).</summary>
    ProcedureNotes = 5,
}

/// <summary>One 5.N test of the procedure. <see cref="TestKey"/> stays the same across revisions.</summary>
public class StpRevisionTest : BaseEntity
{
    public Guid StpRevisionId { get; set; }
    public StpRevision StpRevision { get; set; }
    /// <summary>Stable identifier of the test, carried to every later revision; a specification test cites it.</summary>
    [StringLength(100)] public string TestKey { get; set; }
    /// <summary>The printed number, such as 5.3.</summary>
    [StringLength(20)] public string Number { get; set; }
    [StringLength(500)] public string Title { get; set; }
    public int Order { get; set; }
    public List<StpRevisionTestStep> Steps { get; set; } = [];
}

/// <summary>A 5.N.M step, a conditions block (chromatographic, dissolution) or an unnumbered note inside a test.</summary>
public class StpRevisionTestStep : BaseEntity
{
    public Guid StpRevisionTestId { get; set; }
    public StpRevisionTest StpRevisionTest { get; set; }
    public int Order { get; set; }
    public StpStepKind Kind { get; set; }
    /// <summary>The printed number, such as 5.3.1. Empty for conditions and notes.</summary>
    [StringLength(20)] public string Number { get; set; }
    /// <summary>A conditions block's heading, such as "Chromatographic Conditions".</summary>
    [StringLength(200)] public string Heading { get; set; }
    /// <summary>The step's text. A conditions block keeps one condition line per line.</summary>
    public string Text { get; set; }
    /// <summary>An SOP or STP the step refers to, such as QCD/SOP/003.</summary>
    [StringLength(100)] public string ReferenceCode { get; set; }
}

public enum StpStepKind
{
    Step = 0,
    Conditions = 1,
    Note = 2,
}

public class StpRevisionApproval : ResponsibleApprovalStage
{
    public Guid Id { get; set; }
    public Guid StpRevisionId { get; set; }
    public StpRevision StpRevision { get; set; }
    public Guid ApprovalId { get; set; }
    public Approval Approval { get; set; }
}
