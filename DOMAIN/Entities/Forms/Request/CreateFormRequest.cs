using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Forms.Request;

public class CreateFormRequest
{
    [StringLength(255)] public string Name { get; set; }
    public List<CreateFormSectionRequest> Sections { get; set; } = [];
    public List<CreateFormAssigneeRequest> Assignees { get; set; } = [];
    public List<CreateFormReviewerRequest> Reviewers { get; set; } = [];
}

public class CreateFormSectionRequest
{
    [StringLength(255)] public string Name { get; set; }
    [StringLength(1000)] public string Description { get; set; }
    public List<CreateFormFieldRequest> Fields { get; set; } = [];
}

public class CreateFormFieldRequest
{
    public Guid QuestionId { get; set; }
    public bool Required { get; set; }
    public int Rank { get; set; } 
    public string Description { get; set; }
    public Guid? AssigneeId { get; set; }
    public Guid? ReviewerId { get; set; }
}

public class CreateResponseRequest 
{
    public Guid FormId { get; set; }
    public Guid? MaterialAnalyticalRawDataId { get; set; }
    public List<CreateFormResponseRequest> FormResponses { get; set; } = [];
}

public class CreateFormResponseRequest
{
    public Guid FormFieldId { get; set; }
    public string Value { get; set; }
}

public class CreateFormAssigneeRequest 
{
    public Guid UserId { get; set; }
}

public class CreateFormReviewerRequest
{
    public Guid UserId { get; set; }
}