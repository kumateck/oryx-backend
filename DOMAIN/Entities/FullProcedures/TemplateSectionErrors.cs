using SHARED;

namespace DOMAIN.Entities.FullProcedures;

public static class TemplateSectionErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "TemplateSection.NotFound", "The section or revision was not found.");
    public static readonly Error AccessDenied = Error.Forbidden(
        "TemplateSection.AccessDenied", "Your role is not assigned to this section operation.");
    public static readonly Error Invalid = Error.Validation(
        "TemplateSection.Invalid", "The section revision content is invalid.");
    public static readonly Error Conflict = Error.Conflict(
        "TemplateSection.Conflict", "The section revision changed or is not in the required state.");
    public static readonly Error DependencyUnavailable = Error.Conflict(
        "TemplateSection.DependencyUnavailable",
        "A pinned question revision is no longer published for this section context.");
    public static readonly Error SegregationOfDuties = Error.Forbidden(
        "TemplateSection.SegregationOfDuties",
        "The configured author, reviewer, and publisher independence rule was not met.");
    public static readonly Error InactiveArea = Error.Conflict(
        "TemplateSection.InactiveArea", "An inactive template area cannot accept section changes.");
}
