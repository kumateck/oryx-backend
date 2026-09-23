using SHARED;

namespace DOMAIN.Entities.FullProcedures;

public static class TemplateActivityErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "TemplateActivity.NotFound", "The activity or revision was not found.");
    public static readonly Error AccessDenied = Error.Forbidden(
        "TemplateActivity.AccessDenied", "Your role is not assigned to this activity operation.");
    public static readonly Error Invalid = Error.Validation(
        "TemplateActivity.Invalid", "The activity revision content is invalid.");
    public static readonly Error Conflict = Error.Conflict(
        "TemplateActivity.Conflict", "The activity revision changed or is not in the required state.");
    public static readonly Error DependencyUnavailable = Error.Conflict(
        "TemplateActivity.DependencyUnavailable",
        "A pinned form, role, or resource capability is no longer available.");
    public static readonly Error SegregationOfDuties = Error.Forbidden(
        "TemplateActivity.SegregationOfDuties",
        "The configured author, reviewer, and publisher independence rule was not met.");
    public static readonly Error InactiveArea = Error.Conflict(
        "TemplateActivity.InactiveArea", "An inactive area cannot accept activity changes.");
}
