using DOMAIN.Entities.FullProcedures;
using SHARED;

namespace APP.Services.FullProcedures;

public interface ITemplateSharingService
{
    Task<Result<IReadOnlyList<TemplateSharingGrantDto>>> ListAsync(Guid areaId,
        IReadOnlyCollection<Guid> actorRoleIds, CancellationToken cancellationToken = default);
    Task<Result<TemplateSharingGrantDto>> RequestAsync(
        RequestTemplateSharingGrantRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateSharingGrantDto>> ApproveAsync(Guid grantId,
        DecideTemplateSharingGrantRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateSharingGrantDto>> RejectAsync(Guid grantId,
        DecideTemplateSharingGrantRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateSharingGrantDto>> RevokeAsync(Guid grantId,
        DecideTemplateSharingGrantRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateRevisionUsageDto>> GetUsageAsync(TemplateRevisionKind kind,
        Guid definitionId, Guid revisionId, IReadOnlyCollection<Guid> actorRoleIds,
        CancellationToken cancellationToken = default);
}
