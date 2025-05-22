using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.MaterialStandardTestProcedures;

namespace DOMAIN.Entities.AnalyticalRawData;

public class AnalyticalRawData : BaseEntity
{
    public string StpNumber { get; set; }
    
    public string SpecNumber { get; set; }
    
    public string Description { get; set; }
    
    public Guid StpId { get; set; }
    
    [ForeignKey("StpId")]
    public MaterialStandardTestProcedure MaterialStandardTestProcedure { get; set; }
    
    public Guid FormId { get; set; }
    
    [JsonIgnore]
    public Form Form { get; set; }
}