using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.ProductionOrders;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.QualityAudits;

public class QualityAudit : BaseEntity, IVerifiable
{
    public string AuditNumber { get; set; }
    public AuditType Type { get; set; }
    public AuditFocus FocusArea { get; set; }
    public string Title { get; set; }
    public string Scope { get; set; }
    public string ObjectiveNotes { get; set; }
    public AuditStatus Status { get; set; }

    public DateTime ScheduledStartDate { get; set; }
    public DateTime ScheduledEndDate { get; set; }
    public DateTime? ActualStartDate { get; set; }
    public DateTime? ActualEndDate { get; set; }

    public Guid LeadAuditorId { get; set; }
    public User LeadAuditor { get; set; }
    public List<QualityAuditTeamMember> TeamMembers { get; set; } = [];

    // Optional traceability - "what they manufacture": at most one is typically set per audit.
    public Guid? ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; }
    public Guid? MaterialId { get; set; }
    public Material Material { get; set; }
    public Guid? ProductId { get; set; }
    public Product Product { get; set; }
    public Guid? SupplierId { get; set; }
    public Supplier Supplier { get; set; }

    public Guid? ChecklistTemplateId { get; set; }
    public AuditChecklistTemplate ChecklistTemplate { get; set; }

    public List<AuditChecklistResponse> ChecklistResponses { get; set; } = [];
    public List<AuditFinding> Findings { get; set; } = [];

    public string ClosingMeetingNotes { get; set; }
    public DateTime? ClosedAt { get; set; }
    public Guid? ClosedById { get; set; }
    public User ClosedBy { get; set; }

    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public Guid? VerifiedById { get; set; }
    public User VerifiedBy { get; set; }
}

public class QualityAuditTeamMember : BaseEntity
{
    public Guid QualityAuditId { get; set; }
    public QualityAudit QualityAudit { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; }
}

public enum AuditType
{
    InternalSelfInspection = 0,
    SecondPartySupplier = 1,
    ThirdPartyRegulatory = 2
}

public enum AuditFocus
{
    System = 0,
    Process = 1,
    Product = 2
}

public enum AuditStatus
{
    Planned = 0,
    InProgress = 1,
    PendingReport = 2,
    PendingClosure = 3,
    Closed = 4,
    Cancelled = 5
}
