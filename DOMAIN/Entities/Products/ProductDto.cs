using DOMAIN.Entities.Base;
using DOMAIN.Entities.BillOfMaterials;
using DOMAIN.Entities.Routes;
using SHARED;

namespace DOMAIN.Entities.Products;

public class ProductDto : ProductListDto
{
    public List<ProductBillOfMaterialDto> BillOfMaterials { get; set; } = [];
    public ProductBillOfMaterialDto CurrentBillOfMaterial { get; set; } 
    public List<ProductBillOfMaterialDto> OutdatedBillOfMaterials { get; set; } = [];
    public List<ProductPackageDto> Packages { get; set; } = [];
    public List<RouteDto> Routes { get; set; } = [];
    public List<ProductPackingDto> Packings { get; set; } = [];
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

public class CreateProductPacking
{
    public string Name { get; set; }
    public string Description { get; set; }
    public List<CreateProductPackingList> PackingLists { get; set; } = [];
}

public class CreateProductPackingList
{
    public Guid UomId { get; set; }
    public decimal Quantity { get; set; }
    public int Order { get; set; }
}
public class ProductPackingDto : BaseDto
{
    public string Name { get; set; }
    public string Description { get; set; }
    public List<ProductPackingListDto> PackingLists { get; set; } = [];
}

public class ProductPackingListDto
{
    public UnitOfMeasureDto Uom { get; set; }
    public decimal Quantity { get; set; }
    public int Order { get; set; }
}
