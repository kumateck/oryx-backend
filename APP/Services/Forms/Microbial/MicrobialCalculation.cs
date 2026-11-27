using APP.Services.Formulas;
using DOMAIN.Entities.Forms;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

#nullable enable
namespace APP.Services.Forms.Microbial;

/// <summary>Keeps a section's CFU result in step with its colony count and dilution. The browser never supplies the result.</summary>
public static class MicrobialCalculation
{
    public static bool Feeds(QuestionType type) => type is QuestionType.ColonyCount or QuestionType.Dilution or QuestionType.CfuCalculation;

    /// <summary>
    /// Recalculates every CfuCalculation answer in the section from the response's newest ColonyCount and Dilution answers
    /// (tracked in <paramref name="response"/>). A result that cannot be calculated yet is cleared.
    /// </summary>
    public static async Task Apply(ApplicationDbContext db, Response response, Guid formSectionId, Guid? formRevisionId)
    {
        var fields = await db.FormFields.AsNoTracking().Where(field => field.FormSectionId == formSectionId && field.DeletedAt == null)
            .Select(field => new { field.Id, field.Question.Type, Unit = field.Question.Options.Where(option => option.DeletedAt == null).Select(option => option.Name).FirstOrDefault() })
            .ToListAsync();
        var results = fields.Where(field => field.Type == QuestionType.CfuCalculation).ToList();
        if (results.Count == 0) return;
        string? Newest(QuestionType type)
        {
            var ids = fields.Where(field => field.Type == type).Select(field => field.Id).ToHashSet();
            return ids.Count != 1 ? null : response.FormResponses.Where(answer => ids.Contains(answer.FormFieldId) && answer.DeletedAt == null)
                .OrderByDescending(answer => answer.CreatedAt).FirstOrDefault()?.Value;
        }
        var count = MicrobialValues.ReadColonyCount(Newest(QuestionType.ColonyCount));
        var dilution = MicrobialValues.ReadDilution(Newest(QuestionType.Dilution));
        foreach (var field in results)
        {
            var value = count is null || dilution is null || string.IsNullOrWhiteSpace(field.Unit) ? null
                : MicrobialValues.Serialize(MicrobialValues.Calculate(count, dilution, field.Unit.Trim()));
            var answer = response.FormResponses.Where(item => item.FormFieldId == field.Id && item.DeletedAt == null)
                .OrderByDescending(item => item.CreatedAt).FirstOrDefault();
            if (answer is not null) { answer.Value = value; continue; }
            if (value is null) continue;
            response.FormResponses.Add(new FormResponse
            {
                FormFieldId = field.Id,
                FormFieldRevisionId = await FormRevisionSelection.FieldIdAsync(db, formRevisionId, field.Id),
                Value = value,
            });
        }
    }
}
