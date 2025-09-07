using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.MaterialStandardTestProcedures;
using DOMAIN.Entities.Procurement.Manufacturers;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.UniformityOfWeights;
using DOMAIN.Entities.Users;
using SHARED;

namespace DOMAIN.Entities.MaterialARD;

public class MaterialAnalyticalRawDataDto : WithAttachment
{
    public string SpecNumber { get; set; }
    public string Description { get; set; }
    public MaterialStandardTestProcedureDto MaterialStandardTestProcedure { get; set; }
    public CollectionItemDto Form { get; set; }
    public UniformityOfWeightDto UniformityOfWeight { get; set; }
}

public class MaterialBatchArd
{
    public MaterialBatchReducedDto MaterialBatch { get; set; }
    public string ArNumber { get; set; }
    public string GrnNumber { get; set; }
    public string SpecNumber { get; set; }
    public string StpNumber { get; set; }
    public CollectionItemDto Supplier { get; set; }
    public CollectionItemDto Manufacturer { get; set; }
    public DateTime? SampledDate { get; set; }
    public UserDto SampledBy { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal QuantitySampled { get; set; }
}