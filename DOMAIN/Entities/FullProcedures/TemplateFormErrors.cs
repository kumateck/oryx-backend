using SHARED;

namespace DOMAIN.Entities.FullProcedures;

public static class TemplateFormErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "TemplateForm.NotFound", "The form or revision was not found.");
    public static readonly Error AccessDenied = Error.Forbidden(
        "TemplateForm.AccessDenied", "Your role is not assigned to this form operation.");
    public static readonly Error Invalid = Error.Validation(
        "TemplateForm.Invalid", "The form revision content is invalid.");
    public static readonly Error Conflict = Error.Conflict(
        "TemplateForm.Conflict", "The form revision changed or is not in the required state.");
    public static readonly Error DependencyUnavailable = Error.Conflict(
        "TemplateForm.DependencyUnavailable",
        "A pinned section revision is no longer published for this form context.");
    public static readonly Error SegregationOfDuties = Error.Forbidden(
        "TemplateForm.SegregationOfDuties",
        "The configured author, reviewer, and publisher independence rule was not met.");
    public static readonly Error InactiveArea = Error.Conflict(
        "TemplateForm.InactiveArea", "An inactive template area cannot accept form changes.");
}
