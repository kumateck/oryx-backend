using System.ComponentModel.DataAnnotations;
using SHARED;

namespace DOMAIN.Entities.Base;

public class UnitOfMeasure : BaseEntity
{
    [StringLength(255)] public string Name { get; set; }
    [StringLength(20)] public string Symbol { get; set; }
    [StringLength(1000)] public string Description { get; set; }
    public bool IsScalable { get; set; }
    public bool IsRawMaterial { get; set; }
    public UnitOfMeasureType Type { get; set; }
    public UnitOfMeasureCategory Category { get; set; }
}

public enum UnitOfMeasureType
{
    Raw = 0,
    Packing = 1,
    Shipping = 2,
}

public enum UnitOfMeasureCategory
{
    Weight = 0,
    Volume = 1,
    Countable = 2,
    Length = 3,
    Area = 4,
}

public class UnitOfMeasureDto : BaseDto
{
    public string Name { get; set; }
    public string Symbol { get; set; }
    public string Description { get; set; }
    public bool IsScalable { get; set; }
    public bool IsRawMaterial { get; set; }
    public UnitOfMeasureType Type { get; set; }
    public UnitOfMeasureCategory Category { get; set; }
}

public class PackageStyleDto : BaseDto
{
    public string Name { get; set; }
    public string Description { get; set; }
}

public class TermsOfPaymentDto : BaseDto
{
    public string Name { get; set; }
    public string Description { get; set; }
}

public class DeliveryModeDto : BaseDto
{
    public string Name { get; set; }
    public string Description { get; set; }
}

public class CreateUnitOfMeasure
{
    public string Name { get; set; }
    public string Symbol { get; set; }
    public string Description { get; set; }
    public bool IsScalable { get; set; }
    public UnitOfMeasureType Type { get; set; }
    public UnitOfMeasureCategory Category { get; set; }
}

public class FilterUnitOfMeasure : PagedQuery
{
    public string SearchQuery { get; set; }
    public List<UnitOfMeasureType> Types { get; set; } = [];
    public List<UnitOfMeasureCategory> Categories { get; set; } = [];
}