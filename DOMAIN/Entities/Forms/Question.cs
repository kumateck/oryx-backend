using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using SHARED;

namespace DOMAIN.Entities.Forms;

public class Question : BaseEntity
{
    [StringLength(1000)] public string Label { get; set; }
    public QuestionType Type { get; set; }
    public QuestionValidationType Validation { get; set; }
    public List<QuestionOption> Options { get; set; } = [];
}

public class QuestionOption : BaseEntity
{
    public Guid QuestionId { get; set; }
    public Question Question { get; set; }
    [StringLength(1000)]  public string Name { get; set; }
}

public class QuestionDto : BaseDto
{
    public string Label { get; set; }
    public QuestionType Type { get; set; }
    public QuestionValidationType Validation { get; set; }
    public List<QuestionOptionDto> Options { get; set; } = [];
}

public class QuestionOptionDto : BaseEntity
{
    public CollectionItemDto Question { get; set; }
    public string Name { get; set; }
}

public enum QuestionType
{
    ShortAnswer,
    LongAnswer,
    Paragraph,
    Datepicker,
    SingleChoice,
    Dropdown,
    Checkbox, 
    FileUpload,
    Signature
}

public enum QuestionValidationType
{
    Number,
    Letter,
    Alphanumeric,
    None
}