using SHARED;

namespace DOMAIN.Entities.Approvals;

public class ApprovalDto
{
    public Guid Id { get; set; }
    public string ItemType { get; set; }
    public TimeSpan EscalationDuration  { get; set; }
    public List<ApprovalStageDto> ApprovalStages { get; set; } = [];
    public CollectionItemDto CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ApprovalStageDto
{
    public CollectionItemDto User { get; set; }
    public CollectionItemDto Role { get; set; }
    public bool Required { get; set; }     
    public int Order { get; set; }
}