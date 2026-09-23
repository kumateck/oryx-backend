using DOMAIN.Entities.FullProcedures;
using SHARED;

namespace APP.Services.FullProcedures;

public interface ITemplateQuestionService
{
    Task<Result<IReadOnlyList<TemplateQuestionDto>>> ListAsync(
        Guid areaId, IReadOnlyCollection<Guid> actorRoleIds,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateQuestionDetailDto>> GetAsync(
        Guid questionId, IReadOnlyCollection<Guid> actorRoleIds,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateQuestionRevisionDto>> CreateAsync(
        CreateTemplateQuestionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateQuestionRevisionDto>> CreateRevisionAsync(
        Guid questionId, CreateTemplateQuestionRevisionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateQuestionRevisionDto>> UpdateDraftAsync(
        Guid revisionId, UpdateTemplateQuestionRevisionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateQuestionRevisionDto>> SubmitForReviewAsync(
        Guid revisionId, TemplateQuestionTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateQuestionRevisionDto>> RecordReviewAsync(
        Guid revisionId, TemplateQuestionTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateQuestionRevisionDto>> ReturnToDraftAsync(
        Guid revisionId, TemplateQuestionTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateQuestionRevisionDto>> PublishAsync(
        Guid revisionId, TemplateQuestionTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateQuestionRevisionDto>> RetireAsync(
        Guid revisionId, TemplateQuestionTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
}
