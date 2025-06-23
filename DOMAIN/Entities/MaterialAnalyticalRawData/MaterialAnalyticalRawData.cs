using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.MaterialStandardTestProcedures;

namespace DOMAIN.Entities.MaterialAnalyticalRawData;

public class MaterialAnalyticalRawData : BaseEntity
{
    public string StpNumber { get; set; }
    
    public string SpecNumber { get; set; }
    
    public string Description { get; set; }
    
    public Guid StpId { get; set; }
    
    [ForeignKey("StpId")]
    public MaterialStandardTestProcedure MaterialStandardTestProcedure { get; set; }
    public Guid? MaterialBatchId { get; set; }
    public MaterialBatch MaterialBatch { get; set; }
    public Guid FormId { get; set; }
    public Form Form { get; set; }
}