using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.QualityRoutines;

namespace DOMAIN.Entities.Forms.Request;

public class CreateFormRequest
{
    [StringLength(100000000)]
    public string Name { get; set; }
    public List<CreateFormSectionRequest> Sections { get; set; } = [];
    public List<CreateFormAssigneeRequest> Assignees { get; set; } = [];
    public List<CreateFormReviewerRequest> Reviewers { get; set; } = [];
    public FormType Type { get; set; }
}

public class CreateFormSectionRequest
{
    [StringLength(100000000)]
    public string Name { get; set; }

    [StringLength(100000000)]
    public string Description { get; set; }
    public int Order { get; set; }
    public Guid? InstrumentId { get; set; }
    public List<CreateFormFieldRequest> Fields { get; set; } = [];
    public string GroupName { get; set; }
    public AnalysisType? AnalysisType { get; set; }
}

public class CreateFormFieldRequest
{
    public Guid QuestionId { get; set; }
    public bool Required { get; set; }
    public int Rank { get; set; }
    public string Description { get; set; }
}

public class CreateResponseRequest
{
    public Guid FormId { get; set; }
    public Guid? BatchManufacturingRecordId { get; set; }
    public Guid? MaterialBatchId { get; set; }
    public Guid? MaterialSamplingId { get; set; }
    public Guid? MaterialSpecificationId { get; set; }
    public Guid? ProductSpecificationId { get; set; }
    public Guid? ProductionActivityStepId { get; set; }
    public Guid? RoutineTrackId { get; set; }
    public List<CreateFormResponseRequest> FormResponses { get; set; } = [];
}

public class CreateFormResponseRequest
{
    public Guid FormFieldId { get; set; }
    public string Value { get; set; }
}

public class CreateFormAssigneeRequest
{
    public Guid FormId { get; set; }
    public Guid? BatchManufacturingRecordId { get; set; }
    public TestStage? Stage { get; set; }
    public Guid? MaterialBatchId { get; set; }
    public Guid? MaterialSamplingId { get; set; }
    public Guid? MaterialSpecificationId { get; set; }
    public Guid? ProductSpecificationId { get; set; }
    public Guid? ProductionActivityStepId { get; set; }
    public Guid? RoutineTrackId { get; set; }
    public List<CreateFormFieldAssigneeRequest> FormFieldAssignees { get; set; } = [];

    [StringLength(100, ErrorMessage = "Issue number cannot be longer than 100 characters.")]
    public string IssueNumber { get; set; }
}

public class CreateFormFieldAssigneeRequest
{
    public Guid FormFieldId { get; set; }
    public Guid? AssigneeId { get; set; }
}

public class CreateFormReviewerRequest
{
    public Guid UserId { get; set; }
}

public class SubmitFormSectionValue
{
    public Guid FormSectionId { get; set; }
    public string Value { get; set; }
}

public class GetResponseIdRequest
{
    public Guid? FormId { get; set; }
    public Guid? MaterialBatchId { get; set; }
    public Guid? MaterialSamplingId { get; set; }
    public Guid? BatchManufacturingRecordId { get; set; }
    public Guid? ProductionActivityStepId { get; set; }
    public Guid? RoutineTrackId { get; set; }
}

public class SaveResponseDraftRequest
{
    public Guid? ResponseId { get; set; }
    public Guid FormId { get; set; }
    public Guid FormFieldId { get; set; }
    public string Value { get; set; }
    public Guid? MaterialBatchId { get; set; }
    public Guid? MaterialSamplingId { get; set; }
    public Guid? BatchManufacturingRecordId { get; set; }
    public Guid? MaterialSpecificationId { get; set; }
    public Guid? ProductSpecificationId { get; set; }
    public Guid? ProductionActivityStepId { get; set; }
    public Guid? RoutineTrackId { get; set; }
}

public class SaveFormAssigneeDraftRequest
{
    public Guid? RoutineTrackId { get; set; }
    public Guid? FormAssigneeId { get; set; }
    public Guid FormId { get; set; }
    public Guid FormFieldId { get; set; }
    public Guid? MaterialBatchId { get; set; }
    public Guid? MaterialSamplingId { get; set; }
    public Guid? BatchManufacturingRecordId { get; set; }
    public Guid? ProductionActivityStepId { get; set; }
    public TestStage? Stage { get; set; }
    public Guid? AssigneeId { get; set; }
}

public class ReassignFormAssigneeRequest
{
    public Guid FormAssigneeId { get; set; }
    public Guid OldAssigneeId { get; set; }
    public Guid? NewAssigneeId { get; set; }
}
