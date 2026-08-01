using DOMAIN.Entities.Employees;
using DOMAIN.Entities.LeaveRequests;

namespace DOMAIN.Entities.Reports.HumanResource;

public class EmployeeMasterListReportDto
{
    public int No { get; set; }
    public string StaffNumber { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string Gender { get; set; }
    public DateTime DateOfBirth { get; set; }
    public DateTime DateEmployed { get; set; }
    public string EmploymentType { get; set; }
    public string GradeLevel { get; set; }
    public string Status { get; set; }
    public string Department { get; set; }
    public string Designation { get; set; }
    public string ReportingManager { get; set; }
    public string Nationality { get; set; }
    public string Region { get; set; }
    public string MaritalStatus { get; set; }
    public string Religion { get; set; }
    public string BankAccountNumber { get; set; }
    public string SsnitNumber { get; set; }
    public string GhanaCardNumber { get; set; }
    public int AnnualLeaveEntitlement { get; set; }
}

public class EmployeeDirectoryByDepartmentDto
{
    public int No { get; set; }
    public string Department { get; set; }
    public string StaffNumber { get; set; }
    public string EmployeeName { get; set; }
    public string Designation { get; set; }
    public string GradeLevel { get; set; }
    public string EmploymentType { get; set; }
    public string Status { get; set; }
    public string ReportingManager { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
}

public class EmployeeMasterListFilter
{
    public Guid? DepartmentId { get; set; }
    public EmployeeType? EmployeeType { get; set; }
    public EmployeeLevel? GradeLevel { get; set; }
    public EmployeeStatus? Status { get; set; }
}

public class EmployeeDirectoryFilter
{
    public Guid? DepartmentId { get; set; }
    public EmployeeLevel? GradeLevel { get; set; }
    public EmployeeType? EmployeeType { get; set; }
}

public class EmployeeDemographicsReportDto
{
    public int No { get; set; }
    public string Dimension { get; set; }
    public string Category { get; set; }
    public int PermanentCount { get; set; }
    public int CasualCount { get; set; }
    public int TotalCount { get; set; }
    public double Percentage { get; set; }
}

public class EmployeeDemographicsFilter
{
    public Guid? DepartmentId { get; set; }
}

public class StaffGradeLevelReportDto
{
    public int No { get; set; }
    public string Department { get; set; }
    public int SeniorMgtMale { get; set; }
    public int SeniorMgtFemale { get; set; }
    public int SeniorStaffMale { get; set; }
    public int SeniorStaffFemale { get; set; }
    public int JuniorStaffMale { get; set; }
    public int JuniorStaffFemale { get; set; }
    public int TotalMale { get; set; }
    public int TotalFemale { get; set; }
    public int DepartmentalTotal { get; set; }
}

public class StaffGradeLevelFilter
{
    public Guid? DepartmentId { get; set; }
}

public class LeaveRegisterReportDto
{
    public int No { get; set; }
    public string EmployeeName { get; set; }
    public string StaffNumber { get; set; }
    public string Department { get; set; }
    public string LeaveCategory { get; set; }
    public string LeaveType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int DurationDays { get; set; }
    public int? PaidDays { get; set; }
    public int? UnpaidDays { get; set; }
    public string Status { get; set; }
    public string Justification { get; set; }
    public string ContactPerson { get; set; }
    public string Destination { get; set; }
    public string ApprovedBy { get; set; }
    public DateTime DateApplied { get; set; }
}

public class LeaveRegisterFilter
{
    public Guid? DepartmentId { get; set; }
    public RequestCategory? LeaveCategory { get; set; }
    public LeaveStatus? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class LeaveBalanceReportDto
{
    public int No { get; set; }
    public string EmployeeName { get; set; }
    public string StaffNumber { get; set; }
    public string Department { get; set; }
    public int LeaveYear { get; set; }
    public int DaysAllowed { get; set; }
    public int DaysUsed { get; set; }
    public int DaysRemaining { get; set; }
    public double Utilisation { get; set; }
}

public class LeaveBalanceFilter
{
    public Guid? DepartmentId { get; set; }
    public int? LeaveYear { get; set; }
    public Guid? EmployeeId { get; set; }
}

public class LeaveApprovalAuditReportDto
{
    public int No { get; set; }
    public string Employee { get; set; }
    public string LeaveCategory { get; set; }
    public int DurationDays { get; set; }
    public string ApprovalStage { get; set; }
    public string Approver { get; set; }
    public string ActionTaken { get; set; }
    public DateTime? ActionDate { get; set; }
    public string Comments { get; set; }
    public string FinalStatus { get; set; }
}

public class LeaveApprovalAuditFilter
{
    public Guid? DepartmentId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public LeaveStatus? Status { get; set; }
}
