using SHARED;

namespace DOMAIN.Entities.FullProcedures;

public static class ProcedureErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Procedure.NotFound", "The Procedure definition or revision was not found.");
    public static readonly Error AccessDenied = Error.Forbidden(
        "Procedure.AccessDenied", "Your role is not assigned to this Procedure operation.");
    public static readonly Error Invalid = Error.Validation(
        "Procedure.Invalid", "The Procedure revision content is invalid.");
    public static readonly Error Conflict = Error.Conflict(
        "Procedure.Conflict", "The Procedure changed or is not in the required state.");
    public static readonly Error DependencyUnavailable = Error.Conflict(
        "Procedure.DependencyUnavailable", "The pinned Workflow or applicability is unavailable.");
    public static readonly Error SegregationOfDuties = Error.Forbidden(
        "Procedure.SegregationOfDuties",
        "The configured author, reviewer, and approver independence rule was not met.");
    public static readonly Error InactiveArea = Error.Conflict(
        "Procedure.InactiveArea", "An inactive area cannot accept Procedure changes.");
}
