using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Forms.Request;

public class CreateQuestionRequest
{
    [Required][StringLength(1000)] public string Label { get; set; }
    [Required] public QuestionType Type { get; set; }
    public bool IsMultiSelect { get; set; }
    public QuestionValidationType Validation { get; set; }
    public List<CreateQuestionOptionsRequest> Options { get; set; } = [];
    public string Reference { get; set; }
}

public class CreateQuestionOptionsRequest
{
    public string Name { get; set; }
}