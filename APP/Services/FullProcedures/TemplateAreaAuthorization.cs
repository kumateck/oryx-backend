using APP.Utils;

namespace APP.Services.FullProcedures;

public enum TemplateAreaOperation
{
    ViewCatalog = 0,
    ManageArea = 1,
    ReviewRevision = 2,
    PublishRevision = 3,
    ShareRevision = 4,
    ReadHrResponse = 5,
    ReadQcResponse = 6,
    ReadMicrobiologyResponse = 7,
}

public enum TemplateAreaAccessDenial
{
    MissingCapability = 0,
    AreaNotAssigned = 1,
    OwnerGroupNotAssigned = 2,
}

public sealed record TemplateAreaActorScope(
    IReadOnlySet<string> PermissionKeys,
    IReadOnlySet<Guid> AreaIds,
    IReadOnlySet<Guid> OwnerGroupIds
);

public static class TemplateAreaAuthorization
{
    public static IReadOnlySet<TemplateAreaAccessDenial> Authorize(
        TemplateAreaOperation operation,
        TemplateAreaActorScope actor,
        Guid? areaId = null,
        Guid? ownerGroupId = null)
    {
        var denials = new HashSet<TemplateAreaAccessDenial>();
        if (!actor.PermissionKeys.Contains(RequiredPermission(operation)))
            denials.Add(TemplateAreaAccessDenial.MissingCapability);
        if (areaId.HasValue && !actor.AreaIds.Contains(areaId.Value))
            denials.Add(TemplateAreaAccessDenial.AreaNotAssigned);
        if (operation == TemplateAreaOperation.ManageArea &&
            ownerGroupId.HasValue &&
            !actor.OwnerGroupIds.Contains(ownerGroupId.Value))
            denials.Add(TemplateAreaAccessDenial.OwnerGroupNotAssigned);
        return denials;
    }

    public static string RequiredPermission(TemplateAreaOperation operation) => operation switch
    {
        TemplateAreaOperation.ViewCatalog => FullProcedurePermissionKeys.CanViewQuestionTemplates,
        TemplateAreaOperation.ManageArea => FullProcedurePermissionKeys.CanManageTemplateAreas,
        TemplateAreaOperation.ReviewRevision => FullProcedurePermissionKeys.CanReviewTemplateRevision,
        TemplateAreaOperation.PublishRevision => FullProcedurePermissionKeys.CanPublishTemplateRevision,
        TemplateAreaOperation.ShareRevision => FullProcedurePermissionKeys.CanShareTemplateRevision,
        TemplateAreaOperation.ReadHrResponse => FullProcedurePermissionKeys.CanViewHrTemplateResponse,
        TemplateAreaOperation.ReadQcResponse => FullProcedurePermissionKeys.CanViewQcTemplateResponse,
        TemplateAreaOperation.ReadMicrobiologyResponse => FullProcedurePermissionKeys.CanViewMicrobiologyTemplateResponse,
        _ => throw new ArgumentOutOfRangeException(nameof(operation)),
    };
}
