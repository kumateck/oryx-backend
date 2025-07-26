using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Instruments;
using DOMAIN.Entities.Materials.Batch;
using SHARED;

namespace DOMAIN.Entities.UniformityOfWeights;

public class CreateUniformityOfWeight
{
    public string Name { get; set; }
    public string BalanceNumber { get; set; }
    public int NumberOfItems { get; set; }
    public decimal NominalWeight { get; set; }
    public string ItemType { get; set; }
    public string DisintegrationTest { get; set; }
    public Guid? DisintegrationInstrumentId { get; set; }
    public decimal DisintegrationMean { get; set; }
    public string HardnessTest { get; set; }
    public Guid? HardnessInstrumentId { get; set; }
    public decimal HardnessMean { get; set; }
}

public class UniformityOfWeight : BaseEntity
{
    [StringLength(1000)] public string Name { get; set; }
    [StringLength(1000)] public string BalanceNumber { get; set; }
    public int NumberOfItems { get; set; }
    public decimal NominalWeight { get; set; }
    [StringLength(1000)] public string ItemType { get; set; }
    public List<UniformityOfWeightResponse> Responses { get; set; } = [];
    [StringLength(1000)] public string DisintegrationTest { get; set; }
    public Guid? DisintegrationInstrumentId { get; set; }
    public Instrument DisintegrationInstrument { get; set; }
    public decimal DisintegrationMean { get; set; }
    [StringLength(1000)] public string HardnessTest { get; set; }
    public Guid? HardnessInstrumentId { get; set; }
    public Instrument HardnessInstrument { get; set; }
    public decimal HardnessMean { get; set; }
}

public class CreateUniformityOfWeightResponse
{
    public Guid UniformityOfWeightId { get; set; }
    public Guid? BatchManufacturingRecordId { get; set; }
    public Guid? MaterialBatchId { get; set; }
    public List<decimal> Weights { get; set; } = [];
    public decimal Mean { get; set; }
    public decimal StandardDeviation { get; set; }
    public decimal MinimumStandardDeviation { get; set; }
    public decimal MaximumStandardDeviation { get; set; }
    public decimal MaximumWeight { get; set; }
    public decimal MinimumWeight { get; set; }
}

public class UniformityOfWeightResponse : BaseEntity
{
    public Guid UniformityOfWeightId { get; set; }
    public UniformityOfWeight UniformityOfWeight { get; set; }
    public Guid? MaterialBatchId { get; set; }
    public MaterialBatch MaterialBatch { get; set; }
    public List<decimal> Weights { get; set; } = [];
    public decimal Mean { get; set; }
    public decimal StandardDeviation { get; set; }
    public decimal MinimumStandardDeviation { get; set; }
    public decimal MaximumStandardDeviation { get; set; }
    public decimal MaximumWeight { get; set; }
    public decimal MinimumWeight { get; set; }
}

public class UniformityOfWeightDto : BaseDto
{
    public string Name { get; set; }
    public string BalanceNumber { get; set; }
    public int NumberOfItems { get; set; }
    public decimal NominalWeight { get; set; }
    public string ItemType { get; set; }
    public string DisintegrationTest { get; set; }
    public CollectionItemDto DisintegrationInstrument { get; set; }
    public decimal DisintegrationMean { get; set; }
    public string HardnessTest { get; set; }
    public CollectionItemDto HardnessInstrument { get; set; }
    public decimal HardnessMean { get; set; }
}

public class UniformityOfWeightResponseDto : BaseDto
{
    public CollectionItemDto UniformityOfWeight { get; set; }
    public CollectionItemDto BatchManufacturingRecord { get; set; }
    public CollectionItemDto MaterialBatch { get; set; }
    public List<decimal> Weights { get; set; } = [];
    public decimal Mean { get; set; }
    public decimal StandardDeviation { get; set; }
    public decimal MinimumStandardDeviation { get; set; }
    public decimal MaximumStandardDeviation { get; set; }
    public decimal MaximumWeight { get; set; }
    public decimal MinimumWeight { get; set; }
}

