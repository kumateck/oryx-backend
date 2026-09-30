using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.QualityRoutines;

namespace DOMAIN.Entities.Forms.Request;

public class UpdateFormMetadataRequest
{
    [Required]
    [StringLength(100000000)]
    public string Name { get; set; }
    public FormType Type { get; set; }
}

public class UpdateFormSectionRequest
{
    [Required]
    [StringLength(100000000)]
    public string Name { get; set; }

    [StringLength(100000000)]
    public string Description { get; set; }
    public int Order { get; set; }
    public Guid? InstrumentId { get; set; }

    [StringLength(1000000)]
    public string GroupName { get; set; }
    public AnalysisType? AnalysisType { get; set; }
}

public class UpdateFormFieldRequest
{
    public Guid QuestionId { get; set; }
    public bool Required { get; set; }
    public int Rank { get; set; }

    [StringLength(1000000)]
    public string Description { get; set; }
}
