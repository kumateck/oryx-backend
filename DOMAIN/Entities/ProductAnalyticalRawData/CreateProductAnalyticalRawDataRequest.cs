using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.AnalyticalTestRequests;

namespace DOMAIN.Entities.ProductAnalyticalRawData;

public class CreateProductAnalyticalRawDataRequest
{
    public string SpecNumber { get; set; }

    [Required] public TestStage Stage { get; set; }

    public string Description { get; set; }
    [Required] public Guid StpId { get; set; }
    [Required] public Guid FormId { get; set; }
}