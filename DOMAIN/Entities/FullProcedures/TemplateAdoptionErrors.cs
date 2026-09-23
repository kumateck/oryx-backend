using SHARED;

namespace DOMAIN.Entities.FullProcedures;

public static class TemplateAdoptionErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "TemplateAdoption.NotFound", "The sharing grant or adoption was not found.");
    public static readonly Error AccessDenied = Error.Forbidden(
        "TemplateAdoption.AccessDenied", "The actor cannot adopt into the target area.");
    public static readonly Error Invalid = Error.Validation(
        "TemplateAdoption.Invalid", "The adoption request or local mappings are invalid.");
    public static readonly Error Conflict = Error.Conflict(
        "TemplateAdoption.Conflict", "The grant changed or has already been adopted.");
    public static readonly Error DependencyUnavailable = Error.Conflict(
        "TemplateAdoption.DependencyUnavailable",
        "The source revision, target dependency, role, or area is unavailable.");
}
