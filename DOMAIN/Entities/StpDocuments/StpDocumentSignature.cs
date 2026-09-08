using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.StpDocuments;

public enum StpDocumentSignatureAction
{
    Reviewed = 0,
    Approved = 1,
    Rejected = 2
}

/// <summary>
/// A captured e-signature (21 CFR 11.50) for a review/approve/reject transition.
/// CreatedById/CreatedAt (from BaseEntity) are the signer and the signing timestamp.
/// </summary>
public class StpDocumentSignature : BaseEntity
{
    public Guid StpDocumentVersionId { get; set; }

    public StpDocumentVersion StpDocumentVersion { get; set; }

    public StpDocumentSignatureAction Action { get; set; }

    public string Meaning { get; set; }
}
