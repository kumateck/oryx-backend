using DOMAIN.Entities.Products;

namespace APP.Repository;

internal static class ProductPackagingSelection
{
    // The batch's saved style overrides the default for that material. Other styles never apply.
    internal static List<ProductPackage> ForStyle(IEnumerable<ProductPackage> packages, Guid? packingStyleId)
    {
        var applicable = packages.Where(package => package.DeletedAt == null
            && (package.ProductPackingId == null || package.ProductPackingId == packingStyleId)).ToList();
        var specificMaterials = applicable.Where(package => package.ProductPackingId.HasValue)
            .Select(package => package.MaterialId).ToHashSet();
        // Keep duplicate pairs visible to validation rather than silently choosing one of them.
        return applicable.Where(package => package.ProductPackingId.HasValue || !specificMaterials.Contains(package.MaterialId)).ToList();
    }
}
