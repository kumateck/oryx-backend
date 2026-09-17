using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.QcWorksheets;

/// <summary>
/// What a <see cref="Specification"/> governs.
/// <para>
/// Material is deliberately split into <see cref="RawMaterial"/> and
/// <see cref="PackagingMaterial"/> rather than unified, matching the live system's existing
/// separation (<c>quality-control.ts</c> keys <c>rawMaterialSpecifications</c> and
/// <c>packagingMaterialSpecifications</c> separately throughout). A unified "Material"
/// would have been a regression from what is already established.
/// </para>
/// </summary>
public enum SpecificationAppliesTo
{
    RawMaterial = 0,
    PackagingMaterial = 1,
    Product = 2,
    RoutineWater = 3,
    RoutineEnvironmental = 4
}

/// <summary>
/// The manufacturing stage a Product Specification governs. Carries forward
/// <c>AnalyticalTestRequest.Stage</c> / <c>ProductSpecification.TestStage</c> from the live
/// system.
/// <para>
/// A product's Intermediate/Bulk/Finished Specifications are genuinely different documents
/// with different Characteristics, not tiers of one Specification — Intermediate often skips
/// Dissolution and Microbial entirely, which Finished always requires.
/// </para>
/// <para>
/// A BMR/BPR survey of 8 real products spanning every dosage form manufactured found each
/// defines exactly two QA-sampling/QC-testing gates: one mid-process gate plus a Finished
/// gate at packaging. Which mid-process gate appears follows from manufacturing method —
/// granulation-based solids gate on <see cref="Intermediate"/>, liquid-mix and semi-solid
/// forms gate on <see cref="Bulk"/> — so a product pairs Finished with exactly one of the
/// two, never both and never all three. That pairing is a seeding/UX default rather than a
/// schema rule (see <c>docs/qc-rebuild/domain-model.md</c>): the enum stays three-valued
/// because a future process could genuinely need all three, and a Specification carries no
/// product FK to enforce a per-product pairing against in the first place.
/// </para>
/// </summary>
public enum SpecificationStage
{
    Intermediate = 0,
    Bulk = 1,
    Finished = 2
}

/// <summary>
/// Whether an OOS retest reuses the same sample or mandates a fresh one. Configurable per
/// Specification because it genuinely varies by test type: chemical assay retests are often
/// same-sample, microbial often needs a fresh sample.
/// <para>
/// Numbering starts at 1 on purpose. The brief requires this field with <b>no default</b>,
/// so the CLR/database zero value is left unassigned and means "never set" — a Specification
/// that somehow reached storage without an explicit policy is detectable rather than
/// silently reading as <see cref="SameSample"/>.
/// </para>
/// </summary>
public enum QcRetestPolicy
{
    SameSample = 1,
    FreshResample = 2
}

/// <summary>Which of a Specification's two possible worksheet tracks a link supplies.</summary>
public enum SpecificationAnalysisType
{
    Chemical = 0,
    Microbial = 1
}

/// <summary>
/// Master data naming a group of sampling points that share one Alert/Action tier — e.g.
/// "General Rooms" (Alert 80 / Action 100 CFU/4Hrs) vs. "Dispensing Booth" (Alert 3 /
/// Action 5).
/// <para>
/// Introduced in Milestone 2 because <see cref="SpecificationCharacteristic"/> needs it
/// before <c>MonitoringProgram</c> (Milestone 6) exists; Milestone 6 adds an FK to this same
/// table rather than duplicating the concept.
/// </para>
/// <para>
/// Always selected from this table, never free text: a typo would silently apply the wrong
/// limit tier, which is the single failure mode this entity exists to prevent.
/// </para>
/// </summary>
public class SamplingPointGroup : BaseEntity
{
    [StringLength(200)] public string Name { get; set; }

    public string Description { get; set; }
}

/// <summary>
/// A controlled Specification document in the rebuilt QC module: the acceptance criteria a
/// <c>TestRequest</c>'s results are judged against.
/// <para>
/// Entirely separate from, and coexisting with, the live <c>MaterialSpecification</c>/
/// <c>ProductSpecification</c> tables, which this does not read, write, or modify.
/// </para>
/// <para>
/// Approvals are not held as a child collection: they live in the shared
/// <see cref="QcApproval"/> table, looked up by
/// (EntityType = "Specification", EntityId = this Id).
/// </para>
/// </summary>
public class Specification : BaseEntity, IRequireApproval
{
    /// <summary>From <see cref="IRequireApproval"/>; set true once every required approval stage is approved.</summary>
    public bool Approved { get; set; }

    [StringLength(100)] public string Code { get; set; }

    [StringLength(500)] public string Name { get; set; }

    public SpecificationAppliesTo AppliesTo { get; set; }

    /// <summary>
    /// Required when <see cref="AppliesTo"/> is <see cref="SpecificationAppliesTo.Product"/>,
    /// and null for every other value — Stage is Product-only, not merely optional elsewhere.
    /// Enforced in <c>SpecificationRepository</c>.
    /// </summary>
    public SpecificationStage? Stage { get; set; }

    public int Version { get; set; } = 1;

    /// <summary>Self-referencing: the version this one was created from.</summary>
    public Guid? SupersedesId { get; set; }

    public Specification Supersedes { get; set; }

    public DateTime? EffectiveDate { get; set; }

    public QcDocumentStatus Status { get; set; } = QcDocumentStatus.Draft;

    /// <summary>
    /// Required, with no default. Milestone 4's OOS retest flow reads this to decide whether
    /// a retest reuses the same TestRequestSubject or creates a new one, so there is nothing
    /// safe to silently fall back to.
    /// </summary>
    public QcRetestPolicy RetestPolicy { get; set; }

    public List<SpecificationWorksheetLink> WorksheetLinks { get; set; } = [];

    public List<SpecificationCharacteristic> Characteristics { get; set; } = [];
}

/// <summary>
/// Binds a Specification to the worksheet template that supplies one analysis track's fields.
/// <para>
/// At most one Chemical and one Microbial link per Specification, and
/// <see cref="SpecificationAppliesTo.RoutineEnvironmental"/> only ever has a Microbial link.
/// Both are application-layer rules rather than database constraints, because expressing
/// "at most one row per (parent, enum value)" as a unique index would also forbid the
/// perfectly legitimate act of replacing a link during a Draft edit.
/// </para>
/// </summary>
public class SpecificationWorksheetLink : BaseEntity
{
    public Guid SpecificationId { get; set; }

    public Specification Specification { get; set; }

    public Guid WorksheetTemplateId { get; set; }

    public WorksheetTemplate WorksheetTemplate { get; set; }

    public SpecificationAnalysisType AnalysisType { get; set; }
}

/// <summary>
/// One acceptance-criteria row: a test on this Specification, bound to the worksheet field
/// whose submitted value it judges.
/// <para>
/// The same <see cref="SourceWorksheetTemplateId"/>/<see cref="SourceFieldKey"/> pair may
/// legitimately appear on more than one row of the same Specification — that is how one EM
/// test (e.g. Airborne Viables) carries different Alert/Action tiers per
/// <see cref="SamplingPointGroupId"/> while sourcing a single worksheet field.
/// </para>
/// </summary>
public class SpecificationCharacteristic : BaseEntity
{
    public Guid SpecificationId { get; set; }

    public Specification Specification { get; set; }

    [StringLength(200)] public string TestName { get; set; }

    [StringLength(200)] public string Analyte { get; set; }

    /// <summary>Free text, e.g. "95.0-105.0%" or "Absence of E. coli in 1g".</summary>
    [StringLength(2000)] public string AcceptanceCriteria { get; set; }

    /// <summary>
    /// Text, not a number: "NMT 100 CFU/4Hrs" and "Absence of E. coli" are both valid limits.
    /// Parsing a limit into something comparable is Milestone 4's concern; this milestone
    /// only stores it. A breach flags for trend review without blocking the COA.
    /// </summary>
    [StringLength(500)] public string AlertLimit { get; set; }

    /// <summary>Text, for the same reason as <see cref="AlertLimit"/>. A breach triggers the formal OOS workflow.</summary>
    [StringLength(500)] public string ActionLimit { get; set; }

    public Guid? SamplingPointGroupId { get; set; }

    public SamplingPointGroup SamplingPointGroup { get; set; }

    /// <summary>
    /// Must be one of the owning Specification's own <see cref="Specification.WorksheetLinks"/>
    /// — validated at the application layer, since referential integrity alone would happily
    /// accept any worksheet template in the system.
    /// </summary>
    public Guid SourceWorksheetTemplateId { get; set; }

    public WorksheetTemplate SourceWorksheetTemplate { get; set; }

    /// <summary>Must exist on the source template's current Effective version.</summary>
    [StringLength(100)] public string SourceFieldKey { get; set; }

    public bool IncludeOnCoa { get; set; } = true;

    public int DisplayOrder { get; set; }

    /// <summary>COA section heading, e.g. "CHEMICAL" / "MICROBIAL".</summary>
    [StringLength(200)] public string GroupName { get; set; }
}
