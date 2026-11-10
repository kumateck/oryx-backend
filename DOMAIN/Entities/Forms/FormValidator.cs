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

        return errors.Count != 0 ? Result.Failure(errors) : Result.Success();
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
