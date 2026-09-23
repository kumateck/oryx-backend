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
}
