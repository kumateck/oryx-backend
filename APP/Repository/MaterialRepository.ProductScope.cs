using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Users;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class MaterialRepository
{
    // Existing lookups retain the user's own linkage. QA passes a product ID, never an arbitrary
    // department ID; read the current product department and live grants for every request.
    private async Task<Result<Guid>> ProductMaterialDepartment(Guid userId, Guid? productId, MaterialKind? kind, ProductContext productContext)
    {
        var user = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId && !u.IsDisabled);
        if (user is null) return UserErrors.NotFound(userId);
        if (!productId.HasValue && productContext == ProductContext.Production)
            return user.DepartmentId.HasValue ? user.DepartmentId.Value : UserErrors.DepartmentNotFound;
        if (!productId.HasValue)
            return Error.Validation("Product.MaterialDepartment", "A product is required for QA material lookups.");
        var permissions = await CurrentPermissionSet.Load(context, userId);
        // The page context chooses its own section keys. Shared transport never lets Production
        // grants open QA reads or vice versa. An unfiltered picker needs both material kinds.
        var canReadRaw = permissions.Contains(ProductPermissions.Key(productContext, ProductSection.Bom, ProductVerb.View));
        var canReadPackaging = permissions.Contains(ProductPermissions.Key(productContext, ProductSection.PackingMaterials, ProductVerb.View));
        var canRead = kind switch
        {
            MaterialKind.Raw => canReadRaw,
            MaterialKind.Package => canReadPackaging,
            null => canReadRaw && canReadPackaging,
            _ => false,
        };
        if (!canRead)
            return Error.Forbidden("Product.MaterialAccess", $"The {productContext} product view permission for this material kind is required.");
        var department = await context.Products.IgnoreQueryFilters().AsNoTracking()
            .Where(p => p.Id == productId && !p.DeletedAt.HasValue && p.DepartmentId.HasValue && p.Department.Type == DepartmentType.Production && !p.Department.DeletedAt.HasValue)
            .Select(p => p.DepartmentId).SingleOrDefaultAsync();
        if (!department.HasValue)
            return Error.NotFound("Product.MaterialDepartment", "The product has no active production department.");
        if (productContext == ProductContext.Production && department != user.DepartmentId)
            return Error.Forbidden("Product.MaterialDepartment", "The product is outside your production department.");
        return department.Value;
    }
}
