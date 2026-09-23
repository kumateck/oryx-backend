using SHARED;

namespace DOMAIN.Entities.FullProcedures;

public static class TemplateSharingErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "TemplateSharing.NotFound", "The sharing grant or template revision was not found.");
    public static readonly Error AccessDenied = Error.Forbidden(
        "TemplateSharing.AccessDenied", "The actor is not assigned to the required template area.");
    public static readonly Error Invalid = Error.Validation(
        "TemplateSharing.Invalid", "The template sharing request is invalid.");
    public static readonly Error Conflict = Error.Conflict(
        "TemplateSharing.Conflict", "The sharing grant changed or is not in the required state.");
    public static readonly Error DependencyUnavailable = Error.Conflict(
        "TemplateSharing.DependencyUnavailable",
        "The exact Published revision is no longer available in the source context.");
    public static readonly Error SegregationOfDuties = Error.Forbidden(
        "TemplateSharing.SegregationOfDuties",
        "The requester cannot make the receiving-area decision on the same grant.");
}
