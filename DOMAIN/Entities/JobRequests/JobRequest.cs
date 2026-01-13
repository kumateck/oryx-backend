using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.Products.Equipments;
using DOMAIN.Entities.Services;
using DOMAIN.Entities.Sites;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.JobRequests;

public class JobRequest : BaseEntity, IRequireApproval
{
    [StringLength(255)] public string Code { get; set; }
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; }
    
    public Guid? SiteId { get; set; }
    public Site Site { get; set; }
    public Guid? EquipmentId { get; set; }
    public Equipment Equipment { get; set; }
    [StringLength(1000)]
    public string EquipmentInstrumentNumber { get; set; }
    public DateTime DateOfIssue { get; set; }
    public JobRequestStatus Status { get; set; } = JobRequestStatus.Pending;
    [StringLength(2000)]
    public string DescriptionOfWork { get; set; }
    public DateTime PreferredCompletionDate { get; set; }
    [StringLength(500)]
    public string Item { get; set; }
    [StringLength(500)]
    public string ItemNumber { get; set; }
    public Guid IssuedById { get; set; }
    public User IssuedBy { get; set; }

    // Track if handled internally or externally
    public JobHandlingType HandlingType { get; set; } = JobHandlingType.NotAssigned;

    // For internal assignment
    public Guid? AssignedToEmployeeId { get; set; }
    public Employee AssignedToEmployee { get; set; }
    public DateTime? AssignedAt { get; set; }
    public Guid? AssignedById { get; set; }
    public User AssignedBy { get; set; }

    // For external assignment
    public Guid? ServiceId { get; set; }
    public Service Service { get; set; }

    // Related entities
    public List<JobExecution> Executions { get; set; } = [];
    public List<JobOrder> JobOrders { get; set; } = [];
    public bool Approved { get; set; }
    public List<JobRequestApproval> Approvals { get; set; } = [];
}

public class JobRequestApproval : ResponsibleApprovalStage
{
    public Guid Id { get; set; }
    public Guid JobRequestId { get; set; }
    public JobRequest JobRequest { get; set; }
    public Guid ApprovalId { get; set; }
    public Approval Approval { get; set; }
}

public enum JobRequestStatus
{
    Pending,
    Acknowledged,
    Assigned,
    JobStarted,
    Completed,
    SentToExternal,
    QuotationReceived,
    ContractorSelected,
    Approved,
    Cancelled
}

public enum JobHandlingType
{
    NotAssigned,
    Internal,
    External
}