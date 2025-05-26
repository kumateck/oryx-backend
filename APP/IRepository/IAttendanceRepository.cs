using DOMAIN.Entities.AttendanceRecords;
using SHARED;

namespace APP.IRepository;

public interface IAttendanceRepository
{
    Task<Result<Guid>> UploadAttendance(CreateAttendanceRequest request);
    
    Task<Result<List<AttendanceRecordDepartmentDto>>> DepartmentDailySummaryAttendance(string departmentName, DateTime date);
    
    Task<Result<List<GeneralAttendanceReportDto>>> GeneralAttendanceReport(DateTime date);
    
}