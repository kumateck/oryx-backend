using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Forms.Request;

public class CreateQuestionRequest
{
    [StringLength(1000)] public string Label { get; set; }
    public QuestionType Type { get; set; }
    public QuestionValidationType Validation { get; set; }
    public List<CreateQuestionOptionsRequest> Options { get; set; } = [];
}

public class CreateQuestionOptionsRequest
{
    public string Name { get; set; }
}