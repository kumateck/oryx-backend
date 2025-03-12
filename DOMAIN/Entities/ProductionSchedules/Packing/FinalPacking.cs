using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;

namespace DOMAIN.Entities.ProductionSchedules.Packing;

public class FinalPacking : BaseEntity
{
    public Guid ProductionScheduleId { get; set; }
    public Guid ProductId { get; set; }
    public Guid ProductionActivityStepId { get; set; }
    public List<FinalPackingMaterial> Materials { get; set; }
    public decimal NumberOfBottlesPerShipper { get; set; }
    public decimal NUmberOfFullShipperPacked { get; set; }
    public decimal LeftOver { get; set; }
    public decimal TotalQuantityPacked { get; set; }
    public decimal BatchSize { get; set; }
    public decimal AverageVolumeFilledPerBottle { get; set; }
}


public class FinalPackingMaterial : BaseEntity
{
    public Guid FinalPackingId { get; set; }
    public FinalPacking FinalPacking { get; set; }
    public Guid MaterialId { get; set; }
    public Material Material { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal SubsequentDeliveredQuantity { get; set; }
    public decimal TotalReceivedQuantity { get; set; }
    public decimal PackedQuantity { get; set; }
    public decimal ReturnedQuantity { get; set; }
    public decimal RejectedQuantity { get; set; }
    public decimal SampledQuantity { get; set; }
    public decimal TotalAccountedForQuantity { get; set; }
    public decimal PercentageLoss { get; set; }
}

public class FinalPackingYield : BaseEntity
{
    
}