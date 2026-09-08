using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Products.Equipments;
using DOMAIN.Entities.Users;
using SHARED;

namespace DOMAIN.Entities.Forms;

public class FormDto : BaseDto
{
    public string Name { get; set; }
    public FormType Type { get; set; }
    public List<FormSectionDto> Sections { get; set; } = [];
    public List<FormResponseDto> Responses { get; set; } = [];
    // public List<FormReviewerDto> Reviewers { get; set; } = [];
}

public class FormSectionDto : BaseDto
{
    public CollectionItemDto Form { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public QcEquipmentDto Instrument { get; set; }
    public int Order { get; set; }
    public List<FormFieldDto> Fields { get; set; } = [];
    public string Value { get; set; }
    public string GroupName { get; set; }
    public bool Complies { get; set; }
}

public class FormFieldDto : BaseDto
{
    public CollectionItemDto FormSection { get; set; }
    public QuestionDto Question { get; set; }
    public bool Required { get; set; }
    public string Description { get; set; }
    public int Rank { get; set; }
}

public class ResponseDto : BaseDto
{
    public CollectionItemDto Form { get; set; }
    public List<FormResponseDto> FormResponses { get; set; } = [];
}

public class ResponseDetailDto : ResponseDto
{
    public CollectionItemDto BatchManufacturingRecord { get; set; }
    public CollectionItemDto MaterialBatch { get; set; }
    public Guid? ProductionActivityStepId { get; set; }
    public UserDto CheckedBy { get; set; }
    public DateTime? CheckedAt { get; set; }
    public bool Approved { get; set; }
    public bool Rejected { get; set; }
    public bool HasPendingApproval { get; set; }
}

public class FormResponseDto : WithAttachment
{
    public Guid ResponseId { get; set; }
    public Guid? ProductionActivityStepId { get; set; }
    public FormFieldDto FormField { get; set; }
    public string SectionName { get; set; }
    public bool Complies { get; set; }
    public string Value { get; set; }
    public UserDto CheckedBy { get; set; }
    public DateTime? CheckedAt { get; set; }
    public bool FormulaGoverned { get; set; }
    public bool FormulaResultFinalized { get; set; }
    public Guid? FormulaExecutionId { get; set; }
    public string FormulaDisplayResultsJson { get; set; }
}

public class FormReviewerDto : BaseDto
{
    public CollectionItemDto Form { get; set; }
    public CollectionItemDto User { get; set; }
}
