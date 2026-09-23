using SHARED;

namespace DOMAIN.Entities.FullProcedures;

public static class TemplateWorkflowErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "TemplateWorkflow.NotFound", "The workflow or revision was not found.");
    public static readonly Error AccessDenied = Error.Forbidden(
        "TemplateWorkflow.AccessDenied", "Your role is not assigned to this workflow operation.");
    public static readonly Error Invalid = Error.Validation(
        "TemplateWorkflow.Invalid", "The workflow revision content is invalid.");
    public static readonly Error Conflict = Error.Conflict(
        "TemplateWorkflow.Conflict", "The workflow revision changed or is not in the required state.");
    public static readonly Error DependencyUnavailable = Error.Conflict(
        "TemplateWorkflow.DependencyUnavailable",
        "A pinned Activity revision is no longer available.");
    public static readonly Error SegregationOfDuties = Error.Forbidden(
        "TemplateWorkflow.SegregationOfDuties",
        "The configured author, reviewer, and publisher independence rule was not met.");
    public static readonly Error InactiveArea = Error.Conflict(
        "TemplateWorkflow.InactiveArea", "An inactive area cannot accept workflow changes.");
}
