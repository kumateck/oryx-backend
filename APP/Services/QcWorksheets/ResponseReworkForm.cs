using APP.IRepository;
using AutoMapper;
using DOMAIN.Entities.Forms;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.QcWorksheets;

public static class ResponseReworkForm
{
    public static async Task<Result<FormDto>> Read(ApplicationDbContext context,
        IFormRepository forms, IMapper mapper, Guid taskId)
    {
        var task = await context.ResponseReworkTasks.AsNoTracking()
            .Include(item => item.SourceResponse).SingleOrDefaultAsync(item => item.Id == taskId);
        if (task is null) return Error.NotFound("ResponseRework.NotFound", "Rework task was not found.");
        var source = task.SourceResponse;
        var result = await forms.GetForm(source.FormId);
        if (result.IsFailure || !source.FormRevisionId.HasValue) return result;
        var fields = await context.FormFieldRevisions.AsNoTracking()
            .Include(item => item.FormField).ThenInclude(item => item.FormSection)
            .Include(item => item.Question).ThenInclude(item => item.Options)
            .Where(item => item.FormRevisionId == source.FormRevisionId).ToListAsync();
        if (fields.Count == 0)
            return Error.Conflict("ResponseRework.FormRevision", "The pinned form revision is unavailable.");
        var definition = result.Value;
        // Preserve the cycle's field membership, question references and validation metadata.
        // Newly added fields on the current editable form cannot leak into an older cycle.
        definition.Sections = fields.GroupBy(item => item.FormField.FormSectionId)
            .Select(group =>
            {
                var section = mapper.Map<FormSectionDto>(group.First().FormField.FormSection);
                section.Fields = group.OrderBy(item => item.Rank).Select(item => new FormFieldDto
                {
                    Id = item.FormFieldId, Question = mapper.Map<QuestionDto>(item.Question),
                    Required = item.Required, Rank = item.Rank, Description = item.Description
                }).ToList();
                return section;
            }).OrderBy(item => item.Order).ToList();
        return definition;
    }
}
