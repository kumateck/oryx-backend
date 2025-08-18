using DOMAIN.Entities.AttendanceRecords;
using SHARED;

namespace APP.IRepository;

public interface IAttendanceRepository
{
    Task<Result> UploadAttendance(CreateAttendanceRequest request);
    
    Task<Result<List<AttendanceRecordDepartmentDto>>> DepartmentDailySummaryAttendance(string departmentName, DateTime date);
    
    Task<Result<List<GeneralAttendanceReportDto>>> GeneralAttendanceReport();
    
    Task<Result<FileExportResult>> ExportAttendanceSummary(FileFormat format);
    
}