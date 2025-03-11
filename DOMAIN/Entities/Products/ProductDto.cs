using DOMAIN.Entities.BillOfMaterials;
using DOMAIN.Entities.Routes;
using SHARED;

namespace DOMAIN.Entities.Products;

public class ProductDto : ProductListDto
{
    public List<FinishedProductDto> FinishedProducts { get; set; } = [];
    public List<ProductBillOfMaterialDto> BillOfMaterials { get; set; } = [];
    public ProductBillOfMaterialDto CurrentBillOfMaterial { get; set; } 
    public List<ProductBillOfMaterialDto> OutdatedBillOfMaterials { get; set; } = [];
    public List<ProductPackageDto> Packages { get; set; } = [];
    public List<RouteDto> Routes { get; set; } = [];
    public CollectionItemDto CreatedBy { get; set; }
}

public class ProductBillOfMaterialDto
{
    public Guid ProductId { get; set; }
    public BillOfMaterialDto BillOfMaterial { get; set; }
    public int Version { get; set; } 
    public DateTime EffectiveDate { get; set; }
    public bool IsActive { get; set; }
}