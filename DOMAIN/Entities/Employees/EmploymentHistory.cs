using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Employees;

public class EmploymentHistory
{
    public Guid Id { get; set; }
    
    [StringLength(150)] public string CompanyName { get; set; }
    
    public DateTime StartDate { get; set; }
    
    public DateTime EndDate { get; set; }
    
    [StringLength(100)] public string Position { get; set; }
    
    public Guid EmployeeId { get; set; }
}