using DOMAIN.Entities.FullProcedures;
using SHARED;

namespace APP.Services.FullProcedures;

public interface ITemplateAdoptionService
{
    Task<Result<TemplateAdoptionDto>> AdoptAsync(Guid grantId,
        AdoptTemplateRevisionRequest request, Guid actorId,
        IReadOnlyCollection<Guid> actorRoleIds, Guid correlationId,
        CancellationToken cancellationToken = default);
    Task<Result<TemplateAdoptionDto>> GetAsync(Guid adoptionId,
        IReadOnlyCollection<Guid> actorRoleIds,
        CancellationToken cancellationToken = default);
}
