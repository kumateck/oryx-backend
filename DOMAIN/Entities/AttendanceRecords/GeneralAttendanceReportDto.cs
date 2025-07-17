using DOMAIN.Entities.Employees;

namespace DOMAIN.Entities.AttendanceRecords;

public class GeneralAttendanceReportDto
{
    public string DepartmentName { get; set; }
    
    public int PermanentStaff { get; set; }
    
    public int CasualStaff { get; set; }
    
    public int TotalStaff => PermanentStaff + CasualStaff;
    
    public int PermanentMorning { get; set; }
    
    public int PermanentAfternoon { get; set; }
    
    public int PermanentNight { get; set; }
    
    public int CasualMorning { get; set; }
    
    public int CasualAfternoon { get; set; }
    
    public int CasualNight { get; set; }
    
    public AbsencesDto Absences { get; set; }
    
    public SuspensionsDto Suspensions { get; set; }
    
    public SickLeaveDto SickLeaves {get; set;}
    
    public MaternityLeaveDto MaternityLeaves {get; set;}
}

public class AbsencesDto
{
    public List<MinimalEmployeeInfoDto> AbsentEmployees { get; set; }
}

public class SuspensionsDto
{
    public List<MinimalEmployeeInfoDto> SuspendedEmployees { get; set; }
}

public class GroupedLeaveDto
{
    public string Type { get; set; } 
    public List<MinimalEmployeeInfoDto> Employees { get; set; }
}

public class SickLeaveDto
{
    public List<GroupedLeaveDto> LeaveEmployees { get; set; }
}

public class MaternityLeaveDto
{
    public List<GroupedLeaveDto> MaternityLeaveEmployees { get; set; }
}