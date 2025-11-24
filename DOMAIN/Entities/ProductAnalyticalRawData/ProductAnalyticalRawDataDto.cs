using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.ProductStandardTestProcedures;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.ProductAnalyticalRawData;

public class ProductAnalyticalRawDataDto : WithAttachment
{
    public string SpecNumber { get; set; }
    public TestStage Stage { get; set; }
    public string Description { get; set; }
    public FormDto Form { get; set; }
    public ProductStandardTestProcedureDto ProductStandardTestProcedure { get; set; }
}

public class ProductBatchArd
{
    public BatchManufacturingRecordDto BatchManufacturingRecord { get; set; }
    public string ArNumber { get; set; }
    public string SpecNumber { get; set; }
    public DateTime? SampledDate { get; set; }
    public DateTime? IssueDate { get; set; }
    public UserDto IssuedBy { get; set; }
    public DateTime? AnalysedDate { get; set; }
    public UserDto AnalysedBy { get; set; }
}