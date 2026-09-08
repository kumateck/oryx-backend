using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.StpDocuments;

public enum StpDocumentVersionSource
{
    Upload = 0,
    Created = 1,
    AutoSave = 2,
    ManualSave = 3
}

public class StpDocumentVersion : BaseEntity
{
    public Guid StpDocumentId { get; set; }

    public StpDocument StpDocument { get; set; }

    public int VersionNumber { get; set; }

    public string StorageKey { get; set; }

    public string FileName { get; set; }

    public string Sha256 { get; set; }

    public long Size { get; set; }

    public StpDocumentVersionSource Source { get; set; }

    public string ReasonForChange { get; set; }

    public List<StpDocumentSignature> Signatures { get; set; } = [];
}
