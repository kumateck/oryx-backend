using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Products.Equipments;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.JobRequests;

public class JobRequestDto : WithAttachment
{
    public DepartmentDto Department { get; set; }
    public string Location { get; set; }
    public EquipmentDto Equipment { get; set; }
    public DateTime DateOfIssue { get; set; }
    public JobStatus Status { get; set; }
    public string DescriptionOfWork { get; set;} 
    public DateTime PreferredCompletionDate { get; set; }
    public UserDto IssuedBy {get; set; }
}

