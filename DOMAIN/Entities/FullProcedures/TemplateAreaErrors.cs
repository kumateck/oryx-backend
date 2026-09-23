using SHARED;

namespace DOMAIN.Entities.FullProcedures;

public static class TemplateAreaErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "TemplateArea.NotFound",
        "The template area was not found."
    );
    public static readonly Error AccessDenied = Error.Forbidden(
        "TemplateArea.AccessDenied",
        "You are not assigned to this template area."
    );
    public static readonly Error AdministratorRequired = Error.Forbidden(
        "TemplateArea.AdministratorRequired",
        "An administrator grant for this template area is required."
    );
    public static readonly Error VersionConflict = Error.Conflict(
        "TemplateArea.VersionConflict",
        "The template area changed after it was loaded. Refresh and try again."
    );
    public static readonly Error DuplicateName = Error.Conflict(
        "TemplateArea.DuplicateName",
        "An active template area already uses this name."
    );
    public static readonly Error ReasonRequired = Error.Validation(
        "TemplateArea.ReasonRequired",
        "A reason is required for this governed configuration change."
    );

    public static Error Invalid(string code) => Error.Validation(
        $"TemplateArea.{code}",
        $"The template area configuration is invalid: {code}."
    );
}
