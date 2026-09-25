using SHARED;

namespace DOMAIN.Entities.QcWorksheets;

public static class QcWorksheetErrors
{
    public static Error StpNotFound(Guid id) =>
        Error.NotFound("QcStp.NotFound", $"The standard test procedure with the Id: {id} was not found");

    public static Error TemplateNotFound(Guid id) =>
        Error.NotFound("QcWorksheetTemplate.NotFound", $"The worksheet template with the Id: {id} was not found");

    public static Error ReferencedStpNotFound(Guid id) =>
        Error.Validation("QcStp.ReferencedStpNotFound", $"The referenced standard test procedure with the Id: {id} was not found");

    public static Error DuplicateCode(string code) =>
        Error.Conflict("QcWorksheet.DuplicateCode", $"A record with the code '{code}' already exists");

    /// <summary>
    /// The edit-triggers-versioning rule, enforced server-side. Client-side read-only
    /// rendering is never trusted on its own.
    /// </summary>
    public static Error NotEditableInStatus(QcDocumentStatus status) =>
        Error.Validation(
            "QcWorksheet.NotEditable",
            $"This document is {status} and cannot be edited directly. Create a new version to make changes.");

    public static Error NewVersionRequiresEffective(QcDocumentStatus status) =>
        Error.Validation(
            "QcWorksheet.NewVersionRequiresEffective",
            $"A new version can only be created from an Effective document. This document is {status}.");

    public static Error SubmitRequiresDraft(QcDocumentStatus status) =>
        Error.Validation(
            "QcWorksheet.SubmitRequiresDraft",
            $"Only a Draft document can be submitted for review. This document is {status}.");

    public static Error ApproveRequiresUnderReview(QcDocumentStatus status) =>
        Error.Validation(
            "QcWorksheet.ApproveRequiresUnderReview",
            $"Only a document under review can be approved or rejected. This document is {status}.");

    public static Error MakeEffectiveRequiresApproved(QcDocumentStatus status) =>
        Error.Validation(
            "QcWorksheet.MakeEffectiveRequiresApproved",
            $"Only an Approved document can be made effective. This document is {status}.");

    public static Error SupersedeRequiresEffective(QcDocumentStatus status) =>
        Error.Validation(
            "QcWorksheet.SupersedeRequiresEffective",
            $"Only an Effective document can be superseded. This document is {status}.");

    public static Error ReasonForChangeRequired =>
        Error.Validation("QcWorksheet.ReasonForChangeRequired", "A reason for change is required");

    /// <summary>
    /// The QC-specific opt-out of the approval engine's silent auto-approval fallback.
    /// Locked as a compliance requirement: a QC document must never become approved
    /// without an identified approver and a re-authenticated signature.
    /// </summary>
    public static Error NoApprovalWorkflowConfigured(string modelType) =>
        Error.Validation(
            "QcWorksheet.NoApprovalWorkflowConfigured",
            $"No approval workflow is configured for '{modelType}'. A QC document cannot be "
            + "submitted for review until an administrator defines its approval stages, because "
            + "QC documents may never be automatically approved without a signed approval.");

    public static Error ReauthenticationFailed =>
        Error.Forbidden(
            "QcWorksheet.ReauthenticationFailed",
            "The password you entered is incorrect. An electronic signature requires you to re-authenticate.");

    public static Error ReauthenticationRequired =>
        Error.Validation(
            "QcWorksheet.ReauthenticationRequired",
            "Your password is required to electronically sign this action.");

    public static Error DuplicateFieldKey(string fieldKey) =>
        Error.Validation(
            "QcWorksheetTemplate.DuplicateFieldKey",
            $"The field key '{fieldKey}' is used more than once in this template. Field keys must be "
            + "unique across every section of a worksheet template, because formulas reference them "
            + "worksheet-scoped.");

    public static Error InvalidFormula(string fieldKey, string reason) =>
        Error.Validation(
            "QcWorksheetTemplate.InvalidFormula",
            $"The formula on field '{fieldKey}' is invalid: {reason}");

    public static Error ConstantValueRequired(string fieldKey) =>
        Error.Validation(
            "QcWorksheetTemplate.ConstantValueRequired",
            $"The constant field '{fieldKey}' requires a value.");

    /// <summary>
    /// A choice field with fewer than two real choices cannot be answered by choosing.
    /// <paramref name="fieldKey"/> is a FieldKey, or <c>tableKey.columnKey</c> for a table column.
    /// </summary>
    public static Error OptionsRequired(string fieldKey) =>
        Error.Validation(
            "QcWorksheetTemplate.OptionsRequired",
            $"The field '{fieldKey}' requires at least two distinct, non-blank options, given as a "
            + "JSON array of strings.");

    public static Error OptionsNotAllowed(string fieldKey, string type) =>
        Error.Validation(
            "QcWorksheetTemplate.OptionsNotAllowed",
            $"The field '{fieldKey}' is {type} and cannot carry options. Only Select, MultiSelect "
            + "and GrowthObservation fields (and table columns of those types) have a choice list.");

    /// <summary>
    /// A table's fixed columns together define its row count, so they must agree, and a table
    /// is either wholly fixed-row or wholly open-ended.
    /// </summary>
    public static Error FixedRowCountMismatch(string fieldKey, string reason) =>
        Error.Validation(
            "QcWorksheetTemplate.FixedRowCountMismatch",
            $"Table '{fieldKey}' has an inconsistent fixed-row layout: {reason}");

    public static Error TemplateHasNoSections =>
        Error.Validation("QcWorksheetTemplate.NoSections", "A worksheet template must have at least one section");

    // -----------------------------------------------------------------------
    // Milestone 2 — Specification and SamplingPointGroup
    // -----------------------------------------------------------------------

    public static Error SpecificationNotFound(Guid id) =>
        Error.NotFound("QcSpecification.NotFound", $"The specification with the Id: {id} was not found");

    public static Error SamplingPointGroupNotFound(Guid id) =>
        Error.NotFound("QcSamplingPointGroup.NotFound", $"The sampling point group with the Id: {id} was not found");

    public static Error DuplicateSamplingPointGroupName(string name) =>
        Error.Conflict(
            "QcSamplingPointGroup.DuplicateName",
            $"A sampling point group named '{name}' already exists");

    /// <summary>
    /// The group is chosen from a dropdown precisely so the wrong Alert/Action tier can
    /// never be applied by a typo; an id that resolves to nothing is that same failure.
    /// </summary>
    public static Error SamplingPointGroupNotFoundOnCharacteristic(Guid id) =>
        Error.Validation(
            "QcSpecification.SamplingPointGroupNotFound",
            $"The sampling point group with the Id: {id} was not found. A characteristic's sampling "
            + "point group must be chosen from the existing groups.");

    public static Error SamplingPointGroupInUse(string name, int characteristicCount) =>
        Error.Validation(
            "QcSamplingPointGroup.InUse",
            $"'{name}' is used by {characteristicCount} specification characteristic(s) and cannot be "
            + "deleted. Detach it from those characteristics first, because deleting it would leave "
            + "them without the Alert/Action tier they are grouped by.");

    public static Error DuplicateAnalysisTypeLink(SpecificationAnalysisType analysisType) =>
        Error.Validation(
            "QcSpecification.DuplicateAnalysisTypeLink",
            $"This specification already links a {analysisType} worksheet template. A specification "
            + "may link at most one Chemical and one Microbial template.");

    public static Error EnvironmentalIsMicrobialOnly =>
        Error.Validation(
            "QcSpecification.EnvironmentalIsMicrobialOnly",
            "A Routine Environmental specification may only link a Microbial worksheet template.");

    public static Error LinkedTemplateNotFound(Guid id) =>
        Error.Validation(
            "QcSpecification.LinkedTemplateNotFound",
            $"The worksheet template with the Id: {id} was not found");

    public static Error CharacteristicTemplateNotLinked(Guid templateId) =>
        Error.Validation(
            "QcSpecification.CharacteristicTemplateNotLinked",
            $"A characteristic sources the worksheet template {templateId}, which is not one of this "
            + "specification's own worksheet links. A characteristic may only source a field from a "
            + "template this specification links.");

    public static Error CharacteristicFieldKeyNotFound(string fieldKey, string templateCode, int version) =>
        Error.Validation(
            "QcSpecification.CharacteristicFieldKeyNotFound",
            $"The field key '{fieldKey}' does not exist on worksheet template '{templateCode}' "
            + $"version {version}.");

    public static Error StageRequiredForProduct =>
        Error.Validation(
            "QcSpecification.StageRequired",
            "A stage (Intermediate, Bulk or Finished) is required for a Product specification, "
            + "because a product's stages are genuinely different documents with different "
            + "characteristics rather than tiers of one specification.");

    public static Error StageIsProductOnly(SpecificationAppliesTo appliesTo) =>
        Error.Validation(
            "QcSpecification.StageNotApplicable",
            $"A stage cannot be set on a {appliesTo} specification. Stage applies only to Product "
            + "specifications.");

    /// <summary>
    /// Required with no default: Milestone 4's OOS retest flow reads this to decide whether a
    /// retest reuses the same sample, so there is nothing safe to fall back to.
    /// </summary>
    public static Error RetestPolicyRequired =>
        Error.Validation(
            "QcSpecification.RetestPolicyRequired",
            "A retest policy (Same Sample or Fresh Resample) is required. There is no default: "
            + "whether a retest may reuse the same sample must be declared explicitly.");

    public static Error AppliesToRequired =>
        Error.Validation(
            "QcSpecification.AppliesToRequired",
            "A specification must declare what it applies to.");

    // -----------------------------------------------------------------------
    // Milestone 3 — TestRequest and WorksheetInstance
    // -----------------------------------------------------------------------

    public static Error TestRequestNotFound(Guid id) =>
        Error.NotFound("QcTestRequest.NotFound", $"The test request with the Id: {id} was not found");

    public static Error TestRequestSubjectNotFound(Guid id) =>
        Error.NotFound("QcTestRequest.SubjectNotFound", $"The test request subject with the Id: {id} was not found");

    public static Error WorksheetInstanceNotFound(Guid id) =>
        Error.NotFound("QcWorksheetInstance.NotFound", $"The worksheet with the Id: {id} was not found");

    public static Error TestRequestSpecificationNotFound(Guid id) =>
        Error.Validation(
            "QcTestRequest.SpecificationNotFound",
            $"The specification with the Id: {id} was not found");

    /// <summary>
    /// A round is real testing against a controlled document. An unapproved Specification has
    /// no acceptance criteria anyone has signed for, so it cannot govern results.
    /// </summary>
    public static Error SpecificationNotEffective(string code, QcDocumentStatus status) =>
        Error.Validation(
            "QcTestRequest.SpecificationNotEffective",
            $"Specification '{code}' is {status}. A test request can only be raised against an "
            + "Effective specification.");

    public static Error SpecificationTypeMismatch(
        TestRequestType type, SpecificationAppliesTo appliesTo) =>
        Error.Validation(
            "QcTestRequest.SpecificationTypeMismatch",
            $"A {type} test request cannot run against a {appliesTo} specification. The round's "
            + "type and its specification must agree.");

    public static Error SpecificationHasNoWorksheetLinks(string code) =>
        Error.Validation(
            "QcTestRequest.SpecificationHasNoWorksheetLinks",
            $"Specification '{code}' links no worksheet templates, so there is nothing to test. "
            + "Add a Chemical or Microbial worksheet link to it first.");

    public static Error UnscheduledReasonRequired =>
        Error.Validation(
            "QcTestRequest.UnscheduledReasonRequired",
            "An unscheduled test request must state why it was raised.");

    public static Error UnscheduledReasonNotApplicable =>
        Error.Validation(
            "QcTestRequest.UnscheduledReasonNotApplicable",
            "A scheduled test request cannot carry an unscheduled reason.");

    public static Error TestRequestHasNoSubjects =>
        Error.Validation(
            "QcTestRequest.NoSubjects",
            "A test request must cover at least one subject — a batch, or a sampling point.");

    public static Error SubjectsLockedAfterSampling(TestRequestStatus status) =>
        Error.Validation(
            "QcTestRequest.SubjectsLocked",
            $"Subjects can only be added while a test request is Draft or Sampled. This request "
            + $"is {status}.");

    public static Error RecordSampleRequiresDraft(TestRequestStatus status) =>
        Error.Validation(
            "QcTestRequest.RecordSampleRequiresDraft",
            $"Only a Draft test request can have its sample collection recorded. This request is "
            + $"{status}.");

    public static Error SamplingPointGroupIsRoutineOnly(TestRequestType type) =>
        Error.Validation(
            "QcTestRequest.SamplingPointGroupNotApplicable",
            $"A sampling point group cannot be set on a {type} subject. It applies only to "
            + "Routine Water and Routine Environmental rounds.");

    public static Error MaterialBatchIsMaterialOnly(TestRequestType type) =>
        Error.Validation(
            "QcTestRequest.MaterialBatchNotApplicable",
            $"A material batch cannot be set on a {type} subject.");

    public static Error BatchManufacturingRecordIsProductOnly(TestRequestType type) =>
        Error.Validation(
            "QcTestRequest.BatchManufacturingRecordNotApplicable",
            $"A batch manufacturing record cannot be set on a {type} subject.");

    public static Error MaterialBatchNotFound(Guid id) =>
        Error.Validation("QcTestRequest.MaterialBatchNotFound", $"The material batch with the Id: {id} was not found");

    public static Error BatchManufacturingRecordNotFound(Guid id) =>
        Error.Validation(
            "QcTestRequest.BatchManufacturingRecordNotFound",
            $"The batch manufacturing record with the Id: {id} was not found");

    public static Error SamplingPointGroupNotFoundOnSubject(Guid id) =>
        Error.Validation(
            "QcTestRequest.SamplingPointGroupNotFound",
            $"The sampling point group with the Id: {id} was not found. A subject's sampling "
            + "point group must be chosen from the existing groups.");

    // --- Assignment -------------------------------------------------------

    public static Error AssignRequiresNotStarted(WorksheetInstanceStatus status) =>
        Error.Validation(
            "QcWorksheetInstance.AssignRequiresNotStarted",
            $"Only a worksheet that has not been started can be assigned. This worksheet is "
            + $"{status} — reassign it instead, which is audited.");

    public static Error ReassignRequiresUnlocked =>
        Error.Validation(
            "QcWorksheetInstance.ReassignRequiresUnlocked",
            "A locked worksheet cannot be reassigned.");

    public static Error AssigneeNotFound(Guid id) =>
        Error.Validation("QcWorksheetInstance.AssigneeNotFound", $"The user with the Id: {id} was not found");

    public static Error AlreadyAssignedToUser =>
        Error.Validation(
            "QcWorksheetInstance.AlreadyAssignedToUser",
            "This worksheet is already assigned to that user.");

    /// <summary>
    /// Assignment enforcement (lifecycle-and-governance.md). Holding the permission key is
    /// necessary but not sufficient — only the current assignee may act, which is what makes
    /// an entry attributable in the GxP sense.
    /// </summary>
    public static Error NotTheAssignee =>
        Error.Forbidden(
            "QcWorksheetInstance.NotTheAssignee",
            "Only the analyst this worksheet is assigned to may start it, enter results, or "
            + "submit it. Have it reassigned to you first.");

    public static Error NotAssigned =>
        Error.Validation(
            "QcWorksheetInstance.NotAssigned",
            "This worksheet has not been assigned to anyone yet.");

    // --- Execution --------------------------------------------------------

    public static Error StartRequiresNotStarted(WorksheetInstanceStatus status) =>
        Error.Validation(
            "QcWorksheetInstance.StartRequiresNotStarted",
            $"Only a worksheet that has not been started can be started. This worksheet is {status}.");

    public static Error EnterValuesRequiresInProgress(WorksheetInstanceStatus status) =>
        Error.Validation(
            "QcWorksheetInstance.EnterValuesRequiresInProgress",
            $"Results can only be entered on a worksheet that is in progress. This worksheet is "
            + $"{status}.");

    public static Error SubmitRequiresInProgress(WorksheetInstanceStatus status) =>
        Error.Validation(
            "QcWorksheetInstance.SubmitRequiresInProgress",
            $"Only a worksheet in progress can be submitted. This worksheet is {status}.");

    public static Error ReviewRequiresSubmitted(WorksheetInstanceStatus status) =>
        Error.Validation(
            "QcWorksheetInstance.ReviewRequiresSubmitted",
            $"Only a submitted worksheet can be reviewed. This worksheet is {status}.");

    public static Error ReturnForCorrectionRequiresSubmitted(WorksheetInstanceStatus status) =>
        Error.Validation(
            "QcWorksheetInstance.ReturnRequiresSubmitted",
            $"Only a submitted worksheet can be returned for correction. This worksheet is {status}.");

    public static Error UnknownFieldKey(string fieldKey) =>
        Error.Validation(
            "QcWorksheetInstance.UnknownFieldKey",
            $"'{fieldKey}' is not a field on this worksheet's template version.");

    /// <summary>
    /// The header block is rendered, never entered — there is no write path to it, and a value
    /// aimed at a Constant-mode field is rejected rather than silently stored.
    /// </summary>
    public static Error FieldIsNotEnterable(string fieldKey, WorksheetFieldMode mode) =>
        Error.Validation(
            "QcWorksheetInstance.FieldNotEnterable",
            $"Field '{fieldKey}' is {mode} and cannot be entered by an analyst.");

    public static Error RowHeaderIsNotEnterable(string fieldKey, string columnKey) =>
        Error.Validation(
            "QcWorksheetInstance.RowHeaderNotEnterable",
            $"Column '{columnKey}' of table '{fieldKey}' contains template row headers and cannot be entered by an analyst.");

    public static Error ValueNotAnOption(
        string fieldKey, string label, string value, IEnumerable<string> options) =>
        Error.Validation(
            "QcWorksheetInstance.ValueNotAnOption",
            $"'{label}' ({fieldKey}): '{value}' is not one of its options "
            + $"({string.Join(", ", options)}).");

    public static Error CellValueNotAnOption(
        string fieldKey, string columnKey, int? rowIndex, string value, IEnumerable<string> options) =>
        Error.Validation(
            "QcWorksheetInstance.ValueNotAnOption",
            $"Table '{fieldKey}', column '{columnKey}', row {(rowIndex ?? 0) + 1}: '{value}' is not "
            + $"one of the column's options ({string.Join(", ", options)}).");

    public static Error ReferencedResultIsNotEnterable(string fieldKey) =>
        Error.Validation(
            "QcWorksheetInstance.ReferencedResultNotEnterable",
            $"Field '{fieldKey}' is a referenced result. It resolves from another worksheet and "
            + "cannot be typed in.");

    public static Error RequiredFieldMissing(string fieldKey, string label) =>
        Error.Validation(
            "QcWorksheetInstance.RequiredFieldMissing",
            $"'{label}' ({fieldKey}) has no value. Every field on the worksheet must be completed "
            + "before it can be submitted.");

    public static Error ReferencedResultUnresolved(string fieldKey, string resolutionValue) =>
        Error.Validation(
            "QcWorksheetInstance.ReferencedResultUnresolved",
            string.IsNullOrWhiteSpace(resolutionValue)
                ? $"Referenced result '{fieldKey}' has not resolved: its lookup field has not been "
                  + "entered yet."
                : $"Referenced result '{fieldKey}' has not resolved: no matching reviewed "
                  + $"qualification was found for '{resolutionValue}'.");

    /// <summary>
    /// A Calculated field that cannot be evaluated blocks submission outright.
    /// <para>
    /// The alternative — skipping the field, or storing a blank — would let a worksheet be
    /// submitted as complete while the number a COA later has to cite does not exist. An
    /// unevaluatable formula means the worksheet is not actually finished, so it fails here for
    /// the same reason a missing entry does, naming the field and the arithmetic reason.
    /// </para>
    /// </summary>
    public static Error CalculatedFieldUnevaluatable(string fieldKey, string label, string reason) =>
        Error.Validation(
            "QcWorksheetInstance.CalculatedFieldUnevaluatable",
            $"'{label}' ({fieldKey}) could not be calculated: {reason} A calculated field must "
            + "produce a value before the worksheet can be submitted.");

    /// <summary>
    /// A Calculated field is system-computed at submission, so there is no analyst write path to
    /// it — the same reasoning as <see cref="FieldIsNotEnterable"/> for a Constant field.
    /// </summary>
    public static Error CalculatedFieldIsNotEnterable(string fieldKey) =>
        Error.Validation(
            "QcWorksheetInstance.CalculatedFieldNotEnterable",
            $"Field '{fieldKey}' is calculated. Its value is computed from the worksheet's own "
            + "entries at submission and cannot be typed in.");

    /// <summary>
    /// <see cref="CalculatedFieldUnevaluatable"/> for one cell of a per-row calculated table
    /// column. Same code; the message names the table, column and 1-based row in the same form
    /// as <see cref="CellValueNotAnOption"/> so a client can pin it to the cell.
    /// </summary>
    public static Error CalculatedCellUnevaluatable(
        string fieldKey, string columnKey, int? rowIndex, string reason) =>
        Error.Validation(
            "QcWorksheetInstance.CalculatedFieldUnevaluatable",
            $"Table '{fieldKey}', column '{columnKey}', row {(rowIndex ?? 0) + 1}: could not be "
            + $"calculated: {reason} A calculated cell must produce a value before the worksheet can "
            + "be submitted.");

    /// <summary><see cref="CalculatedFieldIsNotEnterable"/> for a calculated table column.</summary>
    public static Error CalculatedColumnIsNotEnterable(string fieldKey, string columnKey, int? rowIndex) =>
        Error.Validation(
            "QcWorksheetInstance.CalculatedFieldNotEnterable",
            $"Table '{fieldKey}', column '{columnKey}', row {(rowIndex ?? 0) + 1}: this column is "
            + "calculated. Its values are computed from each row's entries at submission and cannot "
            + "be typed in.");

    // --- Hard instrument / reagent gates ----------------------------------

    public static Error InstrumentNotFound(string fieldKey, string value) =>
        Error.Validation(
            "QcWorksheetInstance.InstrumentNotFound",
            $"Field '{fieldKey}': no QC equipment matches '{value}'. An instrument must be chosen "
            + "from the equipment register.");

    /// <summary>
    /// A hard block, not a warning (lifecycle-and-governance.md, "Instrument / Reagent /
    /// Reference Standard gating"). There is deliberately no override path in this milestone.
    /// </summary>
    public static Error InstrumentCalibrationExpired(
        string fieldKey, string equipmentName, DateTime dueDate) =>
        Error.Validation(
            "QcWorksheetInstance.InstrumentCalibrationExpired",
            $"Field '{fieldKey}': {equipmentName} was due for calibration on "
            + $"{dueDate:yyyy-MM-dd} and cannot be used. Calibrate it, or choose a different "
            + "instrument.");

    /// <summary>
    /// Equipment with no calibration due date on record cannot demonstrate valid calibration,
    /// so it is blocked for the same reason expired equipment is. Stricter than the brief's
    /// literal "if it's in the past" wording — flagged for governance rather than assumed.
    /// </summary>
    public static Error InstrumentCalibrationUnknown(string fieldKey, string equipmentName) =>
        Error.Validation(
            "QcWorksheetInstance.InstrumentCalibrationUnknown",
            $"Field '{fieldKey}': {equipmentName} has no calibration due date on record, so its "
            + "calibration status cannot be confirmed. It cannot be used until the equipment "
            + "register is updated.");

    public static Error ReagentNotFound(string fieldKey, string value) =>
        Error.Validation(
            "QcWorksheetInstance.ReagentNotFound",
            $"Field '{fieldKey}': no reagent matches '{value}'. A reagent must be chosen from the "
            + "catalog.");

    public static Error ReagentExpired(string fieldKey, DateTime expiryDate) =>
        Error.Validation(
            "QcWorksheetInstance.ReagentExpired",
            $"Field '{fieldKey}': the reagent/reference standard expired on "
            + $"{expiryDate:yyyy-MM-dd} and cannot be used.");

    public static Error ReagentExpiryUnreadable(string fieldKey, string value) =>
        Error.Validation(
            "QcWorksheetInstance.ReagentExpiryUnreadable",
            $"Field '{fieldKey}': '{value}' is not a readable expiry date.");

    public static Error ReagentEntryIncomplete(string fieldKey) =>
        Error.Validation(
            "QcWorksheetInstance.ReagentEntryIncomplete",
            $"Field '{fieldKey}': a reagent entry must record the reagent, its batch number and "
            + "its expiry date. The batch and expiry are captured per use because no catalog "
            + "record tracks them.");

    /// <summary>
    /// Segregation of duties, enforced in code rather than left to however an administrator
    /// happened to configure the approval chain.
    /// <para>
    /// Whoever performed the work cannot be the one who signs it off — the second pair of eyes
    /// is the entire control. Refusing outright follows the same principle as QC's opt-out of
    /// the approval engine's auto-approval fallback: a compliance gap fails loudly rather than
    /// being quietly allowed.
    /// </para>
    /// </summary>
    public static Error CannotReviewOwnWork =>
        Error.Forbidden(
            "QcWorksheetInstance.CannotReviewOwnWork",
            "You performed this test, so you cannot review it. A worksheet must be reviewed by "
            + "someone who did not enter, submit or hold its results.");

    public static Error ReviewCommentsRequired =>
        Error.Validation(
            "QcWorksheetInstance.ReviewCommentsRequired",
            "A reason is required when returning a worksheet for correction.");

    // --- OOS cases (Milestone 4) ------------------------------------------

    public static Error OosCaseNotFound(Guid id) =>
        Error.NotFound("QcOosCase.NotFound", $"The OOS case with the Id: {id} was not found");

    public static Error StartInvestigationRequiresOpen(OosCaseStatus status) =>
        Error.Validation(
            "QcOosCase.StartInvestigationRequiresOpen",
            $"Only an Open OOS case can have its investigation started. This case is {status}.");

    public static Error InvestigationNotEditable(OosCaseStatus status) =>
        Error.Validation(
            "QcOosCase.InvestigationNotEditable",
            "Investigation findings can only be edited while the investigation is in progress. "
            + $"This case is {status}.");

    public static Error RetestRequiresInvestigationInProgress(OosCaseStatus status) =>
        Error.Validation(
            "QcOosCase.RetestRequiresInvestigationInProgress",
            $"A retest can only be authorized from an investigation in progress. This case is {status}.");

    public static Error EscalateRequiresInvestigationInProgress(OosCaseStatus status) =>
        Error.Validation(
            "QcOosCase.EscalateRequiresInvestigationInProgress",
            $"A case can only be escalated to QA from an investigation in progress. This case is {status}.");

    public static Error DispositionRequiresPendingQa(OosCaseStatus status) =>
        Error.Validation(
            "QcOosCase.DispositionRequiresPendingQa",
            $"Only a case awaiting QA disposition can be disposed. This case is {status}.");

    public static Error DispositionOutcomeRequired =>
        Error.Validation(
            "QcOosCase.DispositionOutcomeRequired",
            "A disposition must state its outcome: ConfirmedOOS, Invalidated or RetestAccepted.");

    /// <summary>
    /// The readiness check carried forward from the live <c>OosInvestigation</c>: required
    /// analysis must be complete before an investigation can close. Disposing while results are
    /// still coming in would decide a batch's fate on a partial picture.
    /// </summary>
    public static Error DispositionBlockedByIncompleteWork(
        string worksheetCode, WorksheetInstanceStatus status) =>
        Error.Validation(
            "QcOosCase.DispositionBlockedByIncompleteWork",
            $"Worksheet '{worksheetCode}' for this sample is still {status}. Every worksheet for "
            + "the sample must be reviewed before QA can dispose of the case.");

    public static Error RetestAcceptedRequiresRetest =>
        Error.Validation(
            "QcOosCase.RetestAcceptedRequiresRetest",
            "A RetestAccepted disposition requires an authorized retest to accept. This case was "
            + "escalated without one.");

    /// <summary>
    /// The Specification is required to declare its retest policy precisely so a retest never
    /// has to guess between reusing the sample and demanding a fresh one.
    /// </summary>
    public static Error RetestPolicyNotSet(string specificationCode) =>
        Error.Validation(
            "QcOosCase.RetestPolicyNotSet",
            $"Specification '{specificationCode}' does not declare a retest policy, so whether "
            + "this retest may reuse the original sample cannot be determined. Set the policy on "
            + "the Specification first.");

    public static Error OosCaseBlocksRelease(int openCaseCount) =>
        Error.Validation(
            "QcOosCase.BlocksRelease",
            $"This test request has {openCaseCount} open OOS case(s) and cannot be released until "
            + "each one is closed by a QA disposition.");

    // -----------------------------------------------------------------------
    // Milestone 5 — certificates
    // -----------------------------------------------------------------------

    public static Error CoaNotFound(Guid id) =>
        Error.NotFound("QcCoa.NotFound", $"The certificate with the Id: {id} was not found");

    public static Error IssueRequiresDraft(CoaStatus status) =>
        Error.Validation(
            "QcCoa.IssueRequiresDraft",
            $"Only a Draft certificate can be issued. This certificate is {status}.");

    public static Error ReviseRequiresIssued(CoaStatus status) =>
        Error.Validation(
            "QcCoa.ReviseRequiresIssued",
            $"Only an Issued certificate can be revised. This certificate is {status}. A Draft "
            + "is regenerated from current data whenever it is raised, so there is nothing to "
            + "revise until it has been issued.");

    public static Error RevisionReasonRequired =>
        Error.Validation(
            "QcCoa.RevisionReasonRequired",
            "A reason for revision is required. A certificate is only ever replaced for a stated "
            + "cause, and the stated cause is part of the record.");

    public static Error RevisionAlreadyInProgress =>
        Error.Validation(
            "QcCoa.RevisionAlreadyInProgress",
            "A revision of this certificate is already drafted and not yet issued. Issue or "
            + "discard it before raising another.");

    /// <summary>
    /// The strict-hold combination rule (lifecycle-and-governance.md), surfaced as an error on the
    /// one path a user can reach it from. Generation itself never errors — it simply withholds.
    /// </summary>
    public static Error CoaGenerationHeld(string reason) =>
        Error.Validation(
            "QcCoa.GenerationHeld",
            $"A certificate cannot be produced for this test request. {reason}");

    // -----------------------------------------------------------------------
    // Milestone 6 — sampling points, monitoring programs, water quality
    // -----------------------------------------------------------------------

    public static Error SamplingPointNotFound(Guid id) =>
        Error.NotFound("QcSamplingPoint.NotFound", $"The sampling point with the Id: {id} was not found");

    public static Error DuplicateSamplingPointCode(string code) =>
        Error.Conflict(
            "QcSamplingPoint.DuplicateCode",
            $"A sampling point with the code '{code}' already exists. Codes are unique precisely "
            + "so a point's trend history cannot be split in two by a near-duplicate.");

    public static Error SamplingPointInUse(string code, int programCount) =>
        Error.Validation(
            "QcSamplingPoint.InUse",
            $"Sampling point '{code}' is scheduled by {programCount} monitoring program(s) and "
            + "cannot be deleted. Delete or repoint the programs first.");

    public static Error SamplingPointTypeImmutable =>
        Error.Validation(
            "QcSamplingPoint.TypeImmutable",
            "A sampling point's type cannot be changed once monitoring programs, rounds or water "
            + "quality periods reference it — the specifications and limits already resolved "
            + "against it are type-specific.");

    /// <summary>
    /// Water/EM only, mirroring <see cref="SamplingPointGroupIsRoutineOnly"/>. A batch round's
    /// Subject names a batch; there is no point to name.
    /// </summary>
    public static Error SamplingPointIsRoutineOnly(TestRequestType type) =>
        Error.Validation(
            "QcTestRequest.SamplingPointIsRoutineOnly",
            "A sampling point can only be named on a routine Water or Environmental subject. "
            + $"This round is {type}.");

    public static Error SamplingPointTypeMismatch(TestRequestType roundType, SamplingPointType pointType) =>
        Error.Validation(
            "QcTestRequest.SamplingPointTypeMismatch",
            $"This is a {roundType} round, but the selected sampling point is a {pointType} point.");

    public static Error MonitoringProgramNotFound(Guid id) =>
        Error.NotFound(
            "QcMonitoringProgram.NotFound",
            $"The monitoring program with the Id: {id} was not found");

    public static Error MonitoringProgramSpecificationNotFound(Guid id) =>
        Error.Validation(
            "QcMonitoringProgram.SpecificationNotFound",
            $"The specification with the Id: {id} was not found");

    /// <summary>
    /// A schedule that raises real testing rounds must name a controlled document somebody
    /// signed for — the same rule <c>TestRequestRepository</c> applies at round creation.
    /// </summary>
    public static Error MonitoringProgramSpecificationNotEffective(string code, QcDocumentStatus status) =>
        Error.Validation(
            "QcMonitoringProgram.SpecificationNotEffective",
            $"Specification '{code}' is {status}. A monitoring program can only schedule testing "
            + "against an Effective specification.");

    public static Error MonitoringProgramSpecificationTypeMismatch(
        SamplingPointType pointType, SpecificationAppliesTo appliesTo) =>
        Error.Validation(
            "QcMonitoringProgram.SpecificationTypeMismatch",
            $"This is a {pointType} sampling point, but the selected specification applies to "
            + $"{appliesTo}.");

    public static Error MonitoringProgramSpecificationHasNoWorksheetLinks(string code) =>
        Error.Validation(
            "QcMonitoringProgram.SpecificationHasNoWorksheetLinks",
            $"Specification '{code}' links no worksheet templates, so a generated round would "
            + "have no worksheets to run.");

    public static Error CustomIntervalRequired =>
        Error.Validation(
            "QcMonitoringProgram.CustomIntervalRequired",
            "A Custom frequency must state its interval in days.");

    public static Error CustomIntervalNotApplicable(MonitoringFrequency frequency) =>
        Error.Validation(
            "QcMonitoringProgram.CustomIntervalNotApplicable",
            "A custom interval is only meaningful with a Custom frequency. This program is "
            + $"{frequency}.");

    /// <summary>
    /// One live schedule per (point, specification). A second identical program would simply
    /// raise the same round twice.
    /// </summary>
    public static Error DuplicateMonitoringProgram(string samplingPointCode, string specificationCode) =>
        Error.Conflict(
            "QcMonitoringProgram.Duplicate",
            $"Sampling point '{samplingPointCode}' already has a monitoring program against "
            + $"specification '{specificationCode}'.");

    public static Error PauseRequiresActive(MonitoringProgramStatus status) =>
        Error.Validation(
            "QcMonitoringProgram.PauseRequiresActive",
            $"Only an Active monitoring program can be paused. This program is {status}.");

    public static Error ResumeRequiresPaused(MonitoringProgramStatus status) =>
        Error.Validation(
            "QcMonitoringProgram.ResumeRequiresPaused",
            $"Only a Paused monitoring program can be resumed. This program is {status}.");

    public static Error WaterQualityPeriodNotFound(Guid id) =>
        Error.NotFound(
            "QcWaterQualityPeriod.NotFound",
            $"The water quality period with the Id: {id} was not found");

    public static Error ActivateRequiresPendingActivation(WaterQualityPeriodStatus status) =>
        Error.Validation(
            "QcWaterQualityPeriod.ActivateRequiresPendingActivation",
            "Only a PendingActivation water quality period can be activated. This period is "
            + $"{status}.");

    public static Error RetrospectiveReasonRequired =>
        Error.Validation(
            "QcWaterQualityPeriod.RetrospectiveReasonRequired",
            "A retrospective reason is required. Activating a period tells production it may rely "
            + "on this water for a window that has already elapsed, and the justification for that "
            + "is part of the record.");

    /// <summary>
    /// The dynamic-ValidUntil rule taken at its word (lifecycle-and-governance.md, "Water
    /// validity periods"): the window ends at the point's next scheduled test. With no schedule
    /// there is no next test, so there is nothing honest to bound the window with — and an
    /// unbounded window would cover production indefinitely, which is the exact failure the
    /// dynamic rule exists to prevent.
    /// </summary>
    public static Error NoMonitoringProgramForValidUntil(string samplingPointCode) =>
        Error.Validation(
            "QcWaterQualityPeriod.NoMonitoringProgramForValidUntil",
            $"Sampling point '{samplingPointCode}' has no active monitoring program, so there is "
            + "no next scheduled test to bound this period's validity against. Configure a "
            + "monitoring program for the point first.");

    public static Error HoldRequiresActive(WaterQualityPeriodStatus status) =>
        Error.Validation(
            "QcWaterQualityPeriod.HoldRequiresActive",
            $"Only an Active water quality period can be held. This period is {status}.");

    public static Error HoldReasonRequired =>
        Error.Validation(
            "QcWaterQualityPeriod.HoldReasonRequired",
            "A hold reason is required. Withdrawing a period flags every water use underneath it "
            + "for Quality Impact Assessment, and the cause is part of that flag.");

    public static Error WaterUseRequiresActivePeriod(WaterQualityPeriodStatus status) =>
        Error.Validation(
            "QcWaterUseRecord.RequiresActivePeriod",
            $"Water use can only be recorded against an Active period. This period is {status}. "
            + "Recording use against a period that covers nothing would assert coverage that was "
            + "never granted.");
}
