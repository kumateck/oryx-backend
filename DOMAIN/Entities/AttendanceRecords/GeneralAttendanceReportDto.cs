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
}

public class AbsencesDto
{
    public List<MinimalEmployeeInfoDto> AbsentEmployees { get; set; }
}

public class SuspensionsDto
{
    public List<MinimalEmployeeInfoDto> SuspendedEmployees { get; set; }
}