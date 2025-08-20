using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Products.Equipments;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.JobRequests;

public class JobRequest : BaseEntity
{
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; }
    public string Location { get; set; }

    public Guid EquipmentId { get; set; }
    public Equipment Equipment { get; set; }
    public DateTime DateOfIssue { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Pending;
    public string DescriptionOfWork { get; set;} 

    public DateTime PreferredCompletionDate { get; set; }

    public Guid IssuedById {get; set; }
    public User IssuedBy {get; set; }
}

public enum JobStatus
{
    Pending,
    Assigned,
    InProgress,
    Completed,
    Cancelled
}