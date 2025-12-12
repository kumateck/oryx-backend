using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.JobRequests;

/// <summary>
/// Records specific activities performed during job execution
/// </summary>
public class JobActivity : BaseEntity
{
    public Guid? JobExecutionId { get; set; }
    public JobExecution JobExecution { get; set; }
    
    public Guid? JobOrderExecutionId { get; set; }
    public JobOrderExecution JobOrderExecution { get; set; }
    
    [StringLength(2000)]
    public string ActivityDescription { get; set; }
    
    public DateTime PerformedAt { get; set; }
    
    public Guid PerformedById { get; set; }
    public User PerformedBy { get; set; }
    
    [StringLength(1000)]
    public string Notes { get; set; }
}

