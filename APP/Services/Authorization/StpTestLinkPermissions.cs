using APP.Utils;
using DOMAIN.Entities.StpRevisions;

namespace APP.Services.Authorization;

/// <summary>
/// Citing an STP test from a specification test is part of the specification: reading the links takes the
/// specification's view key, changing one its edit key, each in the specification's kind.
/// </summary>
public static class StpTestLinkPermissions
{
    public static string Key(StpRevisionKind kind, bool write) => (kind, write) switch
    {
        (StpRevisionKind.RawMaterial, false) => PermissionKeys.CanViewRawMaterialSpecifications,
        (StpRevisionKind.RawMaterial, true) => PermissionKeys.CanEditRawMaterialSpecification,
        (StpRevisionKind.PackagingMaterial, false) => PermissionKeys.CanViewPackagingMaterialSpecifications,
        (StpRevisionKind.PackagingMaterial, true) => PermissionKeys.CanEditPackagingMaterialSpecification,
        (StpRevisionKind.Product, false) => PermissionKeys.CanViewProductSpecifications,
        _ => PermissionKeys.CanEditProductSpecification,
    };

    public static IReadOnlyList<string> KeysFor(bool write) => Enum.GetValues<StpRevisionKind>().Select(kind => Key(kind, write)).ToList();
}
