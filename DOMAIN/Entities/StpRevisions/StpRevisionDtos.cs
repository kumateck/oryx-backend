using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Reviews;

namespace DOMAIN.Entities.StpRevisions;

/// <summary>A revision's header and content: everything a draft save or a correction replaces.</summary>
public class StpRevisionContent
{
    [Range(0, 10000)] public int RevisionNumber { get; set; }
    /// <summary>Entered only on the first revision; a later revision always supersedes the revision it came from.</summary>
    public int? SupersedesRevision { get; set; }
    [StringLength(2000)] public string ReasonForChange { get; set; }
    [StringLength(500)] public string Title { get; set; }
    [StringLength(200)] public string Area { get; set; }
    [StringLength(200)] public string Reference { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public DateTime? ReviewDate { get; set; }
    public DateTime? IssueDate { get; set; }
    /// <summary>Where the content came from (an imported file or document version). Kept when omitted.</summary>
    [StringLength(500)] public string Source { get; set; }
    public List<StpSectionRequest> Sections { get; set; } = [];
    public List<StpTestRequest> Tests { get; set; } = [];
}

public class StpSectionRequest
{
    public StpSectionKind Kind { get; set; }
    public string Text { get; set; }
}

public class StpTestRequest
{
    /// <summary>The test's stable key. Leave empty for a new test; keep the key of an existing one.</summary>
    [StringLength(100)] public string TestKey { get; set; }
    [StringLength(20)] public string Number { get; set; }
    [StringLength(500)] public string Title { get; set; }
    public List<StpStepRequest> Steps { get; set; } = [];
}

public class StpStepRequest
{
    public StpStepKind Kind { get; set; }
    [StringLength(20)] public string Number { get; set; }
    [StringLength(200)] public string Heading { get; set; }
    public string Text { get; set; }
    [StringLength(100)] public string ReferenceCode { get; set; }
}

/// <summary>Starts the first revision of an STP number as a draft, blank or from an import proposal.</summary>
public class CreateStpRevisionRequest : StpRevisionContent
{
    public StpRevisionKind Kind { get; set; }
    [Required, StringLength(100)] public string StpNumber { get; set; }
}

/// <summary>Starts a new revision of the STP number's approved revision, copying its content.</summary>
public class ReviseStpRevisionRequest
{
    public StpRevisionKind Kind { get; set; }
    [Required, StringLength(100)] public string StpNumber { get; set; }
    [Range(0, 10000)] public int RevisionNumber { get; set; }
    [Required, StringLength(2000)] public string ReasonForChange { get; set; }
}

/// <summary>A draft's content, saved as often as needed.</summary>
public class SaveStpRevisionDraftRequest : StpRevisionContent
{
    /// <summary>The version the caller last saw, so a save cannot overwrite a newer one.</summary>
    public int? Version { get; set; }
}

/// <summary>A correction while the revision is in review (awaiting review or returned).</summary>
public class UpdateStpRevisionRequest : StpRevisionContent
{
    public int? Version { get; set; }
}

public class SubmitStpRevisionRequest
{
    public int? Version { get; set; }
}

/// <summary>Reads the current version of the STP's controlled Word document (the ONLYOFFICE document) as an import.</summary>
public class ImportStpDocumentRequest
{
    public StpRevisionKind Kind { get; set; }
    /// <summary>The material or product STP record whose document is read.</summary>
    public Guid StpId { get; set; }
}

public class StpRevisionSummaryDto
{
    public Guid Id { get; set; }
    public StpRevisionKind Kind { get; set; }
    public string StpNumber { get; set; }
    public int RevisionNumber { get; set; }
    public int? SupersedesRevision { get; set; }
    public StpRevisionStatus Status { get; set; }
    /// <summary>Awaiting review or returned, while the revision is pending; null otherwise.</summary>
    public DocumentReviewStatus? ReviewStatus { get; set; }
    public string Title { get; set; }
    public int TestCount { get; set; }
    public string PreparedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? SupersededAt { get; set; }
}

public class StpRevisionDto : StpRevisionSummaryDto
{
    public Guid? RevisedFromId { get; set; }
    public string ReasonForChange { get; set; }
    public string Area { get; set; }
    public string Reference { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public DateTime? ReviewDate { get; set; }
    public DateTime? IssueDate { get; set; }
    public string Source { get; set; }
    public int Version { get; set; }
    public int? ReviewVersion { get; set; }
    public StpRevisionSignatoryDto PreparedBySignatory { get; set; }
    public StpRevisionSignatoryDto CheckedBy { get; set; }
    public StpRevisionSignatoryDto ApprovedBy { get; set; }
    public List<StpSectionDto> Sections { get; set; } = [];
    public List<StpTestDto> Tests { get; set; } = [];
}

public class StpSectionDto
{
    public Guid Id { get; set; }
    public StpSectionKind Kind { get; set; }
    public int Order { get; set; }
    public string Text { get; set; }
}

public class StpTestDto
{
    public Guid Id { get; set; }
    public string TestKey { get; set; }
    public string Number { get; set; }
    public string Title { get; set; }
    public int Order { get; set; }
    public List<StpStepDto> Steps { get; set; } = [];
}

public class StpStepDto
{
    public Guid Id { get; set; }
    public int Order { get; set; }
    public StpStepKind Kind { get; set; }
    public string Number { get; set; }
    public string Heading { get; set; }
    public string Text { get; set; }
    public string ReferenceCode { get; set; }
}

public class StpRevisionSignatoryDto
{
    public Guid UserId { get; set; }
    public string Name { get; set; }
    public string Designation { get; set; }
    public string Signature { get; set; }
    public DateTime? SignedAt { get; set; }
}

/// <summary>The STP numbers of a kind, each with its current and in-preparation revisions.</summary>
public class StpNumberOverviewDto
{
    public string StpNumber { get; set; }
    public StpRevisionSummaryDto Current { get; set; }
    public StpRevisionSummaryDto InPreparation { get; set; }
    public int RevisionCount { get; set; }
}

public enum StpImportConfidence
{
    /// <summary>Read from the expected place in the expected form.</summary>
    High = 0,
    /// <summary>Read, but inferred from layout or wording; check it.</summary>
    Medium = 1,
    /// <summary>Missing or guessed; it must be entered or corrected.</summary>
    Low = 2,
}

/// <summary>One header value of an import proposal.</summary>
public class StpImportField
{
    public string Value { get; set; }
    public StpImportConfidence Confidence { get; set; }
    /// <summary>Why the value needs checking; null when it does not.</summary>
    public string Flag { get; set; }
}

public class StpImportSection
{
    public StpSectionKind Kind { get; set; }
    public string Text { get; set; }
    public StpImportConfidence Confidence { get; set; }
    public string Flag { get; set; }
}

public class StpImportTest
{
    /// <summary>A suggested stable key; an existing approved revision's key is reused when the title matches.</summary>
    public string TestKey { get; set; }
    public string Number { get; set; }
    public string Title { get; set; }
    public StpImportConfidence Confidence { get; set; }
    public string Flag { get; set; }
    public List<StpImportStep> Steps { get; set; } = [];
}

public class StpImportStep
{
    public StpStepKind Kind { get; set; }
    public string Number { get; set; }
    public string Heading { get; set; }
    public string Text { get; set; }
    public string ReferenceCode { get; set; }
    public StpImportConfidence Confidence { get; set; }
    public string Flag { get; set; }
}

/// <summary>
/// What a Word STP was read as. Nothing is saved: the person reviews the flagged values and creates or saves a
/// revision from it.
/// </summary>
public class StpImportProposalDto
{
    public bool Succeeded { get; set; }
    public string FailureReason { get; set; }
    public string FileName { get; set; }
    /// <summary>What to record as the revision's source if this proposal is used.</summary>
    public string Source { get; set; }
    public StpRevisionKind Kind { get; set; }
    public StpImportField StpNumber { get; set; }
    public StpImportField Title { get; set; }
    public StpImportField RevisionNumber { get; set; }
    public StpImportField SupersedesRevision { get; set; }
    public StpImportField Area { get; set; }
    public StpImportField Reference { get; set; }
    public StpImportField EffectiveDate { get; set; }
    public StpImportField ReviewDate { get; set; }
    public StpImportField IssueDate { get; set; }
    public List<StpImportSection> Sections { get; set; } = [];
    public List<StpImportTest> Tests { get; set; } = [];
    /// <summary>Document-level notes: text that was not placed, layout surprises.</summary>
    public List<string> Warnings { get; set; } = [];
    /// <summary>Count of values at Medium or Low confidence.</summary>
    public int FlaggedCount { get; set; }
}
