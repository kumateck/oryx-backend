using DOMAIN.Entities.FullProcedures;
using SHARED;

namespace APP.Services.FullProcedures;

public interface ITemplateSectionService
{
    Task<Result<IReadOnlyList<TemplateSectionDto>>> ListAsync(
        Guid areaId, IReadOnlyCollection<Guid> actorRoleIds,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateSectionDetailDto>> GetAsync(
        Guid sectionId, IReadOnlyCollection<Guid> actorRoleIds,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateSectionRevisionDto>> CreateAsync(
        CreateTemplateSectionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateSectionRevisionDto>> CreateRevisionAsync(
        Guid sectionId, CreateTemplateSectionRevisionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateSectionRevisionDto>> UpdateDraftAsync(
        Guid revisionId, UpdateTemplateSectionRevisionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateSectionRevisionDto>> SubmitForReviewAsync(
        Guid revisionId, TemplateSectionTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateSectionRevisionDto>> RecordReviewAsync(
        Guid revisionId, TemplateSectionTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateSectionRevisionDto>> ReturnToDraftAsync(
        Guid revisionId, TemplateSectionTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateSectionRevisionDto>> PublishAsync(
        Guid revisionId, TemplateSectionTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateSectionRevisionDto>> RetireAsync(
        Guid revisionId, TemplateSectionTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
}
