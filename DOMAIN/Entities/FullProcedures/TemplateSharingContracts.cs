namespace DOMAIN.Entities.FullProcedures;

public sealed class RequestTemplateSharingGrantRequest
{
    public Guid SourceAreaId { get; set; }
    public Guid TargetAreaId { get; set; }
    public Guid RequestedByAreaId { get; set; }
    public TemplateRevisionKind TemplateKind { get; set; }
    public Guid DefinitionId { get; set; }
    public Guid RevisionId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class DecideTemplateSharingGrantRequest
{
    public int ExpectedVersion { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed record TemplateSharingGrantDto(Guid Id, Guid SourceAreaId, string SourceAreaName,
    Guid TargetAreaId, string TargetAreaName, Guid RequestedByAreaId,
    TemplateRevisionKind TemplateKind, Guid DefinitionId, Guid RevisionId,
    string RevisionContentHash, string PurposeId, string SubjectTypeId,
    TemplateSharingGrantStatus Status, int Version, Guid RequestedById,
    DateTime RequestedAt, Guid? DecidedById, DateTime? DecidedAt,
    Guid? RevokedById, DateTime? RevokedAt);

public sealed record TemplateRevisionUsageDto(TemplateRevisionKind TemplateKind,
    Guid DefinitionId, Guid RevisionId, Guid SourceAreaId, string PurposeId,
    string SubjectTypeId, int DirectDefinitionReferences, int ActiveSharingGrants);
