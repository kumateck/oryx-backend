using SHARED;

namespace DOMAIN.Entities.BillOfMaterials;

public static class BillOfMaterialErrors
{
    public static Error NotFound(Guid productId) =>
        Error.NotFound("BoM.NotFound", $"The BOM with the Id: {productId} was not found");
}