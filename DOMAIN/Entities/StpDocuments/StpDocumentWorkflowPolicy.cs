namespace DOMAIN.Entities.StpDocuments;

public static class StpDocumentWorkflowPolicy
{
    public static bool CanTransition(StpDocumentStatus from, StpDocumentStatus to) =>
        (from, to) switch
        {
            (StpDocumentStatus.Draft, StpDocumentStatus.InReview) => true,
            (StpDocumentStatus.InReview, StpDocumentStatus.Reviewed) => true,
            (StpDocumentStatus.InReview, StpDocumentStatus.Draft) => true,
            (StpDocumentStatus.Reviewed, StpDocumentStatus.Approved) => true,
            (StpDocumentStatus.Reviewed, StpDocumentStatus.Draft) => true,
            // Approved -> Draft is deliberately NOT here: an approved version is the
            // effective document and must never become directly editable in place. Revising
            // an approved STP goes through UploadVersion/CreateBlank instead, which creates a
            // brand-new version (with a required reason for change) and leaves
            // EffectiveVersionId untouched until that new version is independently reviewed
            // and approved. Reject is for kicking back work still in review, not for
            // un-approving an effective document.
            _ => false,
        };

    public static bool HasIndependentReviewer(Guid? authorId, Guid reviewerId) =>
        authorId.HasValue && authorId.Value != reviewerId;

    public static bool HasIndependentApprover(
        Guid? authorId,
        Guid? reviewerId,
        Guid approverId
    ) => authorId.HasValue
        && reviewerId.HasValue
        && authorId.Value != approverId
        && reviewerId.Value != approverId;
}
