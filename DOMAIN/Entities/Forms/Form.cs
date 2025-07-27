using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Instruments;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.MaterialSpecifications;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.ProductSpecifications;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.Forms;

public class Form : BaseEntity
{
    [StringLength(255)] public string Name { get; set; }
    public FormType Type { get; set; }
    public List<FormSection> Sections { get; set; } = [];
    public List<FormResponse> Responses { get; set; } = [];
    public List<FormAssignee> Assignees { get; set; } = [];
    public List<FormReviewer> Reviewers { get; set; } = [];
}

public enum FormType
{
    Default,
    Specification
}

public class FormSection : BaseEntity
{
    public Guid FormId { get; set; }
    public Form Form { get; set; }
    [StringLength(255)] public string Name { get; set; }
    [StringLength(1000)] public string Description { get; set; }
    public Guid? InstrumentId { get; set; }
    public Instrument Instrument { get; set; }
    public int Order { get; set; }
    public List<FormField> Fields { get; set; }
}

public class FormField : BaseEntity
{
    public Guid FormSectionId { get; set; }
    public FormSection FormSection { get; set; }
    public Guid QuestionId { get; set; }
    public Question Question { get; set; }
    public bool Required { get; set; }
    [StringLength(1000)] public string Description { get; set; }
    public int Rank { get; set; } 
    public Guid? AssigneeId { get; set; }
    public User Assignee { get; set; }
    public Guid? ReviewerId { get; set; }
    public User Reviewer { get; set; }
}

public class Response : BaseEntity, IRequireApproval
{
    public Guid FormId { get; set; }
    public Form Form { get; set; }
    public Guid? BatchManufacturingRecordId { get; set; }
    public BatchManufacturingRecord BatchManufacturingRecord { get; set; }
    public Guid? MaterialBatchId { get; set; }
    public MaterialBatch MaterialBatch { get; set; }
    public Guid? MaterialSpecificationId { get; set; }
    public MaterialSpecification MaterialSpecification { get; set; }
    public Guid? ProductSpecificationId { get; set; }
    public ProductSpecification ProductSpecification { get; set; }
    public List<FormResponse> FormResponses { get; set; } = [];
    public List<ResponseApproval> Approvals { get; set; } = [];
    public Guid? CheckedById { get; set; }
    public User CheckedBy { get; set; }
    public DateTime? CheckedAt { get; set; }
    public bool Approved { get; set; }
}

public class FormResponse : BaseEntity
{
    public Guid ResponseId { get; set; }
    public Response Response { get; set; }
    public Guid FormFieldId { get; set; }
    public FormField FormField { get; set; }
    [StringLength(100000)] public string Value { get; set; }
}

public class ResponseApproval: ResponsibleApprovalStage
{
    public Guid Id { get; set; }
    
    public Guid ResponseId { get; set; }
    
    public Response Response { get; set; }
    public Guid ApprovalId { get; set; }
    
    public Approval Approval { get; set; }
}

public class FormAssignee 
{
    public Guid Id { get; set; }
    public Guid FormId { get; set; }
    public Form Form { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; }
}

public class FormReviewer
{
    public Guid Id { get; set; }
    public Guid FormId { get; set; }
    public Form Form { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; }
}