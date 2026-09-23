using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.QualityRoutines;

namespace DOMAIN.Entities.MaterialARD;

public class CreateMaterialAnalyticalRawDataRequest
{
    public DOMAIN.Entities.QualityRoutines.AnalysisType AnalysisType { get; set; }
    public string SpecNumber { get; set; }
    public string Description { get; set; }
    [Required] public Guid StpId { get; set; }
    [Required] public Guid FormId { get; set; }
    
    [Required] public Guid MaterialId { get; set; }
    public Guid? MaterialBatchId { get; set; }
    public Guid? UniformityOfWeightId { get; set; }
    public List<CommercialCoaItemRequest> CoaItems { get; set; } = [];
}