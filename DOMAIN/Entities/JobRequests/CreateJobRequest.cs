using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.JobRequests;

public class CreateJobRequest
{
    [Required] public Guid DepartmentId { get; set; }
    public string Location { get; set; }

    public Guid EquipmentId { get; set; }
    public DateTime DateOfIssue { get; set; }
    public JobStatus Status { get; set; }
    public string DescriptionOfWork { get; set;} 

    public DateTime PreferredCompletionDate { get; set; }

    [Required] public Guid IssuedById {get; set; }
}