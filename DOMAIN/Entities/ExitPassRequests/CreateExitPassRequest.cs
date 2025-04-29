using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.ExitPassRequests;

public class CreateExitPassRequest
{
    [Required] public DateTime Date { get; set; }
    
    [Required] public TimeOnly TimeIn { get; set; }
    
    [Required] public TimeOnly TimeOut { get; set; }
    
    public string Justification { get; set; }
    
    [Required] public Guid EmployeeId { get; set; }
}