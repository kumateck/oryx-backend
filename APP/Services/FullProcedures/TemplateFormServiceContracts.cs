using DOMAIN.Entities.FullProcedures;
using SHARED;

namespace APP.Services.FullProcedures;

public interface ITemplateFormService
{
    Task<Result<IReadOnlyList<TemplateFormDto>>> ListAsync(
        Guid areaId, IReadOnlyCollection<Guid> actorRoleIds,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateFormDetailDto>> GetAsync(
        Guid formId, IReadOnlyCollection<Guid> actorRoleIds,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateFormRevisionDto>> CreateAsync(
        CreateTemplateFormRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateFormRevisionDto>> CreateRevisionAsync(
        Guid formId, CreateTemplateFormRevisionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateFormRevisionDto>> UpdateDraftAsync(
        Guid revisionId, UpdateTemplateFormRevisionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateFormRevisionDto>> SubmitForReviewAsync(
        Guid revisionId, TemplateFormTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateFormRevisionDto>> RecordReviewAsync(
        Guid revisionId, TemplateFormTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateFormRevisionDto>> ReturnToDraftAsync(
        Guid revisionId, TemplateFormTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateFormRevisionDto>> PublishAsync(
        Guid revisionId, TemplateFormTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateFormRevisionDto>> RetireAsync(
        Guid revisionId, TemplateFormTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
}
