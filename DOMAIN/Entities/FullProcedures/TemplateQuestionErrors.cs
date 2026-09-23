using SHARED;

namespace DOMAIN.Entities.FullProcedures;

public static class TemplateQuestionErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "TemplateQuestion.NotFound", "The question or revision was not found.");
    public static readonly Error AccessDenied = Error.Forbidden(
        "TemplateQuestion.AccessDenied", "Your role is not assigned to this question operation.");
    public static readonly Error Invalid = Error.Validation(
        "TemplateQuestion.Invalid", "The question revision content is invalid.");
    public static readonly Error Conflict = Error.Conflict(
        "TemplateQuestion.Conflict", "The question revision changed or is not in the required state.");
    public static readonly Error SegregationOfDuties = Error.Forbidden(
        "TemplateQuestion.SegregationOfDuties", "The configured author, reviewer, and publisher independence rule was not met.");
    public static readonly Error InactiveArea = Error.Conflict(
        "TemplateQuestion.InactiveArea", "An inactive template area cannot accept question changes.");
}
