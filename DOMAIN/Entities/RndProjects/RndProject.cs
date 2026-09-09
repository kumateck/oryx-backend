using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Users;
using SHARED;

namespace DOMAIN.Entities.RndProjects;

public class RndProject : BaseEntity, IRequireApproval
{
    [StringLength(100)] public string Code { get; set; }
    public Guid? ProductId { get; set; }
    public Product Product { get; set; }
    [StringLength(1000)] public string Title { get; set; }
    [StringLength(4000)] public string Objective { get; set; }
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; }
    public Guid RequestedById { get; set; }
    public User RequestedBy { get; set; }
    public RndProjectStatus Status { get; set; }
    public DateTime? TargetLaunchDate { get; set; }
    public Guid? QtppFormId { get; set; }
    public Form QtppForm { get; set; }
    public Guid? AttachmentId { get; set; }
    public Attachment Attachment { get; set; }
    public bool Approved { get; set; }
    public List<RndProjectApprovals> Approvals { get; set; } = [];
}

public enum RndProjectStatus
{
    Intake = 0,
    FeasibilityReview = 1,
    InDevelopment = 2,
    TechnologyTransfer = 3,
    Completed = 4,
    OnHold = 5,
    Cancelled = 6,
}

public class RndProjectApprovals : ResponsibleApprovalStage
{
    public Guid Id { get; set; }
    public Guid RndProjectId { get; set; }
    public RndProject RndProject { get; set; }
    public Guid ApprovalId { get; set; }
    public Approval Approval { get; set; }
}

public class RndProjectDto : BaseDto
{
    public string Code { get; set; }
    public CollectionItemDto Product { get; set; }
    public string Title { get; set; }
    public string Objective { get; set; }
    public CollectionItemDto Department { get; set; }
    public UserDto RequestedBy { get; set; }
    public RndProjectStatus Status { get; set; }
    public DateTime? TargetLaunchDate { get; set; }
    public Guid? QtppFormId { get; set; }
    public Guid? AttachmentId { get; set; }
    public bool Approved { get; set; }
}

public class CreateRndProjectRequest
{
    public Guid? ProductId { get; set; }
    [StringLength(1000)] public string Title { get; set; }
    [StringLength(4000)] public string Objective { get; set; }
    public DateTime? TargetLaunchDate { get; set; }
    public Guid? QtppFormId { get; set; }
    public Guid? AttachmentId { get; set; }
}

public class UpdateRndProjectRequest
{
    public Guid? ProductId { get; set; }
    [StringLength(1000)] public string Title { get; set; }
    [StringLength(4000)] public string Objective { get; set; }
    public DateTime? TargetLaunchDate { get; set; }
    public Guid? QtppFormId { get; set; }
    public Guid? AttachmentId { get; set; }
}

public class UpdateRndProjectStatusRequest
{
    public RndProjectStatus Status { get; set; }
    public string Comments { get; set; }
}
