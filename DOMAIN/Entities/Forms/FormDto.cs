using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.Base;
using SHARED;

namespace DOMAIN.Entities.Forms;

public class FormDto : BaseDto
{
    public string Name { get; set; }
    public List<FormSectionDto> Sections { get; set; } = [];
    public List<FormResponseDto> Responses { get; set; } = [];
    public List<FormAssigneeDto> Assignees { get; set; } = [];
    public List<FormReviewerDto> Reviewers { get; set; } = [];
}

public class FormSectionDto : BaseDto
{
    public CollectionItemDto Form { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<FormFieldDto> Fields { get; set; } = [];
}

public class FormFieldDto : BaseDto
{
    public CollectionItemDto FormSection { get; set; }
    public QuestionDto Question { get; set; }
    public bool Required { get; set; }
    public string Description { get; set; }
    public int Rank { get; set; } 
    public CollectionItemDto Assignee { get; set; }
    public CollectionItemDto Reviewer { get; set; }
}

public class ResponseDto : BaseDto
{
    public CollectionItemDto Form { get; set; }
    public List<FormResponseDto> FormResponses { get; set; } = [];
}

public class FormResponseDto :  WithAttachment
{
    public FormFieldDto FormField { get; set; }
    public string Value { get; set; }
}

public class FormAssigneeDto : BaseDto
{
    public CollectionItemDto Form { get; set; }
    public CollectionItemDto User { get; set; }
}

public class FormReviewerDto : BaseDto
{
    public CollectionItemDto Form { get; set; }
    public CollectionItemDto User { get; set; }
}