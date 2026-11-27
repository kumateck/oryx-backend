using DOMAIN.Entities.QualityRoutines;
using SHARED;

namespace DOMAIN.Entities.Forms;

public static class FormValidator
{
    public static Result Validate(Form form)
    {
        if (form.Sections == null || form.Sections.Count == 0)
            return Result.Failure(FormErrors.SectionMissing());

        return Result.Success();
    }

    public static Result ValidateForUse(Form form, AnalysisType analysisType)
    {
        var draftValidation = Validate(form);
        if (draftValidation.IsFailure)
            return draftValidation;

        var sections = form.Sections.Where(section => section.DeletedAt == null)
            .OrderBy(section => section.Order).ThenBy(section => section.Id).ToList();
        var errors = sections.Select((section, index) => new { Section = section, Position = index + 1 })
            .Where(item => item.Section.AnalysisType == null || item.Section.AnalysisType == analysisType)
            .Where(item => item.Section.Fields?.Any(field =>
                field.DeletedAt == null && field.Question is { DeletedAt: null }) != true)
            .Select(item => FormErrors.SectionWithoutQuestions(form.Name, item.Section.Name, item.Position))
            .ToList();

        errors.AddRange(sections.Where(section => section.AnalysisType == null || section.AnalysisType == analysisType)
            .SelectMany(section => MicrobialSectionErrors(form.Name, section)));
        return errors.Count != 0 ? Result.Failure(errors) : Result.Success();
    }

    /// <summary>A CFU calculation reads exactly one colony count and one dilution from its own section.</summary>
    public static IEnumerable<Error> MicrobialSectionErrors(string formName, FormSection section)
    {
        var types = section.Fields?.Where(field => field.DeletedAt == null && field.Question is { DeletedAt: null })
            .Select(field => field.Question.Type).ToList() ?? [];
        if (!types.Contains(QuestionType.CfuCalculation)) yield break;
        if (types.Count(type => type == QuestionType.ColonyCount) != 1 || types.Count(type => type == QuestionType.Dilution) != 1)
            yield return Error.Validation("Form.MicrobialSection",
                $"In {FormErrors.PlainName(formName)}, the test \"{FormErrors.PlainName(section.Name)}\" calculates CFU, so it needs exactly one colony count and one dilution question.");
    }

    private static void ValidateQuestionOptions(Question question, List<Error> errors)
    {
        switch (question.Type)
        {
            case QuestionType.ShortAnswer:
            case QuestionType.LongAnswer:
            case QuestionType.Paragraph:
            case QuestionType.FileUpload:
            case QuestionType.Signature:
            case QuestionType.Datepicker:
                // These question types should not have options
                if (question.Options is { Count: > 0 })
                {
                    errors.Add(FormErrors.InvalidQuestionOptions(question.Label, question.Type.ToString()));
                }
                break;

            case QuestionType.Dropdown:
            case QuestionType.SingleChoice:
            case QuestionType.Checkbox:
                // These question types must have options
                if (question.Options == null || question.Options.Count == 0)
                {
                    errors.Add(FormErrors.MissingQuestionOptions(question.Label, question.Type.ToString()));
                }
                break;

            default:
                errors.Add(FormErrors.InvalidQuestionType(question.Type.ToString()));
                break;
        }
    }
}
