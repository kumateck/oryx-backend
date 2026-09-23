using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.MaterialARD;
using DOMAIN.Entities.ProductAnalyticalRawData;

namespace DOMAIN.Entities.QualityRoutines;

public class CommercialCoaItem : BaseEntity
{
    public Guid? MaterialArdId { get; set; }
    public MaterialAnalyticalRawData MaterialArd { get; set; }
    public Guid? ProductArdId { get; set; }
    public DOMAIN.Entities.ProductAnalyticalRawData.ProductAnalyticalRawData ProductArd { get; set; }
    public Guid FormFieldId { get; set; }
    public FormField FormField { get; set; }
    [StringLength(255)] public string DisplayLabel { get; set; }
    [StringLength(255)] public string GroupName { get; set; }
    [StringLength(2000)] public string SpecificationText { get; set; }
    [StringLength(100)] public string Unit { get; set; }
    [StringLength(255)] public string Reference { get; set; }
    public int DisplayOrder { get; set; }
}

public class CommercialCoaItemRequest
{
    public Guid FormFieldId { get; set; }
    [StringLength(255)] public string DisplayLabel { get; set; }
    [StringLength(255)] public string GroupName { get; set; }
    [StringLength(2000)] public string SpecificationText { get; set; }
    [StringLength(100)] public string Unit { get; set; }
    [StringLength(255)] public string Reference { get; set; }
    public int DisplayOrder { get; set; }
}
