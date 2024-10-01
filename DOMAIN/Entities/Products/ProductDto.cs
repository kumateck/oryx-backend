using DOMAIN.Entities.Base;
using DOMAIN.Entities.BillOfMaterials;
using SHARED;

namespace DOMAIN.Entities.Products;

public class ProductDto : ProductListDto
{
    public List<FinishedProductDto> FinishedProducts { get; set; }
    public List<ProductBillOfMaterialDto> BillOfMaterials { get; set; } = [];
    public List<ProductPackageDto> Packages { get; set; }
}

public class ProductBillOfMaterialDto
{
    public Guid ProductId { get; set; }
    public BillOfMaterialDto BillOfMaterial { get; set; }
    public int Version { get; set; }   // Version of the BOM
    public DateTime EffectiveDate { get; set; }
    public bool IsActive { get; set; }
}