using DOMAIN.Entities.Forms;
using DOMAIN.Entities.QualityRoutines;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.Forms;

/// <summary>
/// A specification has a chemical template (FormId) and an optional microbial template (MicrobialFormId).
/// Each template's sections hold that analysis type's tests and references, so the two must be different forms.
/// </summary>
public static class SpecificationTemplateSlots
{
    public static async Task<Result> Validate(ApplicationDbContext db, string owner, Guid formId, Guid? microbialFormId)
    {
        if (!microbialFormId.HasValue) return Result.Success();
        if (microbialFormId.Value == Guid.Empty)
            return Error.Validation($"{owner}.MicrobialForm", "Choose a microbial template or leave it empty.");
        if (microbialFormId.Value == formId)
            return Error.Validation($"{owner}.MicrobialForm",
                "The microbial template must be a different template from the chemical one.");
        var form = await db.Forms.AsNoTracking().AsSplitQuery()
            .Include(item => item.Sections).ThenInclude(section => section.Fields).ThenInclude(field => field.Question)
            .FirstOrDefaultAsync(item => item.Id == microbialFormId.Value);
        if (form is null)
            return Error.Validation($"{owner}.MicrobialForm", "The microbial template was not found.");
        return HasSectionsFor(form, AnalysisType.Microbial)
            ? Result.Success()
            : Error.Validation($"{owner}.MicrobialForm",
                "The microbial template has no sections that apply to microbial analysis.");
    }

    /// <summary>A section with no analysis type is shared by both; otherwise it must match.</summary>
    public static bool HasSectionsFor(Form form, AnalysisType analysisType) =>
        form.Sections?.Any(section => section.DeletedAt == null
            && (section.AnalysisType == null || section.AnalysisType == analysisType)) == true;

    /// <summary>
    /// The template an ARD of this analysis type must use when the specification names one for it.
    /// Null means the specification does not constrain it (no microbial slot set, or no specification).
    /// </summary>
    public static Guid? RequiredArdForm(AnalysisType analysisType, Guid? chemicalFormId, Guid? microbialFormId) =>
        analysisType == AnalysisType.Microbial ? microbialFormId : null;
}
