using DOMAIN.Entities.FullProcedures;
using SHARED;

namespace APP.Services.FullProcedures;

public interface ITemplateAreaService
{
    Task<Result<TemplateAreaCatalogDto>> GetCatalogAsync(CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<TemplateAreaDto>>> ListAsync(
        IReadOnlyCollection<Guid> actorRoleIds, bool includeInactive,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateAreaDto>> GetAsync(
        Guid id, IReadOnlyCollection<Guid> actorRoleIds,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateAreaDto>> CreateAsync(
        TemplateAreaDraftRequest request, Guid actorId, IReadOnlyCollection<Guid> actorRoleIds,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateAreaDto>> UpdateAsync(
        Guid id, UpdateTemplateAreaRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, CancellationToken cancellationToken = default);
    Task<Result<TemplateAreaDto>> SetActiveAsync(
        Guid id, ChangeTemplateAreaActiveRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, CancellationToken cancellationToken = default);
}
