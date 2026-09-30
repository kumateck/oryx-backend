using DOMAIN.Entities.FullProcedures;
using SHARED;

namespace APP.Services.FullProcedures;

public interface ITemplateWorkflowService
{
    Task<Result<IReadOnlyList<TemplateWorkflowDto>>> ListAsync(Guid areaId,
        IReadOnlyCollection<Guid> actorRoleIds, CancellationToken cancellationToken = default);
    Task<Result<TemplateWorkflowDetailDto>> GetAsync(Guid workflowId,
        IReadOnlyCollection<Guid> actorRoleIds, CancellationToken cancellationToken = default);
    Task<Result<TemplateWorkflowRevisionDto>> CreateAsync(CreateTemplateWorkflowRequest request,
        Guid actorId, IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateWorkflowRevisionDto>> CreateRevisionAsync(Guid workflowId,
        CreateTemplateWorkflowRevisionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateWorkflowRevisionDto>> UpdateDraftAsync(Guid revisionId,
        UpdateTemplateWorkflowRevisionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateWorkflowRevisionDto>> UpdateLayoutAsync(Guid revisionId,
        UpdateTemplateWorkflowLayoutRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, CancellationToken cancellationToken = default);
    Task<Result<TemplateWorkflowRevisionDto>> SubmitForReviewAsync(Guid revisionId,
        TemplateWorkflowTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateWorkflowRevisionDto>> RecordReviewAsync(Guid revisionId,
        TemplateWorkflowTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateWorkflowRevisionDto>> ReturnToDraftAsync(Guid revisionId,
        TemplateWorkflowTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateWorkflowRevisionDto>> PublishAsync(Guid revisionId,
        TemplateWorkflowTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateWorkflowRevisionDto>> RetireAsync(Guid revisionId,
        TemplateWorkflowTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
}
