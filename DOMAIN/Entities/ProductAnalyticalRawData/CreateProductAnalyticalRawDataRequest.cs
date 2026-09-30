using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.QualityRoutines;

namespace DOMAIN.Entities.ProductAnalyticalRawData;

public class CreateProductAnalyticalRawDataRequest
{
    public DOMAIN.Entities.QualityRoutines.AnalysisType AnalysisType { get; set; }
    public string SpecNumber { get; set; }

    [Required] public TestStage Stage { get; set; }

    public string Description { get; set; }
    [Required] public Guid StpId { get; set; }
    [Required] public Guid FormId { get; set; }
    public List<CommercialCoaItemRequest> CoaItems { get; set; } = [];
}