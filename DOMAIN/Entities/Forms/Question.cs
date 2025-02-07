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
    public bool IsMultiSelect { get; set; }
    [StringLength(100)] public string Reference { get; set; }
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
    public bool IsMultiSelect { get; set; }
    public string Reference { get; set; }
    public List<QuestionOptionDto> Options { get; set; } = [];
}

public class QuestionOptionDto : BaseEntity
{
    public CollectionItemDto Question { get; set; }
    public string Name { get; set; }
}

public enum QuestionType
{
    ShortAnswer = 0,
    LongAnswer = 1,
    Paragraph = 2,
    Datepicker = 3,
    SingleChoice = 4,
    Dropdown = 5,
    Checkbox = 6, 
    FileUpload = 7,
    Signature = 8,
    Reference = 9
}

public enum QuestionValidationType
{
    Number = 0,
    Letter = 1,
    Alphanumeric= 2,
    None = 3
}