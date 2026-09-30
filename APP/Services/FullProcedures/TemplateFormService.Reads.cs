using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.FullProcedures;

public sealed partial class TemplateFormService
{
    public async Task<Result<IReadOnlyList<TemplateFormDto>>> ListAsync(
        Guid areaId, IReadOnlyCollection<Guid> actorRoleIds,
        CancellationToken cancellationToken = default)
    {
        var area = await LoadAreaAsync(areaId, cancellationToken);
        if (area is null) return TemplateAreaErrors.NotFound;
        if (!TemplateDefinitionAuthorization.Allows(
                area, actorRoleIds, TemplateDefinitionOperation.View))
            return TemplateFormErrors.AccessDenied;
        var forms = await FormQuery().AsNoTracking()
            .Where(item => item.TemplateAreaId == areaId)
            .OrderBy(item => item.PurposeId).ThenBy(item => item.SubjectTypeId)
            .ThenBy(item => item.Id).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<TemplateFormDto>>(
            forms.Select(ToFormDto).ToArray());
    }

    public async Task<Result<TemplateFormDetailDto>> GetAsync(
        Guid formId, IReadOnlyCollection<Guid> actorRoleIds,
        CancellationToken cancellationToken = default)
    {
        var form = await FormQuery().AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == formId, cancellationToken);
        if (form is null) return TemplateFormErrors.NotFound;
        if (!TemplateDefinitionAuthorization.Allows(
                form.TemplateArea, actorRoleIds, TemplateDefinitionOperation.View))
            return TemplateFormErrors.AccessDenied;
        return new TemplateFormDetailDto(form.Id, form.TemplateAreaId, form.TemplateArea.Name,
            form.PurposeId, form.SubjectTypeId,
            form.Revisions.OrderByDescending(item => item.Sequence)
                .Select(TemplateFormServiceSupport.ToDto).ToArray());
    }
}
