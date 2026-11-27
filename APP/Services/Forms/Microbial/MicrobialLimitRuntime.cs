using DOMAIN.Entities.Forms;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

#nullable enable
namespace APP.Services.Forms.Microbial;

/// <summary>Applies a specification's typed microbial limits to a worksheet response. The server's verdict replaces any compliance the browser sent.</summary>
public static class MicrobialLimitRuntime
{
    public static async Task<Result> Apply(ApplicationDbContext db, Response response)
    {
        var limits = await SpecificationMicrobialLimitService.ForResponse(db, response);
        if (limits.Count == 0) return Result.Success();
        var limitedFields = limits.Select(limit => limit.FormFieldId).ToList();
        var sections = await db.FormFields.AsNoTracking().Where(field => limitedFields.Contains(field.Id))
            .Select(field => new { field.Id, field.FormSectionId, field.Question.Label }).ToListAsync();
        var sectionIds = sections.Select(item => item.FormSectionId).Distinct().ToList();
        var answers = await db.FormResponses.Include(answer => answer.FormField)
            .Where(answer => answer.ResponseId == response.Id && answer.DeletedAt == null && sectionIds.Contains(answer.FormField.FormSectionId)).ToListAsync();
        var verdicts = new Dictionary<Guid, bool>();
        foreach (var limit in limits)
        {
            var field = sections.SingleOrDefault(item => item.Id == limit.FormFieldId);
            if (field is null) continue;
            var answer = answers.Where(item => item.FormFieldId == limit.FormFieldId).OrderByDescending(item => item.CreatedAt).FirstOrDefault();
            var verdict = MicrobialLimits.Evaluate(limit, answer?.Value);
            if (verdict == MicrobialLimits.Verdict.Unresolved)
                return Error.Conflict("MicrobialLimit.Unresolved",
                    $"\"{field.Label}\" has no result that can be compared with its limit ({MicrobialLimits.Text(limit)}). Complete the worksheet before generating the certificate.");
            verdicts[field.FormSectionId] = verdicts.GetValueOrDefault(field.FormSectionId, true) && verdict == MicrobialLimits.Verdict.Complies;
        }
        foreach (var answer in answers)
            if (verdicts.TryGetValue(answer.FormField.FormSectionId, out var complies)) answer.Complies = complies;
        return Result.Success();
    }

    /// <summary>The printed limit for each limited field of the response, for certificates.</summary>
    public static async Task<Dictionary<Guid, string>> Texts(ApplicationDbContext db, Response response) =>
        (await SpecificationMicrobialLimitService.ForResponse(db, response)).ToDictionary(limit => limit.FormFieldId, MicrobialLimits.Text);
}
