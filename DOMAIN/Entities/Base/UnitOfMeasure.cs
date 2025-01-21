using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Base;

public class UnitOfMeasure : BaseEntity
{
    [StringLength(255)] public string Name { get; set; }
    [StringLength(20)] public string Symbol { get; set; }
    [StringLength(1000)] public string Description { get; set; }
    public bool IsScalable { get; set; }
}

public class UnitOfMeasureDto : BaseDto
{
    public string Name { get; set; }
    public string Symbol { get; set; }
    public string Description { get; set; }
    public bool IsScalable { get; set; }
}