namespace DOMAIN.Entities.StpDocuments;

public class StpDocumentDto
{
    public Guid Id { get; set; }

    public string OwnerType { get; set; }

    public Guid OwnerId { get; set; }

    public StpDocumentStatus Status { get; set; }

    public Guid? CurrentDraftVersionId { get; set; }

    public Guid? EffectiveVersionId { get; set; }

    public Guid? LockedById { get; set; }

    public string LockedByName { get; set; }

    public DateTime? LockedAt { get; set; }

    public List<StpDocumentVersionDto> Versions { get; set; } = [];
}

public class StpDocumentVersionDto
{
    public Guid Id { get; set; }

    public Guid StpDocumentId { get; set; }

    public int VersionNumber { get; set; }

    public string FileName { get; set; }

    public string Sha256 { get; set; }

    public long Size { get; set; }

    public StpDocumentVersionSource Source { get; set; }

    public string ReasonForChange { get; set; }

    /// <summary>
    /// True once a later version has been approved as the document's effective version.
    /// Derived, not stored - superseded versions are never deleted, only labeled.
    /// </summary>
    public bool IsSuperseded { get; set; }

    public bool IsEffective { get; set; }

    public Guid? CreatedById { get; set; }

    public string CreatedByName { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<StpDocumentSignatureDto> Signatures { get; set; } = [];
}

public class StpDocumentSignatureDto
{
    public Guid Id { get; set; }

    public Guid StpDocumentVersionId { get; set; }

    public StpDocumentSignatureAction Action { get; set; }

    public string Meaning { get; set; }

    public Guid? CreatedById { get; set; }

    public string CreatedByName { get; set; }

    public DateTime CreatedAt { get; set; }
}
