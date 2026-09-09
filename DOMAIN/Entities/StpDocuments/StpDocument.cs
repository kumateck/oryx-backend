using DOMAIN.Entities.Base;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.StpDocuments;

public enum StpDocumentStatus
{
    Draft = 0,
    InReview = 1,
    Approved = 2,
    Reviewed = 3
}

public class StpDocument : BaseEntity
{
    public string OwnerType { get; set; }

    public Guid OwnerId { get; set; }

    public StpDocumentStatus Status { get; set; } = StpDocumentStatus.Draft;

    public Guid? CurrentDraftVersionId { get; set; }

    public StpDocumentVersion CurrentDraftVersion { get; set; }

    public Guid? EffectiveVersionId { get; set; }

    public StpDocumentVersion EffectiveVersion { get; set; }

    public Guid? LockedById { get; set; }

    public User LockedBy { get; set; }

    public DateTime? LockedAt { get; set; }

    public List<StpDocumentVersion> Versions { get; set; } = [];
}
