using SHARED;

namespace DOMAIN.Entities.Products;

public static class ProductErrors
{
    public static Error NotFound(Guid productId) =>
        Error.NotFound("Products.NotFound", $"The product with the Id: {productId} was not found");

    public static Error InUse(IReadOnlyList<string> uses) =>
        Error.Conflict("Products.InUse", $"This product cannot be deleted because it is used by {string.Join(", ", uses)}.");
}