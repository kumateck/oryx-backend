using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.Users;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class MaterialRepository
{
    // Existing lookups retain the user's own linkage. QA passes a product ID, never an arbitrary
    // department ID; read the current product department and live grants for every request.
    private async Task<Result<Guid>> ProductMaterialDepartment(Guid userId, Guid? productId)
    {
        var user = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId && !u.IsDisabled);
        if (user is null) return UserErrors.NotFound(userId);
        if (!productId.HasValue)
            return user.DepartmentId.HasValue ? user.DepartmentId.Value : UserErrors.DepartmentNotFound;
        var permissions = await CurrentPermissionSet.Load(context, userId);
        if (!permissions.Contains(PermissionKeys.CanViewPlannedProducts) || !permissions.Contains(PermissionKeys.CanEditProductionPlan))
            return Error.Forbidden("Product.MaterialAccess", "Product view and edit permissions are required for product-scoped materials.");
        var department = await context.Products.IgnoreQueryFilters().AsNoTracking()
            .Where(p => p.Id == productId && !p.DeletedAt.HasValue && p.DepartmentId.HasValue && p.Department.Type == DepartmentType.Production && !p.Department.DeletedAt.HasValue)
            .Select(p => p.DepartmentId).SingleOrDefaultAsync();
        return department.HasValue ? department.Value
            : Error.NotFound("Product.MaterialDepartment", "The product has no active production department.");
    }
}
