using System.ComponentModel.DataAnnotations.Schema;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.ProductStandardTestProcedures;

namespace DOMAIN.Entities.ProductAnalyticalRawData;

public class ProductAnalyticalRawData : BaseEntity
{
    public string SpecNumber { get; set; }
    public string Description { get; set; }
    public Stage Stage { get; set; }
    public Guid StpId { get; set; }
    [ForeignKey("StpId")]
    public ProductStandardTestProcedure ProductStandardTestProcedure { get; set; }
    public Guid FormId { get; set; }
    public Form Form { get; set; }
}

public enum Stage
{
    Intermediate,
    Bulk,
    Finished
}