using System.Globalization;
using System.Text;
using APP.IRepository;
using DOMAIN.Entities.AttendanceRecords;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.LeaveRequests;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using SHARED;

namespace APP.Repository;

public class AttendanceRepository(ApplicationDbContext context) : IAttendanceRepository
{
    public async Task<Result> UploadAttendance(CreateAttendanceRequest request)
    {
        if (Path.GetExtension(request.Attendance.FileName) != ".xlsx" && Path.GetExtension(request.Attendance.FileName) != ".xls")
        {
            return Error.Validation("Attendance.InvalidFileType", "Invalid file type. Only .xlsx or .xls files are allowed.");
        }

        using var stream = new MemoryStream();
        await request.Attendance.CopyToAsync(stream);

        ExcelPackage.License.SetNonCommercialPersonal("Oryx");

        using var package = new ExcelPackage(stream);
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();

        if (worksheet?.Dimension == null || worksheet.Dimension.End.Row < 2)
        {
            return Error.Validation("Attendance.Empty", "The uploaded Excel file is empty or does not contain any records.");
        }

        var attendanceRecords = new List<AttendanceRecords>();
        
        var lastRow = worksheet.Dimension.End.Row;
        while (lastRow >= 2 && string.IsNullOrWhiteSpace(worksheet.Cells[lastRow, 1].Text))
        {
            lastRow--;
        }

        for (var row = 2; row <= lastRow; row++)
        {
            var empId = worksheet.Cells[row, 1].Text?.Trim();
            var timestampStr = worksheet.Cells[row, 3].Text?.Trim();
            var workState = worksheet.Cells[row, 4].Text?.Trim().Replace(" ", "");

            if (string.IsNullOrWhiteSpace(empId) || string.IsNullOrWhiteSpace(timestampStr) || string.IsNullOrWhiteSpace(workState))
            {
                return Error.Validation("Attendance.MissingFields", $"Missing required fields at row {row}.");
            }

            if (!DateTime.TryParseExact(timestampStr, "dd/MM/yyyy HH:mm:ss", null, DateTimeStyles.AssumeLocal, out var localTime))
            {
                return Error.Validation("Attendance.InvalidTimestamp", $"Invalid timestamp format at row {row}. Use dd/MM/yyyy HH:mm:ss.");
            }

            if (localTime.Date != DateTime.UtcNow.Date)
            {
                return Error.Validation("Attendance.InvalidDate", $"The timestamp at row {row} is not for today. Only today's records are allowed.");
            }

            var timeStamp = localTime.ToUniversalTime();

            if (!Enum.TryParse<WorkState>(workState, true, out var parsedWorkState))
            {
                return Error.Validation("Attendance.InvalidWorkState", $"Invalid work state '{workState}' at row {row}. Allowed values: Check In, Check Out.");
            }

            var existingAttendance = await context.AttendanceRecords
                .FirstOrDefaultAsync(a => a.EmployeeId == empId && a.TimeStamp == timeStamp);

            if (existingAttendance != null)
            {
                return Error.Validation("Attendance.Duplicate", $"Duplicate record found at row {row}.");
            }

            var employeeExists = await context.Employees.AnyAsync(e => e.StaffNumber == empId);
            if (!employeeExists)
            {
                return Error.Validation("Attendance.InvalidEmployee", $"Employee with staff number '{empId}' not found.");
            }

            attendanceRecords.Add(new AttendanceRecords
            {
                EmployeeId = empId,
                TimeStamp = timeStamp,
                WorkState = parsedWorkState
            });
        }

        if (attendanceRecords.Count == 0)
        {
            return Error.Validation("Attendance.NoValidRecords", "No valid attendance records were found in the uploaded file.");
        }

        await context.AttendanceRecords.AddRangeAsync(attendanceRecords);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<List<AttendanceRecordDepartmentDto>>> DepartmentDailySummaryAttendance(string departmentName, DateTime date)
    {
        // Filter attendance records for the given date
        var dailyRecords = await context.AttendanceRecords
            .Where(a => a.TimeStamp.Date == date.Date)
            .ToListAsync();

        if (dailyRecords.Count == 0)
            return Error.NotFound("Attendance.NotFound", $"No attendance records found for {date:yyyy-MM-dd}");

        // Get employees in the specified department
        var employeeIds = dailyRecords.Select(r => r.EmployeeId).Distinct().ToList();

        var employees = await context.Employees
            .Include(e => e.Department)
            .Include(e => e.ShiftAssignments)
            .ThenInclude(sa => sa.ShiftCategory)
            .Where(e => employeeIds.Contains(e.StaffNumber) && e.Department.Name == departmentName)
            .ToListAsync();

        return (from employee in employees
            let records = dailyRecords
                .Where(r => r.EmployeeId == employee.StaffNumber).ToList()
            where records.Count != 0
            let clockIn = records.Min(r => r.TimeStamp)
            let clockOut = records.Max(r => r.TimeStamp)
            let workHours = (clockOut - clockIn).TotalHours
            let shift = employee.ShiftAssignments.FirstOrDefault(sa => sa.ScheduleDate.Date == date.Date)
            select new AttendanceRecordDepartmentDto
            {
                StaffName = $"{employee.FirstName} {employee.LastName}",
                EmployeeId = employee.StaffNumber,
                ShiftName = shift.ShiftCategory.Name,
                ClockInTime = clockIn.ToString("hh:mm tt"),
                ClockOutTime = clockOut.ToString("hh:mm tt"),
                WorkHours = Math.Round(workHours, 2)
            }).ToList();
    }

    public async Task<Result<GeneralAttendanceReportResponse>> GeneralAttendanceReport()
    {
        var today = DateTime.UtcNow.Date;
        
        var dailyRecords = await context.AttendanceRecords
            .Where(a => a.TimeStamp.Date == today && a.WorkState == WorkState.CheckIn)
            .ToListAsync();
        
        var allEmployees = await context.Employees
            .Include(e => e.Department)
            .ToListAsync();

        var employeeDbIds = allEmployees.Select(e => e.Id).ToList();
        
        var shiftAssignments = await context.ShiftAssignments
            .AsSplitQuery()
            .Include(sa => sa.ShiftType)
            .Include(sa => sa.ShiftSchedules)
            .Where(sa =>
                sa.ShiftSchedules != null &&
                sa.ShiftSchedules.StartDate.Date <= today &&
                sa.ShiftSchedules.EndDate.Date >= today &&
                employeeDbIds.Contains(sa.EmployeeId))
            .ToListAsync();

        var shiftAssignmentMap = shiftAssignments
            .GroupBy(sa => sa.EmployeeId)
            .ToDictionary(g => g.Key, g => g.FirstOrDefault());
        
        var approvedLeaves = await context.LeaveRequests
            .Where(l =>
                l.Approved &&
                l.RequestCategory == RequestCategory.LeaveRequest &&
                l.LeaveType.Name != "Maternity Leave" &&
                l.LeaveType.Name != "Sick Leave" &&
                l.StartDate <= today && l.EndDate >= today)
            .Include(l => l.Employee).ThenInclude(e => e.Department)
            .ToListAsync();

        var sickLeaves = await context.LeaveRequests
            .Where(l => l.Approved && l.LeaveType.Name == "Sick Leave" &&
                l.StartDate <= today && l.EndDate >= today)
            .Include(l => l.Employee).ThenInclude(e => e.Department)
            .ToListAsync();

        var maternityLeaves = await context.LeaveRequests
            .Where(l => l.Approved && l.LeaveType.Name == "Maternity Leave" &&
                l.StartDate <= today && l.EndDate >= today)
            .Include(l => l.Employee).ThenInclude(e => e.Department)
            .ToListAsync();

        var absences = await context.LeaveRequests
            .Where(l => l.Approved && l.RequestCategory == RequestCategory.AbsenceRequest &&
                l.StartDate <= today && l.EndDate >= today)
            .Include(l => l.Employee).ThenInclude(e => e.Department)
            .ToListAsync();

        var officialDuties = await context.LeaveRequests
            .Where(l => l.Approved && l.RequestCategory == RequestCategory.OfficialDuty &&
                l.StartDate <= today && l.EndDate >= today)
            .Include(l => l.Employee).ThenInclude(e => e.Department)
            .ToListAsync();

        var suspendedEmployees = await context.Employees
            .Where(s => s.ActiveStatus == EmployeeActiveStatus.Suspension &&
                s.SuspensionStartDate <= today && s.SuspensionEndDate >= today)
            .Include(s => s.Department)
            .ToListAsync();
        
        var attendanceMap = dailyRecords.ToDictionary(r => r.EmployeeId);
        var groupedByDepartment = allEmployees.GroupBy(e => e.Department?.Name ?? "Unassigned").ToList();

        var departmentReports = new List<GeneralAttendanceReportDto>();

        foreach (var group in groupedByDepartment)
        {
            var departmentName = group.Key;
            var summary = new GeneralAttendanceReportDto { DepartmentName = departmentName };

            foreach (var employee in group)
            {
                var isCasual = employee.Type == EmployeeType.Casual;

                if (attendanceMap.ContainsKey(employee.StaffNumber))
                {
                    if (!shiftAssignmentMap.TryGetValue(employee.Id, out var shiftAssignment) ||
                        shiftAssignment?.ShiftType?.StartTime == null)
                        continue;

                    if (!DateTime.TryParseExact(
                            shiftAssignment.ShiftType.StartTime,
                            "hh:mm tt",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out var parsedShiftStartTime))
                        continue;

                    var shiftStartTime = parsedShiftStartTime.TimeOfDay;

                    if (isCasual) summary.CasualStaff++;
                    else summary.PermanentStaff++;

                    if (shiftStartTime >= TimeSpan.FromHours(5) && shiftStartTime < TimeSpan.FromHours(12))
                    {
                        if (isCasual) summary.CasualMorning++;
                        else summary.PermanentMorning++;
                    }
                    else if (shiftStartTime >= TimeSpan.FromHours(12) && shiftStartTime < TimeSpan.FromHours(17))
                    {
                        if (isCasual) summary.CasualAfternoon++;
                        else summary.PermanentAfternoon++;
                    }
                    else
                    {
                        if (isCasual) summary.CasualNight++;
                        else summary.PermanentNight++;
                    }
                }
                else if (suspendedEmployees.Any(s => s.Id == employee.Id))
                {
                    summary.Suspensions++;
                }
                else if (absences.Any(a => a.EmployeeId == employee.Id))
                {
                    summary.Absences++;
                }
            }

            // Add leaves by department
            summary.ApprovedLeaves = approvedLeaves.Count(l => l.Employee.Department?.Name == departmentName);
            summary.SickLeaves = sickLeaves.Count(l => l.Employee.Department?.Name == departmentName);
            summary.MaternityLeaves = maternityLeaves.Count(l => l.Employee.Department?.Name == departmentName);

            departmentReports.Add(summary);
        }

        // ✅ System-Wide Stats (Yellow Section)
        var departmentStats = groupedByDepartment.Select(group =>
        {
            var deptName = group.Key;
            return new SystemGeneraltaffCountDto
            {
                Department = deptName,
                NumberOfPermanentLeaves = approvedLeaves.Count(l => l.Employee.Department?.Name == deptName && l.Employee.Type == EmployeeType.Permanent),
                NumberOfCasualLeaves = approvedLeaves.Count(l => l.Employee.Department?.Name == deptName && l.Employee.Type == EmployeeType.Casual),

                NumberOfPermanentSickLeaves = sickLeaves.Count(l => l.Employee.Department?.Name == deptName && l.Employee.Type == EmployeeType.Permanent),
                NumberOfCasualSickLeaves = sickLeaves.Count(l => l.Employee.Department?.Name == deptName && l.Employee.Type == EmployeeType.Casual),

                NumberOfPermanentMaternityLeave = maternityLeaves.Count(l => l.Employee.Department?.Name == deptName && l.Employee.Type == EmployeeType.Permanent),
                NumberOfCasualMaternityLeave = maternityLeaves.Count(l => l.Employee.Department?.Name == deptName && l.Employee.Type == EmployeeType.Casual),

                NumberOfPermanentAbsentEmployees = absences.Count(a => a.Employee.Department?.Name == deptName && a.Employee.Type == EmployeeType.Permanent),
                NumberOfCasualAbsentEmployees = absences.Count(a => a.Employee.Department?.Name == deptName && a.Employee.Type == EmployeeType.Casual),

                NumberOfPermanentOfficialDuty = officialDuties.Count(o => o.Employee.Department?.Name == deptName && o.Employee.Type == EmployeeType.Permanent),
                NumberOfCasualOfficialDuty = officialDuties.Count(o => o.Employee.Department?.Name == deptName && o.Employee.Type == EmployeeType.Casual),

                NumberOfPermanentSuspensions = suspendedEmployees.Count(s => s.Department?.Name == deptName && s.Type == EmployeeType.Permanent),
                NumberOfCasualSuspensions = suspendedEmployees.Count(s => s.Department?.Name == deptName && s.Type == EmployeeType.Casual),
            };
        }).ToList();

        var systemStats = new SystemGeneralStats
        {
            NumberOfPermanentLeaves = departmentStats.Sum(d => d.NumberOfPermanentLeaves),
            NumberOfCasualLeaves = departmentStats.Sum(d => d.NumberOfCasualLeaves),
            NumberOfPermanentSickLeaves = departmentStats.Sum(d => d.NumberOfPermanentSickLeaves),
            NumberOfCasualSickLeaves = departmentStats.Sum(d => d.NumberOfCasualSickLeaves),
            NumberOfPermanentMaternityLeave = departmentStats.Sum(d => d.NumberOfPermanentMaternityLeave),
            NumberOfCasualMaternityLeave = departmentStats.Sum(d => d.NumberOfCasualMaternityLeave),
            NumberOfPermanentAbsentEmployees = departmentStats.Sum(d => d.NumberOfPermanentAbsentEmployees),
            NumberOfCasualAbsentEmployees = departmentStats.Sum(d => d.NumberOfCasualAbsentEmployees),
            NumberOfPermanentOfficialDuty = departmentStats.Sum(d => d.NumberOfPermanentOfficialDuty),
            NumberOfCasualOfficialDuty = departmentStats.Sum(d => d.NumberOfCasualOfficialDuty),
            NumberOfPermanentSuspensions = departmentStats.Sum(d => d.NumberOfPermanentSuspensions),
            NumberOfCasualSuspensions = departmentStats.Sum(d => d.NumberOfCasualSuspensions)
        };

        return Result.Success(new GeneralAttendanceReportResponse
        {
            DepartmentReports = departmentReports,
            SystemStatistics = new StaffGenderRatioReport
            {
                Departments = departmentStats,
                Totals = systemStats
            }
        });
    }
    
    public async Task<Result<FileExportResult>> ExportAttendanceSummary(FileFormat format)
    {
        var attendanceResult = await GeneralAttendanceReport();
        if (!attendanceResult.IsSuccess)
            return Error.Failure("Export.Failed", "Failed to generate attendance report.");
        
        var report = attendanceResult.Value; // now contains DepartmentReports + SystemStatistics
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        
        if (format.FileType == "csv")
        {
            var sb = new StringBuilder();
            sb.AppendLine("Department,Permanent,Casual,Morning(P),Afternoon(P),Night(P),Morning(C),Afternoon(C),Night(C),Absent,Suspended,Sick,Maternity,Leave");

            foreach (var item in report.DepartmentReports)
            {
                sb.AppendLine($"{item.DepartmentName},{item.PermanentStaff},{item.CasualStaff}," +
                              $"{item.PermanentMorning},{item.PermanentAfternoon},{item.PermanentNight}," +
                              $"{item.CasualMorning},{item.CasualAfternoon},{item.CasualNight}," +
                              $"{item.Absences}," +
                              $"{item.Suspensions}," +
                              $"{item.SickLeaves}," +
                              $"{item.MaternityLeaves}," +
                              $"{item.ApprovedLeaves}");
            }

            // Add a separator for system statistics
            sb.AppendLine();
            sb.AppendLine("===== System Wide Breakdown =====");
            sb.AppendLine("Department,Perm.Leaves,Cas.Leaves,Perm.Sick,Cas.Sick,Perm.Maternity,Cas.Maternity,Perm.Absent,Cas.Absent,Perm.OfficialDuty,Cas.OfficialDuty,Perm.Suspended,Cas.Suspended");

            foreach (var dept in report.SystemStatistics.Departments)
            {
                sb.AppendLine($"{dept.Department}," +
                              $"{dept.NumberOfPermanentLeaves},{dept.NumberOfCasualLeaves}," +
                              $"{dept.NumberOfPermanentSickLeaves},{dept.NumberOfCasualSickLeaves}," +
                              $"{dept.NumberOfPermanentMaternityLeave},{dept.NumberOfCasualMaternityLeave}," +
                              $"{dept.NumberOfPermanentAbsentEmployees},{dept.NumberOfCasualAbsentEmployees}," +
                              $"{dept.NumberOfPermanentOfficialDuty},{dept.NumberOfCasualOfficialDuty}," +
                              $"{dept.NumberOfPermanentSuspensions},{dept.NumberOfCasualSuspensions}");
            }

            // Add totals
            var totals = report.SystemStatistics.Totals;
            sb.AppendLine();
            sb.AppendLine("TOTALS," +
                          $"{totals.NumberOfPermanentLeaves},{totals.NumberOfCasualLeaves}," +
                          $"{totals.NumberOfPermanentSickLeaves},{totals.NumberOfCasualSickLeaves}," +
                          $"{totals.NumberOfPermanentMaternityLeave},{totals.NumberOfCasualMaternityLeave}," +
                          $"{totals.NumberOfPermanentAbsentEmployees},{totals.NumberOfCasualAbsentEmployees}," +
                          $"{totals.NumberOfPermanentOfficialDuty},{totals.NumberOfCasualOfficialDuty}," +
                          $"{totals.NumberOfPermanentSuspensions},{totals.NumberOfCasualSuspensions}");

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return Result.Success(new FileExportResult
            {
                FileBytes = bytes,
                ContentType = "text/csv",
                FileName = $"AttendanceSummary_{timestamp}.csv"
            });
        }

        // ---------- EXCEL ----------
        ExcelPackage.License.SetNonCommercialPersonal("Oryx");
        using var package = new ExcelPackage();

        // Department Report
        var deptSheet = package.Workbook.Worksheets.Add("Department Summary");
        deptSheet.Cells[1, 1].Value = "Department";
        deptSheet.Cells[1, 2].Value = "Permanent Staff";
        deptSheet.Cells[1, 3].Value = "Casual Staff";
        deptSheet.Cells[1, 4].Value = "Morning (P)";
        deptSheet.Cells[1, 5].Value = "Afternoon (P)";
        deptSheet.Cells[1, 6].Value = "Night (P)";
        deptSheet.Cells[1, 7].Value = "Morning (C)";
        deptSheet.Cells[1, 8].Value = "Afternoon (C)";
        deptSheet.Cells[1, 9].Value = "Night (C)";
        deptSheet.Cells[1, 10].Value = "Absences";
        deptSheet.Cells[1, 11].Value = "Suspensions";
        deptSheet.Cells[1, 12].Value = "Sick Leaves";
        deptSheet.Cells[1, 13].Value = "Maternity Leaves";
        deptSheet.Cells[1, 14].Value = "Approved Leaves";

        var row = 2;
        foreach (var item in report.DepartmentReports)
        {
            deptSheet.Cells[row, 1].Value = item.DepartmentName;
            deptSheet.Cells[row, 2].Value = item.PermanentStaff;
            deptSheet.Cells[row, 3].Value = item.CasualStaff;
            deptSheet.Cells[row, 4].Value = item.PermanentMorning;
            deptSheet.Cells[row, 5].Value = item.PermanentAfternoon;
            deptSheet.Cells[row, 6].Value = item.PermanentNight;
            deptSheet.Cells[row, 7].Value = item.CasualMorning;
            deptSheet.Cells[row, 8].Value = item.CasualAfternoon;
            deptSheet.Cells[row, 9].Value = item.CasualNight;
            deptSheet.Cells[row, 10].Value = item.Absences;
            deptSheet.Cells[row, 11].Value = item.Suspensions;
            deptSheet.Cells[row, 12].Value = item.SickLeaves;
            deptSheet.Cells[row, 13].Value = item.MaternityLeaves;
            deptSheet.Cells[row, 14].Value = item.ApprovedLeaves;
            row++;
        }
        deptSheet.Cells.AutoFitColumns();

        // System-wide report
        var sysSheet = package.Workbook.Worksheets.Add("System Statistics");
        sysSheet.Cells[1, 1].Value = "Department";
        sysSheet.Cells[1, 2].Value = "Permanent Leaves";
        sysSheet.Cells[1, 3].Value = "Casual Leaves";
        sysSheet.Cells[1, 4].Value = "Permanent Sick";
        sysSheet.Cells[1, 5].Value = "Casual Sick";
        sysSheet.Cells[1, 6].Value = "Permanent Maternity";
        sysSheet.Cells[1, 7].Value = "Casual Maternity";
        sysSheet.Cells[1, 8].Value = "Permanent Absent";
        sysSheet.Cells[1, 9].Value = "Casual Absent";
        sysSheet.Cells[1, 10].Value = "Permanent OfficialDuty";
        sysSheet.Cells[1, 11].Value = "Casual OfficialDuty";
        sysSheet.Cells[1, 12].Value = "Permanent Suspended";
        sysSheet.Cells[1, 13].Value = "Casual Suspended";

        var sysRow = 2;
        foreach (var dept in report.SystemStatistics.Departments)
        {
            sysSheet.Cells[sysRow, 1].Value = dept.Department;
            sysSheet.Cells[sysRow, 2].Value = dept.NumberOfPermanentLeaves;
            sysSheet.Cells[sysRow, 3].Value = dept.NumberOfCasualLeaves;
            sysSheet.Cells[sysRow, 4].Value = dept.NumberOfPermanentSickLeaves;
            sysSheet.Cells[sysRow, 5].Value = dept.NumberOfCasualSickLeaves;
            sysSheet.Cells[sysRow, 6].Value = dept.NumberOfPermanentMaternityLeave;
            sysSheet.Cells[sysRow, 7].Value = dept.NumberOfCasualMaternityLeave;
            sysSheet.Cells[sysRow, 8].Value = dept.NumberOfPermanentAbsentEmployees;
            sysSheet.Cells[sysRow, 9].Value = dept.NumberOfCasualAbsentEmployees;
            sysSheet.Cells[sysRow, 10].Value = dept.NumberOfPermanentOfficialDuty;
            sysSheet.Cells[sysRow, 11].Value = dept.NumberOfCasualOfficialDuty;
            sysSheet.Cells[sysRow, 12].Value = dept.NumberOfPermanentSuspensions;
            sysSheet.Cells[sysRow, 13].Value = dept.NumberOfCasualSuspensions;
            sysRow++;
        }

        // Add totals row
        var systemTotals = report.SystemStatistics.Totals;
        sysSheet.Cells[sysRow, 1].Value = "TOTALS";
        sysSheet.Cells[sysRow, 2].Value = systemTotals.NumberOfPermanentLeaves;
        sysSheet.Cells[sysRow, 3].Value = systemTotals.NumberOfCasualLeaves;
        sysSheet.Cells[sysRow, 4].Value = systemTotals.NumberOfPermanentSickLeaves;
        sysSheet.Cells[sysRow, 5].Value = systemTotals.NumberOfCasualSickLeaves;
        sysSheet.Cells[sysRow, 6].Value = systemTotals.NumberOfPermanentMaternityLeave;
        sysSheet.Cells[sysRow, 7].Value = systemTotals.NumberOfCasualMaternityLeave;
        sysSheet.Cells[sysRow, 8].Value = systemTotals.NumberOfPermanentAbsentEmployees;
        sysSheet.Cells[sysRow, 9].Value = systemTotals.NumberOfCasualAbsentEmployees;
        sysSheet.Cells[sysRow, 10].Value = systemTotals.NumberOfPermanentOfficialDuty;
        sysSheet.Cells[sysRow, 11].Value = systemTotals.NumberOfCasualOfficialDuty;
        sysSheet.Cells[sysRow, 12].Value = systemTotals.NumberOfPermanentSuspensions;
        sysSheet.Cells[sysRow, 13].Value = systemTotals.NumberOfCasualSuspensions;

        sysSheet.Cells.AutoFitColumns();

        return Result.Success(new FileExportResult
        {
            FileBytes = await package.GetAsByteArrayAsync(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            FileName = $"AttendanceSummary_{timestamp}.xlsx"
        });
    }

    private async Task<int> EmployeesOnSickLeave(DateTime today)
    {
        var sickLeaveTypeId = await context.LeaveTypes
            .Where(t => t.Name == "Sick Leave")
            .Select(t => t.Id)
            .FirstOrDefaultAsync();

        return await context.LeaveRequests
            .Where(l => l.LeaveTypeId == sickLeaveTypeId &&
                        l.Approved &&
                        l.StartDate <= today &&
                        l.EndDate >= today)
            .CountAsync();
    }

    private async Task<int> NumberOfApprovedLeaves(DateTime today)
    {
        var leaveType = await context.LeaveTypes
            .Where(t => t.Name != "Maternity Leave" 
                        && t.Name != "Sick Leave")
            .Select(t => t.Id)
            .FirstOrDefaultAsync();

        return await context.LeaveRequests
            .Where(l => l.LeaveTypeId == leaveType
                        && l.RequestCategory != RequestCategory.OfficialDuty
                        && l.Approved &&
                        l.StartDate <= today &&
                        l.EndDate >= today)
            .CountAsync();
    }
}