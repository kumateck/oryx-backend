using System.ComponentModel.DataAnnotations.Schema;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.MaterialStandardTestProcedures;

namespace DOMAIN.Entities.MaterialARD;

public class MaterialAnalyticalRawData : BaseEntity
{
    public string SpecNumber { get; set; }
    
    public string Description { get; set; }
    
    public Guid StpId { get; set; }
    
    [ForeignKey("StpId")]
    public MaterialStandardTestProcedure MaterialStandardTestProcedure { get; set; }
    public Guid FormId { get; set; }
    public Form Form { get; set; }
}