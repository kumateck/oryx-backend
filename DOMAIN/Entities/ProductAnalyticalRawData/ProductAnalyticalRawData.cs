using System.ComponentModel.DataAnnotations.Schema;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.ProductStandardTestProcedures;
using DOMAIN.Entities.QualityRoutines;

namespace DOMAIN.Entities.ProductAnalyticalRawData;

public class ProductAnalyticalRawData : BaseEntity, IVerifiable
{
    public DOMAIN.Entities.QualityRoutines.AnalysisType AnalysisType { get; set; }
    public string SpecNumber { get; set; }
    public string Description { get; set; }
    public TestStage Stage { get; set; }
    public Guid StpId { get; set; }
    [ForeignKey("StpId")]
    public ProductStandardTestProcedure ProductStandardTestProcedure { get; set; }
    public Guid FormId { get; set; }
    public Form Form { get; set; }
    public List<CommercialCoaItem> CoaItems { get; set; } = [];
    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public Guid? VerifiedById { get; set; }
}