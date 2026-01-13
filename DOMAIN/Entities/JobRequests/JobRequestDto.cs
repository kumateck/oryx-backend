using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.Products.Equipments;
using DOMAIN.Entities.Services;
using DOMAIN.Entities.Sites;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.JobRequests;

public class JobRequestDto : WithAttachment
{
    public string Code { get; set; }
    public DepartmentListDto Department { get; set; }
    public SiteDto Site { get; set; }
    public EquipmentDto Equipment { get; set; }
    public string EquipmentInstrumentNumber { get; set; }
    public DateTime DateOfIssue { get; set; }
    public JobRequestStatus Status { get; set; }
    public string DescriptionOfWork { get; set; }
    public DateTime PreferredCompletionDate { get; set; }
    public string Item { get; set; }
    public string ItemNumber { get; set; }
    public UserDto IssuedBy { get; set; }
    public JobHandlingType HandlingType { get; set; }

    // For internal assignment
    public EmployeeDto AssignedToEmployee { get; set; }
    public DateTime? AssignedAt { get; set; }
    public UserDto AssignedBy { get; set; }

    // For external assignment
    public ServiceDto Service { get; set; }

    // Related entities
    //public List<JobExecutionDto> Executions { get; set; } = [];
    //public List<JobOrderDto> JobOrders { get; set; } = [];
    public bool Approved { get; set; }
}

