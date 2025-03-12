using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;
using SHARED;

namespace DOMAIN.Entities.ProductionSchedules.Packing;

public class CreateFinalPacking
{
    public Guid ProductionScheduleId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? ProductionActivityStepId { get; set; }
    public List<CreateFinalPackingMaterial> Materials { get; set; } = [];
    public decimal NumberOfBottlesPerShipper { get; set; }
    public decimal NUmberOfFullShipperPacked { get; set; }
    public decimal LeftOver { get; set; }
    public decimal BatchSize { get; set; }
    public decimal AverageVolumeFilledPerBottle { get; set; }
    public decimal PackSize { get; set; }
    public decimal ExpectedYield { get; set; }
    public decimal TotalQuantityPacked { get; set; }
    public decimal QualityControlAnalyticalSample { get; set; }
    public decimal RetainedSamples { get; set; }
    public decimal StabilitySamples { get; set; }
    public decimal TotalNumberOfBottles { get; set; }
    public decimal TotalGainOrLoss { get; set; }
}


public class CreateFinalPackingMaterial
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

public class FinalPacking : BaseEntity
{
    public Guid ProductionScheduleId { get; set; }
    public ProductionSchedule ProductionSchedule { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; }
    public Guid? ProductionActivityStepId { get; set; }
    public ProductionActivityStep ProductionActivityStep { get; set; }
    public List<FinalPackingMaterial> Materials { get; set; } = [];
    public decimal NumberOfBottlesPerShipper { get; set; }
    public decimal NUmberOfFullShipperPacked { get; set; }
    public decimal LeftOver { get; set; }
    public decimal BatchSize { get; set; }
    public decimal AverageVolumeFilledPerBottle { get; set; }
    public decimal PackSize { get; set; }
    public decimal ExpectedYield { get; set; }
    public decimal TotalQuantityPacked { get; set; }
    public decimal QualityControlAnalyticalSample { get; set; }
    public decimal RetainedSamples { get; set; }
    public decimal StabilitySamples { get; set; }
    public decimal TotalNumberOfBottles { get; set; }
    public decimal TotalGainOrLoss { get; set; }
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


public class FinalPackingDto : BaseDto
{
    public CollectionItemDto ProductionSchedule { get; set; }
    public CollectionItemDto Product { get; set; }
    public List<FinalPackingMaterialDto> Materials { get; set; } = [];
    public decimal NumberOfBottlesPerShipper { get; set; }
    public decimal NUmberOfFullShipperPacked { get; set; }
    public decimal LeftOver { get; set; }
    public decimal BatchSize { get; set; }
    public decimal AverageVolumeFilledPerBottle { get; set; }
    public decimal PackSize { get; set; }
    public decimal ExpectedYield { get; set; }
    public decimal TotalQuantityPacked { get; set; }
    public decimal QualityControlAnalyticalSample { get; set; }
    public decimal RetainedSamples { get; set; }
    public decimal StabilitySamples { get; set; }
    public decimal TotalNumberOfBottles { get; set; }
    public decimal TotalGainOrLoss { get; set; }
}


public class FinalPackingMaterialDto : BaseDto
{
    public MaterialDto Material { get; set; }
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
