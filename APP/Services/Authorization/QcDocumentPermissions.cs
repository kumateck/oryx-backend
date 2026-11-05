using APP.Utils;
using DOMAIN.Entities.Materials;

namespace APP.Services.Authorization;

/// <summary>QC documents: standard test procedures, specifications and analytical raw data, for materials and products.</summary>
public enum QcDocument { MaterialStp, MaterialSpecification, MaterialArd, ProductStp, ProductSpecification, ProductArd }

public enum QcTask { Create, Edit, Delete }

/// <summary>
/// One key per QC document task (permissions audit phase 3). Material documents have a raw and a packaging key, chosen
/// from the materials the document belongs to; product documents have one key per task.
/// </summary>
public static class QcDocumentPermissions
{
    private static readonly Dictionary<(QcDocument, QcTask, MaterialKind), string> MaterialKeys = new()
    {
        [(QcDocument.MaterialStp, QcTask.Create, MaterialKind.Raw)] = PermissionKeys.CanCreateRawMaterialStp,
        [(QcDocument.MaterialStp, QcTask.Edit, MaterialKind.Raw)] = PermissionKeys.CanEditRawMaterialStp,
        [(QcDocument.MaterialStp, QcTask.Delete, MaterialKind.Raw)] = PermissionKeys.CanDeleteRawMaterialStp,
        [(QcDocument.MaterialStp, QcTask.Create, MaterialKind.Package)] = PermissionKeys.CanCreatePackagingMaterialStp,
        [(QcDocument.MaterialStp, QcTask.Edit, MaterialKind.Package)] = PermissionKeys.CanEditPackagingMaterialStp,
        [(QcDocument.MaterialStp, QcTask.Delete, MaterialKind.Package)] = PermissionKeys.CanDeletePackagingMaterialStp,
        [(QcDocument.MaterialSpecification, QcTask.Create, MaterialKind.Raw)] = PermissionKeys.CanCreateRawMaterialSpecification,
        [(QcDocument.MaterialSpecification, QcTask.Edit, MaterialKind.Raw)] = PermissionKeys.CanEditRawMaterialSpecification,
        [(QcDocument.MaterialSpecification, QcTask.Delete, MaterialKind.Raw)] = PermissionKeys.CanDeleteRawMaterialSpecification,
        [(QcDocument.MaterialSpecification, QcTask.Create, MaterialKind.Package)] = PermissionKeys.CanCreatePackagingMaterialSpecification,
        [(QcDocument.MaterialSpecification, QcTask.Edit, MaterialKind.Package)] = PermissionKeys.CanEditPackagingMaterialSpecification,
        [(QcDocument.MaterialSpecification, QcTask.Delete, MaterialKind.Package)] = PermissionKeys.CanDeletePackagingMaterialSpecification,
        [(QcDocument.MaterialArd, QcTask.Create, MaterialKind.Raw)] = PermissionKeys.CanCreateRawMaterialArd,
        [(QcDocument.MaterialArd, QcTask.Edit, MaterialKind.Raw)] = PermissionKeys.CanEditRawMaterialArd,
        [(QcDocument.MaterialArd, QcTask.Delete, MaterialKind.Raw)] = PermissionKeys.CanDeleteRawMaterialArd,
        [(QcDocument.MaterialArd, QcTask.Create, MaterialKind.Package)] = PermissionKeys.CanCreatePackagingMaterialArd,
        [(QcDocument.MaterialArd, QcTask.Edit, MaterialKind.Package)] = PermissionKeys.CanEditPackagingMaterialArd,
        [(QcDocument.MaterialArd, QcTask.Delete, MaterialKind.Package)] = PermissionKeys.CanDeletePackagingMaterialArd,
    };

    private static readonly Dictionary<(QcDocument, QcTask), string> ProductKeys = new()
    {
        [(QcDocument.ProductStp, QcTask.Create)] = PermissionKeys.CanCreateProductStp,
        [(QcDocument.ProductStp, QcTask.Edit)] = PermissionKeys.CanEditProductStp,
        [(QcDocument.ProductStp, QcTask.Delete)] = PermissionKeys.CanDeleteProductStp,
        [(QcDocument.ProductSpecification, QcTask.Create)] = PermissionKeys.CanCreateProductSpecification,
        [(QcDocument.ProductSpecification, QcTask.Edit)] = PermissionKeys.CanEditProductSpecification,
        [(QcDocument.ProductSpecification, QcTask.Delete)] = PermissionKeys.CanDeleteProductSpecification,
        [(QcDocument.ProductArd, QcTask.Create)] = PermissionKeys.CanCreateProductArd,
        [(QcDocument.ProductArd, QcTask.Edit)] = PermissionKeys.CanEditProductArd,
        [(QcDocument.ProductArd, QcTask.Delete)] = PermissionKeys.CanDeleteProductArd,
    };

    public static bool IsMaterialDocument(QcDocument document) =>
        document is QcDocument.MaterialStp or QcDocument.MaterialSpecification or QcDocument.MaterialArd;

    public static string MaterialKey(QcDocument document, QcTask task, MaterialKind kind) =>
        MaterialKeys.TryGetValue((document, task, kind), out var key) ? key : null;

    public static string ProductKey(QcDocument document, QcTask task) => ProductKeys.TryGetValue((document, task), out var key) ? key : null;

    /// <summary>Every key a document task can need, in any kind.</summary>
    public static IEnumerable<string> KeysFor(QcDocument document, QcTask task) => IsMaterialDocument(document)
        ? Enum.GetValues<MaterialKind>().Select(kind => MaterialKey(document, task, kind)).Where(key => key is not null)
        : new[] { ProductKey(document, task) }.Where(key => key is not null);

    public static IEnumerable<string> All => MaterialKeys.Values.Concat(ProductKeys.Values);
}
