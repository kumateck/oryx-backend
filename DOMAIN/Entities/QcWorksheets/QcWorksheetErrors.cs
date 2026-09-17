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
}
