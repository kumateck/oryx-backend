using System.ComponentModel.DataAnnotations.Schema;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.MaterialStandardTestProcedures;
using DOMAIN.Entities.UniformityOfWeights;

namespace DOMAIN.Entities.MaterialARD;

public class MaterialAnalyticalRawData : BaseEntity, IVerifiable
{
    public string SpecNumber { get; set; }

    public string Description { get; set; }

    public Guid StpId { get; set; }

    [ForeignKey("StpId")]
    public MaterialStandardTestProcedure MaterialStandardTestProcedure { get; set; }
    public Guid FormId { get; set; }
    public Form Form { get; set; }
    public Guid? UniformityOfWeightId { get; set; }
    public UniformityOfWeight UniformityOfWeight { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public Guid? VerifiedById { get; set; }
}