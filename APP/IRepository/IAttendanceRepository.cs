using DOMAIN.Entities.AttendanceRecords;
using SHARED;

namespace APP.IRepository;

public interface IAttendanceRepository
{
    Task<Result> UploadAttendance(CreateAttendanceRequest request, DateTime? date);

    Task<Result<List<AttendanceRecordDepartmentDto>>> DepartmentDailySummaryAttendance(string departmentName, DateTime date);

    Task<Result<GeneralAttendanceReportResponse>> GeneralAttendanceReport(Guid? departmentId = null);

    Task<Result<FileExportResult>> ExportAttendanceSummary(FileFormat format);

}