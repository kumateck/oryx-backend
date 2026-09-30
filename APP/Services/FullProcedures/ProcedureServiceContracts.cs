using DOMAIN.Entities.FullProcedures;
using SHARED;

namespace APP.Services.FullProcedures;

public interface IProcedureService
{
    Task<Result<IReadOnlyList<ProcedureDefinitionDto>>> ListAsync(Guid areaId,
        IReadOnlyCollection<Guid> actorRoleIds, CancellationToken cancellationToken = default);
    Task<Result<ProcedureDefinitionDetailDto>> GetAsync(Guid definitionId,
        IReadOnlyCollection<Guid> actorRoleIds, CancellationToken cancellationToken = default);
    Task<Result<ProcedureRevisionDto>> CreateAsync(CreateProcedureRequest request,
        Guid actorId, IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<ProcedureRevisionDto>> CreateRevisionAsync(Guid definitionId,
        CreateProcedureRevisionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<ProcedureRevisionDto>> UpdateDraftAsync(Guid revisionId,
        UpdateProcedureRevisionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<ProcedureValidationReportDto>> ValidateAsync(Guid revisionId,
        IReadOnlyCollection<Guid> actorRoleIds, CancellationToken cancellationToken = default);
    Task<Result<ProcedureRevisionDto>> SubmitForReviewAsync(Guid revisionId,
        ProcedureTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<ProcedureRevisionDto>> RecordReviewAsync(Guid revisionId,
        ProcedureTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<ProcedureRevisionDto>> ReturnToDraftAsync(Guid revisionId,
        ProcedureTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<ProcedureRevisionDto>> ApproveAsync(Guid revisionId,
        ProcedureTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<ProcedureRevisionDto>> RetireAsync(Guid revisionId,
        ProcedureTransitionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
}
