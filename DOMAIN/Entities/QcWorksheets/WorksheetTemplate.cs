using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.QcWorksheets;

public enum WorksheetCategory
{
    Chemical = 0,
    Microbial = 1,
    MediaQualification = 2
}

/// <summary>
/// Field types, grouped as in field-catalog.md (Structure / Basic / Scientific /
/// Microbiology / Integration).
/// </summary>
public enum WorksheetFieldType
{
    // Basic
    ShortText = 0,
    LongText = 1,
    Number = 2,
    Date = 3,
    Time = 4,
    Select = 5,
    MultiSelect = 6,
    Checkbox = 7,
    Table = 8,

    // Structure
    Instructions = 9,
    Heading = 10,

    // Scientific
    Measurement = 11,
    CalculatedValue = 12,
    Result = 13,
    Instrument = 14,
    Reagent = 15,
    ReferenceStandard = 16,

    // Microbiology
    Organism = 17,
    Dilution = 18,
    IncubationTemperature = 19,
    IncubationPeriod = 20,
    GrowthObservation = 21,
    ColonyCount = 22,
    CfuCalculation = 23,

    // Integration
    ReferencedResult = 24
}

/// <summary>
/// Constant = a fixed method parameter set at template-authoring time; Entry = filled by
/// the analyst at execution; Calculated = derived from a formula.
/// </summary>
public enum WorksheetFieldMode
{
    Constant = 0,
    Entry = 1,
    Calculated = 2
}

/// <summary>
/// A reusable, versioned worksheet definition. Approvals live in the shared
/// <see cref="QcApproval"/> table, looked up by
/// (EntityType = "WorksheetTemplate", EntityId = this Id).
/// </summary>
public class WorksheetTemplate : BaseEntity, IRequireApproval
{
    public bool Approved { get; set; }

    [StringLength(100)] public string Code { get; set; }

    [StringLength(500)] public string Name { get; set; }

    [StringLength(200)] public string Department { get; set; }

    public Guid? StpId { get; set; }

    public StandardTestProcedure Stp { get; set; }

    public WorksheetCategory Category { get; set; }

    public int Version { get; set; } = 1;

    public Guid? SupersedesId { get; set; }

    public WorksheetTemplate Supersedes { get; set; }

    public DateTime? EffectiveDate { get; set; }

    public QcDocumentStatus Status { get; set; } = QcDocumentStatus.Draft;

    public List<WorksheetSection> Sections { get; set; } = [];
}

public class WorksheetSection : BaseEntity
{
    public Guid WorksheetTemplateId { get; set; }

    public WorksheetTemplate WorksheetTemplate { get; set; }

    public int Order { get; set; }

    [StringLength(200)] public string Name { get; set; }

    /// <summary>Optional linked Instrument, per field-catalog.md's Structure &gt; Section entry.</summary>
    public Guid? InstrumentId { get; set; }

    public List<WorksheetField> Fields { get; set; } = [];
}

public class WorksheetField : BaseEntity
{
    public Guid WorksheetSectionId { get; set; }

    public WorksheetSection WorksheetSection { get; set; }

    public int Order { get; set; }

    /// <summary>
    /// Unique within the whole <see cref="WorksheetTemplate"/>, not merely within the
    /// section — CalculatedValue formulas reference FieldKeys worksheet-scoped. Enforced at
    /// the application layer across all sections of the same template.
    /// </summary>
    [StringLength(100)] public string FieldKey { get; set; }

    [StringLength(500)] public string Label { get; set; }

    public WorksheetFieldType Type { get; set; }

    public WorksheetFieldMode Mode { get; set; }

    [StringLength(50)] public string Unit { get; set; }

    /// <summary>Optional multi-active tag.</summary>
    [StringLength(200)] public string Analyte { get; set; }

    /// <summary>Populated only when <see cref="Mode"/> is Constant — the fixed method parameter text/value.</summary>
    public string ConstantValue { get; set; }

    /// <summary>
    /// Populated only when <see cref="Type"/> is CalculatedValue or CfuCalculation.
    /// Syntax: <c>{field_key}</c> scalars, and AVG/SUM/MIN/MAX/RSD over
    /// <c>{table_key.column_key}</c>, with + - * / and parentheses.
    /// </summary>
    public string FormulaExpression { get; set; }

    /// <summary>
    /// Populated only when <see cref="Type"/> is Table; a JSON array of column objects
    /// <c>{ key, label, type, unit, rowHeader?, fixedValues?, group?, options? }</c>. Stored
    /// verbatim, so keys the backend does not interpret survive a round-trip. See
    /// field-catalog.md for the fixed-column contract.
    /// </summary>
    public string ColumnDefinitions { get; set; }

    /// <summary>
    /// The choice list of a Select, MultiSelect or GrowthObservation field: a JSON array of
    /// strings, e.g. <c>["Complies","Does not comply"]</c>. Null for every other type, and for
    /// templates authored before options existed.
    /// </summary>
    public string OptionsJson { get; set; }

    /// <summary>Populated only when <see cref="Type"/> is ReferencedResult.</summary>
    public Guid? ReferencedResultSourceTemplateId { get; set; }

    public WorksheetTemplate ReferencedResultSourceTemplate { get; set; }

    [StringLength(100)] public string ReferencedResultSourceFieldKey { get; set; }

    /// <summary>
    /// The FieldKey elsewhere in this same template (typically a Reagent field) whose
    /// entered value is the runtime lookup key against the source template's instances.
    /// </summary>
    [StringLength(100)] public string ReferencedResultResolutionFieldKey { get; set; }

    public List<WorksheetFieldRevision> Revisions { get; set; } = [];
}

/// <summary>
/// A snapshot of a field's configuration, written on every save while the template is
/// Draft or UnderReview. Mirrors the existing Form/FormField revision pattern: it gives
/// an author field-level change history <i>within</i> one version, separate from the
/// template-level Version/SupersedesId chain that tracks whole-document versions.
/// </summary>
public class WorksheetFieldRevision : BaseEntity
{
    public Guid WorksheetFieldId { get; set; }

    public WorksheetField WorksheetField { get; set; }

    /// <summary>1-based, increments per snapshot of the owning field.</summary>
    public int RevisionNumber { get; set; }

    [StringLength(100)] public string FieldKey { get; set; }

    [StringLength(500)] public string Label { get; set; }

    public WorksheetFieldType Type { get; set; }

    public WorksheetFieldMode Mode { get; set; }

    [StringLength(50)] public string Unit { get; set; }

    [StringLength(200)] public string Analyte { get; set; }

    public string ConstantValue { get; set; }

    public string FormulaExpression { get; set; }

    public string ColumnDefinitions { get; set; }

    public string OptionsJson { get; set; }

    public Guid? ReferencedResultSourceTemplateId { get; set; }

    [StringLength(100)] public string ReferencedResultSourceFieldKey { get; set; }

    [StringLength(100)] public string ReferencedResultResolutionFieldKey { get; set; }

    public int Order { get; set; }
}
