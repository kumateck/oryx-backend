namespace DOMAIN.Entities.QcWorksheets;

// ---------------------------------------------------------------------------
// Worksheet DOCX import (build brief 07, Phase B). Proposals only: nothing here is
// persisted, and nothing is ever approved automatically.
// ---------------------------------------------------------------------------

/// <summary>Where in the source document something came from.</summary>
public class ImportSourceLocation
{
    /// <summary>0-based index into <see cref="ImportSourceDocument.Blocks"/>; null for the running header.</summary>
    public int? Block { get; set; }

    /// <summary>0-based ordinal of the table among the document's (stitched) tables.</summary>
    public int? Table { get; set; }

    public int? Row { get; set; }
    public int? Column { get; set; }
    public bool Header { get; set; }

    public override string ToString() =>
        Header ? "running header"
            : string.Join(" / ", new[]
            {
                Block is null ? null : $"block {Block}",
                Table is null ? null : $"table {Table}",
                Row is null ? null : $"row {Row}",
                Column is null ? null : $"col {Column}"
            }.Where(part => part is not null));
}

public class WorksheetImportFlag
{
    public string Code { get; set; }
    public string Message { get; set; }
    public ImportSourceLocation Location { get; set; }
}

public class ImportFieldProvenance
{
    public string FieldKey { get; set; }

    /// <summary>Set when the provenance is for one column of a Table field.</summary>
    public string ColumnKey { get; set; }

    public ImportSourceLocation Location { get; set; }
    public ImportConfidence Confidence { get; set; }
    public string Reason { get; set; }
}

/// <summary>
/// One proposed field. Mirrors <see cref="CreateWorksheetFieldRequest"/> plus
/// <see cref="Options"/>, which the model gains in Phase A (<c>OptionsJson</c>). Column
/// definitions may carry <c>options</c>, <c>group</c>, <c>fixedValues</c>, <c>rowHeader</c>
/// and <c>formula</c> keys.
/// </summary>
public class ProposedWorksheetField
{
    public int Order { get; set; }
    public string FieldKey { get; set; }
    public string Label { get; set; }
    public WorksheetFieldType Type { get; set; }
    public WorksheetFieldMode Mode { get; set; }
    public string Unit { get; set; }
    public string Analyte { get; set; }
    public string ConstantValue { get; set; }
    public string FormulaExpression { get; set; }
    public string ColumnDefinitions { get; set; }
    public List<string> Options { get; set; }
    public Guid? ReferencedResultSourceTemplateId { get; set; }
    public string ReferencedResultSourceFieldKey { get; set; }
    public string ReferencedResultResolutionFieldKey { get; set; }
}

public class ProposedWorksheetSection
{
    public int Order { get; set; }
    public string Name { get; set; }
    public Guid? InstrumentId { get; set; }

    /// <summary>
    /// Raw-material worksheets (brief 12): the key of the test definition this section was read
    /// with ("assay_titration", "loss_on_drying"); null when no definition matched and the layout
    /// was read instead.
    /// </summary>
    public string TestDefinition { get; set; }

    /// <summary>The definition's alternatives the sheet selected ("back titration", "dried basis (100 − LOD)").</summary>
    public List<string> TestDefinitionVariants { get; set; }

    public List<ProposedWorksheetField> Fields { get; set; } = [];
}

public class ProposedWorksheetTemplate
{
    public string Code { get; set; }
    public string Name { get; set; }
    public string Department { get; set; }
    public WorksheetCategory Category { get; set; }
    public List<ProposedWorksheetSection> Sections { get; set; } = [];
}

public class SamplingPointProposal
{
    public string Code { get; set; }
    public string Name { get; set; }
    public string Area { get; set; }
    public SamplingPointType Type { get; set; }

    /// <summary>The <see cref="SamplingPointGroupProposal"/> whose limit tier applies to this point, if any.</summary>
    public string GroupName { get; set; }

    public ImportSourceLocation Location { get; set; }
}

/// <summary>
/// Points printed under one limit tier ("SP14, SP15, NSP1 … – NMT 80 cfu/mL"). The
/// Specifications phase creates a SamplingPointGroup from it and a characteristic per tier.
/// </summary>
public class SamplingPointGroupProposal
{
    public string Name { get; set; }
    public string AcceptanceCriteria { get; set; }

    /// <summary>The point list as printed, ranges and all ("SP10- SP15, NSP1-NSP15").</summary>
    public string PrintedPoints { get; set; }

    /// <summary>The codes of this sheet's sampling points that fall in the tier, as printed in the point list.</summary>
    public List<string> PointCodes { get; set; } = [];

    public ImportSourceLocation Location { get; set; }
}

/// <summary>
/// A Specification characteristic printed in the sheet. Carried forward to the
/// Specifications phase; the importer never creates a Specification.
/// </summary>
public class SpecificationCharacteristicProposal
{
    public string TestName { get; set; }
    public string Analyte { get; set; }

    /// <summary>
    /// What the limit check compares against. For a choice result it is exactly the compliant
    /// option ("Absence of E.coli"), since qualitative criteria are a normalized exact match.
    /// </summary>
    public string AcceptanceCriteria { get; set; }

    /// <summary>The specification as printed ("Absence of E. coli in 1g of sample"), kept as context.</summary>
    public string PrintedCriteria { get; set; }

    public string AlertLimit { get; set; }

    /// <summary>Where a suggested <see cref="AlertLimit"/> came from, e.g. a completed COA in the same upload.</summary>
    public string AlertLimitSource { get; set; }

    public string ActionLimit { get; set; }
    public string SourceFieldKey { get; set; }

    /// <summary>
    /// Set when the field belongs to an already-saved template (a shared template that exists);
    /// null when it belongs to the template proposed in this upload.
    /// </summary>
    public Guid? SourceWorksheetTemplateId { get; set; }

    public string GroupName { get; set; }

    /// <summary>The manufacturing stage the Specification governs; Finished for product ARDs.</summary>
    public SpecificationStage? Stage { get; set; }

    /// <summary>Context from the running header: the product the Specification is for.</summary>
    public string ProductName { get; set; }

    /// <summary>Context from the running header: the printed Specification number ("Spec. No."), when present.</summary>
    public string SpecificationCode { get; set; }

    /// <summary>The pharmacopoeia or in-house reference printed beside the test (raw-material Specifications: "BP 2025", "USP", "In-House").</summary>
    public string Reference { get; set; }

    public ImportConfidence Confidence { get; set; }
    public ImportSourceLocation Location { get; set; }
}

public class ImportEquipmentMatch
{
    public string FieldKey { get; set; }
    public string PrintedName { get; set; }
    public string PrintedCode { get; set; }

    /// <summary>The matched <c>QcEquipment.Id</c>; null when unmatched.</summary>
    public Guid? EquipmentId { get; set; }

    public string MatchedName { get; set; }
}

public class ImportReagentMatch
{
    public string FieldKey { get; set; }
    public string PrintedName { get; set; }

    /// <summary>The matched <c>Reagent.Id</c>; null when unmatched.</summary>
    public Guid? ReagentId { get; set; }

    public string MatchedName { get; set; }
}

/// <summary>
/// Which medium a culture-media proposal is for, normalized so the old and new form of one
/// medium compare equal ("Plate count Agar" / "Plate Count Agar").
/// </summary>
public class MediumIdentity
{
    /// <summary>The medium name as printed.</summary>
    public string Name { get; set; }

    /// <summary>Lower-case letters and digits of the name only.</summary>
    public string NameKey { get; set; }

    /// <summary>The printed medium code (e.g. QCD/RGT/CA-001); null on the older form.</summary>
    public string Code { get; set; }

    /// <summary>Lower-case letters and digits of the code only; null when there is no code.</summary>
    public string CodeKey { get; set; }
}

/// <summary>
/// Raw-material context (brief 09). A worksheet and its Specification document share the
/// three-digit pairing key NNN (worksheet code RM-NNN, "SPC No.: QCD/SPC/RM/NNN").
/// </summary>
public class RawMaterialDocumentInfo
{
    /// <summary>The three-digit material number, from the file name prefix or the printed Spec./SPC number. The reviewer confirms it.</summary>
    public string PairingKey { get; set; }

    /// <summary>The worksheet template code the pair binds to: "RM-" + <see cref="PairingKey"/>.</summary>
    public string TemplateCode { get; set; }

    /// <summary>The material as printed ("Raw Material Name", or the Specification's title).</summary>
    public string MaterialName { get; set; }

    /// <summary>Specification documents: the SPC number ("QCD/SPC/RM/012").</summary>
    public string SpecificationCode { get; set; }

    /// <summary>Specification documents: the printed "Revision No.".</summary>
    public string Revision { get; set; }

    /// <summary>Specification documents: where the bound worksheet is. Null on a worksheet.</summary>
    public RawMaterialPairingStatus? Pairing { get; set; }

    /// <summary>With <see cref="RawMaterialPairingStatus.InUpload"/>: the worksheet file in this upload.</summary>
    public string PairedFileName { get; set; }

    /// <summary>With <see cref="RawMaterialPairingStatus.ExistingTemplate"/>: the saved template.</summary>
    public Guid? PairedTemplateId { get; set; }

    public string PairedTemplateCode { get; set; }
}

/// <summary>One file's proposal. Writes nothing; the reviewer saves through the normal endpoints.</summary>
public class WorksheetImportProposal
{
    public string FileName { get; set; }
    public ArdFamily Family { get; set; }

    /// <summary>For culture media: "New" or "Superseded".</summary>
    public string FormatVersion { get; set; }

    /// <summary>For culture media: the medium this sheet qualifies, used to find a newer twin.</summary>
    public MediumIdentity Medium { get; set; }

    /// <summary>For families that share one template across files (EM): which template, and where it is proposed.</summary>
    public SharedTemplateReference SharedTemplate { get; set; }

    /// <summary>For the raw-material families: the pairing key and header metadata (brief 09).</summary>
    public RawMaterialDocumentInfo RawMaterial { get; set; }

    /// <summary>Null when the family produces no template (certificates, unknown, pending recognizers, Specification documents).</summary>
    public ProposedWorksheetTemplate Template { get; set; }

    public List<SamplingPointProposal> SamplingPointProposals { get; set; } = [];
    public List<SamplingPointGroupProposal> SamplingPointGroupProposals { get; set; } = [];
    public List<SpecificationCharacteristicProposal> SpecificationProposals { get; set; } = [];
    public List<ImportEquipmentMatch> EquipmentMatches { get; set; } = [];
    public List<ImportReagentMatch> ReagentMatches { get; set; } = [];
    public List<WorksheetImportFlag> Flags { get; set; } = [];
    public List<ImportFieldProvenance> FieldProvenance { get; set; } = [];
    public ImportSourceDocument Source { get; set; }
}
