using APP.Utils;

namespace APP.Services.Authorization;

/// <summary>Who is working on a product: Production authors it, QA reviews and maintains it.</summary>
public enum ProductContext { Production, Qa }

/// <summary>The parts of a product that are viewed and changed separately.</summary>
public enum ProductSection { List, Info, Bom, PackingMaterials, PackingStyles, PackingOrder, Procedure }

/// <summary>
/// What a request does to a section. <see cref="Save"/> is a bulk save of a whole section: the rows are compared with
/// the stored ones and each kind of change (add, edit, delete) needs its own permission.
/// </summary>
public enum ProductVerb { View, Create, Add, Edit, Delete, Archive, Import, Export, Save }

/// <summary>One permission per task on a product, per context. Nothing in one context grants anything in another.</summary>
public static class ProductPermissions
{
    private static readonly Dictionary<(ProductContext, ProductSection, ProductVerb), string> Keys = new()
    {
        [(ProductContext.Production, ProductSection.List, ProductVerb.View)] = PermissionKeys.CanViewProductionProducts,
        [(ProductContext.Production, ProductSection.List, ProductVerb.Import)] = PermissionKeys.CanImportProductionProducts,
        [(ProductContext.Production, ProductSection.List, ProductVerb.Export)] = PermissionKeys.CanExportProductionProducts,
        [(ProductContext.Production, ProductSection.Info, ProductVerb.View)] = PermissionKeys.CanViewProductionProductInfo,
        [(ProductContext.Production, ProductSection.Info, ProductVerb.Create)] = PermissionKeys.CanCreateProductionProduct,
        [(ProductContext.Production, ProductSection.Info, ProductVerb.Edit)] = PermissionKeys.CanEditProductionProductInfo,
        [(ProductContext.Production, ProductSection.Info, ProductVerb.Delete)] = PermissionKeys.CanDeleteProductionProduct,
        [(ProductContext.Production, ProductSection.Bom, ProductVerb.View)] = PermissionKeys.CanViewProductionProductBom,
        [(ProductContext.Production, ProductSection.Bom, ProductVerb.Add)] = PermissionKeys.CanAddProductionProductBom,
        [(ProductContext.Production, ProductSection.Bom, ProductVerb.Edit)] = PermissionKeys.CanEditProductionProductBom,
        [(ProductContext.Production, ProductSection.Bom, ProductVerb.Delete)] = PermissionKeys.CanDeleteProductionProductBom,
        [(ProductContext.Production, ProductSection.Bom, ProductVerb.Archive)] = PermissionKeys.CanArchiveProductionProductBom,
        [(ProductContext.Production, ProductSection.Bom, ProductVerb.Import)] = PermissionKeys.CanImportProductionProductBom,
        [(ProductContext.Production, ProductSection.PackingMaterials, ProductVerb.View)] = PermissionKeys.CanViewProductionProductPackingMaterials,
        [(ProductContext.Production, ProductSection.PackingMaterials, ProductVerb.Add)] = PermissionKeys.CanAddProductionProductPackingMaterials,
        [(ProductContext.Production, ProductSection.PackingMaterials, ProductVerb.Edit)] = PermissionKeys.CanEditProductionProductPackingMaterials,
        [(ProductContext.Production, ProductSection.PackingMaterials, ProductVerb.Delete)] = PermissionKeys.CanDeleteProductionProductPackingMaterials,
        [(ProductContext.Production, ProductSection.PackingMaterials, ProductVerb.Import)] = PermissionKeys.CanImportProductionProductPackingMaterials,
        [(ProductContext.Production, ProductSection.PackingStyles, ProductVerb.View)] = PermissionKeys.CanViewProductionProductPackingStyles,
        [(ProductContext.Production, ProductSection.PackingStyles, ProductVerb.Add)] = PermissionKeys.CanAddProductionProductPackingStyles,
        [(ProductContext.Production, ProductSection.PackingStyles, ProductVerb.Edit)] = PermissionKeys.CanEditProductionProductPackingStyles,
        [(ProductContext.Production, ProductSection.PackingStyles, ProductVerb.Delete)] = PermissionKeys.CanDeleteProductionProductPackingStyles,
        [(ProductContext.Production, ProductSection.PackingOrder, ProductVerb.View)] = PermissionKeys.CanViewProductionProductPackingOrder,
        [(ProductContext.Production, ProductSection.PackingOrder, ProductVerb.Edit)] = PermissionKeys.CanEditProductionProductPackingOrder,
        [(ProductContext.Production, ProductSection.Procedure, ProductVerb.View)] = PermissionKeys.CanViewProductionProductProcedure,
        [(ProductContext.Production, ProductSection.Procedure, ProductVerb.Add)] = PermissionKeys.CanAddProductionProductProcedure,
        [(ProductContext.Production, ProductSection.Procedure, ProductVerb.Edit)] = PermissionKeys.CanEditProductionProductProcedure,
        [(ProductContext.Production, ProductSection.Procedure, ProductVerb.Delete)] = PermissionKeys.CanDeleteProductionProductProcedure,

        [(ProductContext.Qa, ProductSection.List, ProductVerb.View)] = PermissionKeys.CanViewQaProducts,
        [(ProductContext.Qa, ProductSection.Info, ProductVerb.View)] = PermissionKeys.CanViewQaProductInfo,
        [(ProductContext.Qa, ProductSection.Info, ProductVerb.Edit)] = PermissionKeys.CanEditQaProductInfo,
        [(ProductContext.Qa, ProductSection.Bom, ProductVerb.View)] = PermissionKeys.CanViewQaProductBom,
        [(ProductContext.Qa, ProductSection.Bom, ProductVerb.Add)] = PermissionKeys.CanAddQaProductBom,
        [(ProductContext.Qa, ProductSection.Bom, ProductVerb.Edit)] = PermissionKeys.CanEditQaProductBom,
        [(ProductContext.Qa, ProductSection.Bom, ProductVerb.Delete)] = PermissionKeys.CanDeleteQaProductBom,
        [(ProductContext.Qa, ProductSection.Bom, ProductVerb.Archive)] = PermissionKeys.CanArchiveQaProductBom,
        [(ProductContext.Qa, ProductSection.PackingMaterials, ProductVerb.View)] = PermissionKeys.CanViewQaProductPackingMaterials,
        [(ProductContext.Qa, ProductSection.PackingMaterials, ProductVerb.Add)] = PermissionKeys.CanAddQaProductPackingMaterials,
        [(ProductContext.Qa, ProductSection.PackingMaterials, ProductVerb.Edit)] = PermissionKeys.CanEditQaProductPackingMaterials,
        [(ProductContext.Qa, ProductSection.PackingMaterials, ProductVerb.Delete)] = PermissionKeys.CanDeleteQaProductPackingMaterials,
        [(ProductContext.Qa, ProductSection.PackingStyles, ProductVerb.View)] = PermissionKeys.CanViewQaProductPackingStyles,
        [(ProductContext.Qa, ProductSection.PackingStyles, ProductVerb.Add)] = PermissionKeys.CanAddQaProductPackingStyles,
        [(ProductContext.Qa, ProductSection.PackingStyles, ProductVerb.Edit)] = PermissionKeys.CanEditQaProductPackingStyles,
        [(ProductContext.Qa, ProductSection.PackingStyles, ProductVerb.Delete)] = PermissionKeys.CanDeleteQaProductPackingStyles,
        [(ProductContext.Qa, ProductSection.PackingOrder, ProductVerb.View)] = PermissionKeys.CanViewQaProductPackingOrder,
        [(ProductContext.Qa, ProductSection.PackingOrder, ProductVerb.Edit)] = PermissionKeys.CanEditQaProductPackingOrder,
        [(ProductContext.Qa, ProductSection.Procedure, ProductVerb.View)] = PermissionKeys.CanViewQaProductProcedure,
        [(ProductContext.Qa, ProductSection.Procedure, ProductVerb.Add)] = PermissionKeys.CanAddQaProductProcedure,
        [(ProductContext.Qa, ProductSection.Procedure, ProductVerb.Edit)] = PermissionKeys.CanEditQaProductProcedure,
        [(ProductContext.Qa, ProductSection.Procedure, ProductVerb.Delete)] = PermissionKeys.CanDeleteQaProductProcedure,
    };

    /// <summary>The key for a task, or null when that context cannot do it (for example QA does not create products).</summary>
    public static string Key(ProductContext context, ProductSection section, ProductVerb verb) =>
        Keys.TryGetValue((context, section, verb), out var key) ? key : null;

    /// <summary>The request's <c>context</c> query value: <c>qa</c> for the QA product pages, otherwise Production.</summary>
    public static ProductContext Parse(string context) =>
        string.Equals(context?.Trim(), "qa", StringComparison.OrdinalIgnoreCase) ? ProductContext.Qa : ProductContext.Production;

    public static IEnumerable<string> All => Keys.Values;
}
