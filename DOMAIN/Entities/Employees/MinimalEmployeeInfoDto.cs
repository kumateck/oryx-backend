using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Designations;

namespace DOMAIN.Entities.Employees;

public class MinimalEmployeeInfoDto
{
    public string FirstName { get; set; }
    
    public string LastName { get; set; }
    
    public string StaffNumber { get; set; }
    
    public string Type { get; set; } 
    
    public string Department { get; set; }
    
    public string Designation { get; set; }
    
}