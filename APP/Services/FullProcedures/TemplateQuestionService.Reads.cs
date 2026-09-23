using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.FullProcedures;

public sealed partial class TemplateQuestionService
{
    public async Task<Result<IReadOnlyList<TemplateQuestionDto>>> ListAsync(
        Guid areaId, IReadOnlyCollection<Guid> actorRoleIds,
        CancellationToken cancellationToken = default)
    {
        var area = await LoadAreaAsync(areaId, cancellationToken);
        if (area is null) return TemplateAreaErrors.NotFound;
        if (!TemplateDefinitionAuthorization.Allows(
                area, actorRoleIds, TemplateDefinitionOperation.View))
            return TemplateQuestionErrors.AccessDenied;
        var questions = await QuestionQuery().AsNoTracking()
            .Where(item => item.TemplateAreaId == areaId)
            .OrderBy(item => item.PurposeId).ThenBy(item => item.SubjectTypeId)
            .ThenBy(item => item.Id).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<TemplateQuestionDto>>(
            questions.Select(ToQuestionDto).ToArray());
    }

    public async Task<Result<TemplateQuestionDetailDto>> GetAsync(
        Guid questionId, IReadOnlyCollection<Guid> actorRoleIds,
        CancellationToken cancellationToken = default)
    {
        var question = await QuestionQuery().AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == questionId, cancellationToken);
        if (question is null) return TemplateQuestionErrors.NotFound;
        if (!TemplateDefinitionAuthorization.Allows(
                question.TemplateArea, actorRoleIds, TemplateDefinitionOperation.View))
            return TemplateQuestionErrors.AccessDenied;
        return new TemplateQuestionDetailDto(
            question.Id, question.TemplateAreaId, question.TemplateArea.Name,
            question.PurposeId, question.SubjectTypeId,
            question.Revisions.OrderByDescending(item => item.Sequence)
                .Select(TemplateQuestionServiceSupport.ToRevisionDto).ToArray());
    }
}
