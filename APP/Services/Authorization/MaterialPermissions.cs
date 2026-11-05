using APP.Utils;
using DOMAIN.Entities.Materials;

namespace APP.Services.Authorization;

/// <summary>Tasks on the material master and its department links, each with a raw and a packaging key.</summary>
public enum MaterialTask { Create, Edit, Delete, Link, Unlink }

/// <summary>
/// One key per task and material kind (permissions audit phase 2). The kind is taken from the stored material whenever
/// the request names one, so a request cannot reach a packaging material with a raw-material key, or the other way.
/// </summary>
public static class MaterialPermissions
{
    private static readonly Dictionary<(MaterialTask, MaterialKind), string> Keys = new()
    {
        [(MaterialTask.Create, MaterialKind.Raw)] = PermissionKeys.CanCreateNewRawMaterials,
        [(MaterialTask.Create, MaterialKind.Package)] = PermissionKeys.CanCreateNewPackagingMaterials,
        [(MaterialTask.Edit, MaterialKind.Raw)] = PermissionKeys.CanEditRawMaterials,
        [(MaterialTask.Edit, MaterialKind.Package)] = PermissionKeys.CanEditPackagingMaterials,
        [(MaterialTask.Delete, MaterialKind.Raw)] = PermissionKeys.CanDeleteRawMaterials,
        [(MaterialTask.Delete, MaterialKind.Package)] = PermissionKeys.CanDeletePackagingMaterials,
        [(MaterialTask.Link, MaterialKind.Raw)] = PermissionKeys.CanLinkRawMaterials,
        [(MaterialTask.Link, MaterialKind.Package)] = PermissionKeys.CanLinkPackagingMaterials,
        [(MaterialTask.Unlink, MaterialKind.Raw)] = PermissionKeys.CanUnlinkRawMaterials,
        [(MaterialTask.Unlink, MaterialKind.Package)] = PermissionKeys.CanUnlinkPackagingMaterials,
    };

    public static string Key(MaterialTask task, MaterialKind kind) => Keys.TryGetValue((task, kind), out var key) ? key : null;

    public static IEnumerable<string> All => Keys.Values;
}
