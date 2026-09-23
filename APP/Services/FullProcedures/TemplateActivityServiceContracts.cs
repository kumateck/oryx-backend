using DOMAIN.Entities.FullProcedures;
using SHARED;

namespace APP.Services.FullProcedures;

public interface ITemplateActivityService
{
    Task<Result<IReadOnlyList<TemplateActivityDto>>> ListAsync(Guid areaId,
        IReadOnlyCollection<Guid> actorRoleIds, CancellationToken cancellationToken = default);
    Task<Result<TemplateActivityDetailDto>> GetAsync(Guid activityId,
        IReadOnlyCollection<Guid> actorRoleIds, CancellationToken cancellationToken = default);
    Task<Result<TemplateActivityRevisionDto>> CreateAsync(CreateTemplateActivityRequest request,
        Guid actorId, IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateActivityRevisionDto>> CreateRevisionAsync(Guid activityId,
        CreateTemplateActivityRevisionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateActivityRevisionDto>> UpdateDraftAsync(Guid revisionId,
        UpdateTemplateActivityRevisionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateActivityRevisionDto>> SubmitForReviewAsync(Guid revisionId,
        TemplateActivityTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateActivityRevisionDto>> RecordReviewAsync(Guid revisionId,
        TemplateActivityTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateActivityRevisionDto>> ReturnToDraftAsync(Guid revisionId,
        TemplateActivityTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateActivityRevisionDto>> PublishAsync(Guid revisionId,
        TemplateActivityTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateActivityRevisionDto>> RetireAsync(Guid revisionId,
        TemplateActivityTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
}
