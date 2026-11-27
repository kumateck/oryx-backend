using APP.Utils;
using DOMAIN.Entities.StpRevisions;

namespace APP.Services.Authorization;

/// <summary>The tasks on a structured STP revision; each has its own key per kind.</summary>
public enum StpRevisionTask { View, Create, Import, EditDraft, Submit, Edit, Revise, Review, Return, Approve, Reject, Delete, Export, Print }

/// <summary>
/// One key per STP revision task and kind (raw material, packaging material, product). The kind of an existing revision
/// is always read from the stored record, never from the request.
/// </summary>
public static class StpRevisionPermissions
{
    private static readonly Dictionary<(StpRevisionTask, StpRevisionKind), string> Keys = new()
    {
        [(StpRevisionTask.View, StpRevisionKind.RawMaterial)] = PermissionKeys.CanViewRawMaterialStpRevisions,
        [(StpRevisionTask.Create, StpRevisionKind.RawMaterial)] = PermissionKeys.CanCreateRawMaterialStpRevision,
        [(StpRevisionTask.Import, StpRevisionKind.RawMaterial)] = PermissionKeys.CanImportRawMaterialStpRevision,
        [(StpRevisionTask.EditDraft, StpRevisionKind.RawMaterial)] = PermissionKeys.CanEditRawMaterialStpRevisionDraft,
        [(StpRevisionTask.Submit, StpRevisionKind.RawMaterial)] = PermissionKeys.CanSubmitRawMaterialStpRevision,
        [(StpRevisionTask.Edit, StpRevisionKind.RawMaterial)] = PermissionKeys.CanEditRawMaterialStpRevision,
        [(StpRevisionTask.Revise, StpRevisionKind.RawMaterial)] = PermissionKeys.CanReviseRawMaterialStpRevision,
        [(StpRevisionTask.Review, StpRevisionKind.RawMaterial)] = PermissionKeys.CanReviewRawMaterialStpRevision,
        [(StpRevisionTask.Return, StpRevisionKind.RawMaterial)] = PermissionKeys.CanReturnRawMaterialStpRevision,
        [(StpRevisionTask.Approve, StpRevisionKind.RawMaterial)] = PermissionKeys.CanApproveRawMaterialStpRevision,
        [(StpRevisionTask.Reject, StpRevisionKind.RawMaterial)] = PermissionKeys.CanRejectRawMaterialStpRevision,
        [(StpRevisionTask.Delete, StpRevisionKind.RawMaterial)] = PermissionKeys.CanDeleteRawMaterialStpRevision,
        [(StpRevisionTask.Export, StpRevisionKind.RawMaterial)] = PermissionKeys.CanExportRawMaterialStpRevision,
        [(StpRevisionTask.Print, StpRevisionKind.RawMaterial)] = PermissionKeys.CanPrintRawMaterialStpRevision,

        [(StpRevisionTask.View, StpRevisionKind.PackagingMaterial)] = PermissionKeys.CanViewPackagingMaterialStpRevisions,
        [(StpRevisionTask.Create, StpRevisionKind.PackagingMaterial)] = PermissionKeys.CanCreatePackagingMaterialStpRevision,
        [(StpRevisionTask.Import, StpRevisionKind.PackagingMaterial)] = PermissionKeys.CanImportPackagingMaterialStpRevision,
        [(StpRevisionTask.EditDraft, StpRevisionKind.PackagingMaterial)] = PermissionKeys.CanEditPackagingMaterialStpRevisionDraft,
        [(StpRevisionTask.Submit, StpRevisionKind.PackagingMaterial)] = PermissionKeys.CanSubmitPackagingMaterialStpRevision,
        [(StpRevisionTask.Edit, StpRevisionKind.PackagingMaterial)] = PermissionKeys.CanEditPackagingMaterialStpRevision,
        [(StpRevisionTask.Revise, StpRevisionKind.PackagingMaterial)] = PermissionKeys.CanRevisePackagingMaterialStpRevision,
        [(StpRevisionTask.Review, StpRevisionKind.PackagingMaterial)] = PermissionKeys.CanReviewPackagingMaterialStpRevision,
        [(StpRevisionTask.Return, StpRevisionKind.PackagingMaterial)] = PermissionKeys.CanReturnPackagingMaterialStpRevision,
        [(StpRevisionTask.Approve, StpRevisionKind.PackagingMaterial)] = PermissionKeys.CanApprovePackagingMaterialStpRevision,
        [(StpRevisionTask.Reject, StpRevisionKind.PackagingMaterial)] = PermissionKeys.CanRejectPackagingMaterialStpRevision,
        [(StpRevisionTask.Delete, StpRevisionKind.PackagingMaterial)] = PermissionKeys.CanDeletePackagingMaterialStpRevision,
        [(StpRevisionTask.Export, StpRevisionKind.PackagingMaterial)] = PermissionKeys.CanExportPackagingMaterialStpRevision,
        [(StpRevisionTask.Print, StpRevisionKind.PackagingMaterial)] = PermissionKeys.CanPrintPackagingMaterialStpRevision,

        [(StpRevisionTask.View, StpRevisionKind.Product)] = PermissionKeys.CanViewProductStpRevisions,
        [(StpRevisionTask.Create, StpRevisionKind.Product)] = PermissionKeys.CanCreateProductStpRevision,
        [(StpRevisionTask.Import, StpRevisionKind.Product)] = PermissionKeys.CanImportProductStpRevision,
        [(StpRevisionTask.EditDraft, StpRevisionKind.Product)] = PermissionKeys.CanEditProductStpRevisionDraft,
        [(StpRevisionTask.Submit, StpRevisionKind.Product)] = PermissionKeys.CanSubmitProductStpRevision,
        [(StpRevisionTask.Edit, StpRevisionKind.Product)] = PermissionKeys.CanEditProductStpRevision,
        [(StpRevisionTask.Revise, StpRevisionKind.Product)] = PermissionKeys.CanReviseProductStpRevision,
        [(StpRevisionTask.Review, StpRevisionKind.Product)] = PermissionKeys.CanReviewProductStpRevision,
        [(StpRevisionTask.Return, StpRevisionKind.Product)] = PermissionKeys.CanReturnProductStpRevision,
        [(StpRevisionTask.Approve, StpRevisionKind.Product)] = PermissionKeys.CanApproveProductStpRevision,
        [(StpRevisionTask.Reject, StpRevisionKind.Product)] = PermissionKeys.CanRejectProductStpRevision,
        [(StpRevisionTask.Delete, StpRevisionKind.Product)] = PermissionKeys.CanDeleteProductStpRevision,
        [(StpRevisionTask.Export, StpRevisionKind.Product)] = PermissionKeys.CanExportProductStpRevision,
        [(StpRevisionTask.Print, StpRevisionKind.Product)] = PermissionKeys.CanPrintProductStpRevision,
    };

    public static string Key(StpRevisionTask task, StpRevisionKind kind) => Keys[(task, kind)];

    /// <summary>Every key the task can need, one per kind.</summary>
    public static IReadOnlyList<string> KeysFor(StpRevisionTask task) =>
        Enum.GetValues<StpRevisionKind>().Select(kind => Key(task, kind)).ToList();

    public static IEnumerable<string> All => Keys.Values;
}
