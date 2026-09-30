using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.FullProcedures;

public sealed partial class ProcedureService
{
    public async Task<Result<IReadOnlyList<ProcedureDefinitionDto>>> ListAsync(Guid areaId,
        IReadOnlyCollection<Guid> actorRoleIds, CancellationToken cancellationToken = default)
    {
        var area = await AreaQuery().AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == areaId, cancellationToken);
        if (area is null) return ProcedureErrors.NotFound;
        if (!TemplateDefinitionAuthorization.Allows(
                area, actorRoleIds, TemplateDefinitionOperation.View))
            return ProcedureErrors.AccessDenied;
        var items = await DefinitionQuery().AsNoTracking()
            .Where(x => x.TemplateAreaId == areaId).OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<ProcedureDefinitionDto>>(
            items.Select(ToDefinitionDto).ToArray());
    }

    public async Task<Result<ProcedureDefinitionDetailDto>> GetAsync(Guid definitionId,
        IReadOnlyCollection<Guid> actorRoleIds, CancellationToken cancellationToken = default)
    {
        var item = await DefinitionQuery().AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == definitionId, cancellationToken);
        if (item is null) return ProcedureErrors.NotFound;
        if (!TemplateDefinitionAuthorization.Allows(
                item.TemplateArea, actorRoleIds, TemplateDefinitionOperation.View))
            return ProcedureErrors.AccessDenied;
        return new ProcedureDefinitionDetailDto(item.Id, item.TemplateAreaId,
            item.TemplateArea.Name, item.PurposeId, item.SubjectTypeId,
            item.Revisions.OrderByDescending(x => x.Sequence)
                .Select(ProcedureServiceSupport.ToDto).ToArray());
    }

    public async Task<Result<ProcedureValidationReportDto>> ValidateAsync(Guid revisionId,
        IReadOnlyCollection<Guid> actorRoleIds, CancellationToken cancellationToken = default)
    {
        var revision = await LoadRevisionAsync(revisionId, cancellationToken);
        if (revision is null) return ProcedureErrors.NotFound;
        if (!TemplateDefinitionAuthorization.Allows(revision.ProcedureDefinition.TemplateArea,
                actorRoleIds, TemplateDefinitionOperation.View))
            return ProcedureErrors.AccessDenied;
        var reasons = new List<string>();
        if (!revision.ProcedureDefinition.TemplateArea.IsActive) reasons.Add("AreaInactive");
        if (!await DependenciesAvailableAsync(revision, cancellationToken))
            reasons.Add("DependencyUnavailable");
        return new ProcedureValidationReportDto(reasons.Count == 0, reasons,
            revision.ContentHash);
    }

    private static ProcedureDefinitionDto ToDefinitionDto(ProcedureDefinition item)
    {
        var latest = item.Revisions.OrderByDescending(x => x.Sequence).FirstOrDefault();
        return new ProcedureDefinitionDto(item.Id, item.TemplateAreaId, item.TemplateArea.Name,
            item.PurposeId, item.SubjectTypeId,
            latest is null ? null : ProcedureServiceSupport.ToDto(latest));
    }
}
