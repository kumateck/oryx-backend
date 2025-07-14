using System.Globalization;
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

    for (var row = 2; row <= worksheet.Dimension.End.Row; row++)
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

// public async Task<Result<List<GeneralAttendanceReportDto>>> GeneralAttendanceReport()
// {
//     var today = DateTime.UtcNow.Date;
//
//     var dailyRecords = await context.AttendanceRecords
//         .Where(a => a.TimeStamp.Date == today && a.WorkState == WorkState.CheckIn)
//         .ToListAsync();
//
//     if (dailyRecords.Count == 0)
//         return Error.NotFound("Attendance.NotFound", $"No attendance records found for {today:dd-MM-yyyy}");
//
//     var employeeIds = dailyRecords.Select(r => r.EmployeeId).Distinct().ToList();
//
//     var employees = await context.Employees
//         .Include(e => e.Department)
//         .Where(e => employeeIds.Contains(e.StaffNumber))
//         .ToListAsync();
//
//     var groupedByDepartment = employees
//         .GroupBy(e => e.Department?.Name ?? "Unassigned")
//         .ToList();
//
//     var report = new List<GeneralAttendanceReportDto>();
//
//     foreach (var group in groupedByDepartment)
//     {
//         var summary = new GeneralAttendanceReportDto
//         {
//             DepartmentName = group.Key
//         };
//
//         foreach (var employee in group)
//         {
//             var shiftAssignment = context.ShiftAssignments
//                 .AsSplitQuery()
//                 .Include(shiftAssignment => shiftAssignment.ShiftType)
//                 .Include(s => s.ShiftSchedules)
//                 .Include(assignment => assignment.Employee)
//                 .FirstOrDefault(sa => sa.ShiftSchedules != null &&
//                                       sa.ShiftSchedules.StartDate.Date <= today &&
//                                       sa.ShiftSchedules.EndDate.Date >= today 
//                                       && sa.EmployeeId == employee.Id);
//
//             if (shiftAssignment?.ShiftType.StartTime == null)
//                 continue;
//
//             if (!TimeSpan.TryParse(shiftAssignment.ShiftType.StartTime, out var shiftStartTime))
//                 continue;
//
//             var attendance = dailyRecords
//                 .FirstOrDefault(a => a.EmployeeId == employee.StaffNumber);
//
//             if (attendance == null)
//                 continue;
//
//             var isCasual = employee.Type == EmployeeType.Casual;
//
//             if (isCasual)
//                 summary.CasualStaff++;
//             else
//                 summary.PermanentStaff++;
//
//             // Classify based on shift start 
//             if (shiftStartTime >= TimeSpan.FromHours(5) && shiftStartTime < TimeSpan.FromHours(12))
//             {
//                 if (isCasual) summary.CasualMorning++;
//                 else summary.PermanentMorning++;
//             }
//             else if (shiftStartTime >= TimeSpan.FromHours(12) && shiftStartTime < TimeSpan.FromHours(17))
//             {
//                 if (isCasual) summary.CasualAfternoon++;
//                 else summary.PermanentAfternoon++;
//             }
//             else
//             {
//                 if (isCasual) summary.CasualNight++;
//                 else summary.PermanentNight++;
//             }
//         }
//
//         report.Add(summary);
//     }
//
//     return report;
// }


    public async Task<Result<List<GeneralAttendanceReportDto>>> GeneralAttendanceReport()
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

        // Approved absences
        var approvedAbsences = await context.LeaveRequests
            .Where(a =>
                a.RequestCategory == RequestCategory.AbsenceRequest && 
                a.Approved && a.StartDate.Date <= today &&
                a.EndDate.Date >= today)
            .Select(a => a.EmployeeId)
            .ToListAsync();

        // Active suspensions
        var suspendedEmployees = await context.Employees
            .Where(s =>
                s.ActiveStatus == EmployeeActiveStatus.Suspension &&
                s.SuspensionStartDate.Value.Date <= today &&
                s.SuspensionEndDate.Value.Date >= today)
            .Select(s => s.Id)
            .ToListAsync();

        var attendanceMap = dailyRecords.ToDictionary(r => r.EmployeeId);

        var groupedByDepartment = allEmployees
            .GroupBy(e => e.Department?.Name ?? "Unassigned")
            .ToList();

        var report = new List<GeneralAttendanceReportDto>();

        foreach (var group in groupedByDepartment)
        {
            var summary = new GeneralAttendanceReportDto
            {
                DepartmentName = group.Key,
                Absences = new AbsencesDto { AbsentEmployees = [] },
                Suspensions = new SuspensionsDto { SuspendedEmployees = [] }
            };

            foreach (var employee in group)
            {
                var isCasual = employee.Type == EmployeeType.Casual;

                // If checked in today
                if (attendanceMap.ContainsKey(employee.StaffNumber))
                {
                    if (!shiftAssignmentMap.TryGetValue(employee.Id, out var shiftAssignment) ||
                        shiftAssignment?.ShiftType?.StartTime == null)
                        continue;

                    if (!DateTime.TryParseExact(shiftAssignment.ShiftType.StartTime, "hh:mm tt",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None, out var parsedShiftStartTime))
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
                else if (suspendedEmployees.Contains(employee.Id))
                {
                    summary.Suspensions.SuspendedEmployees.Add(new MinimalEmployeeInfoDto
                    {
                        EmployeeId = employee.Id,
                        StaffNumber = employee.StaffNumber,
                        FirstName = employee.FirstName,
                        LastName = employee.LastName
                    });
                }
                else if (approvedAbsences.Contains(employee.Id))
                {
                    summary.Absences.AbsentEmployees.Add(new MinimalEmployeeInfoDto
                    {
                        EmployeeId = employee.Id,
                        StaffNumber = employee.StaffNumber,
                        FirstName = employee.FirstName,
                        LastName = employee.LastName
                    });
                }
            }

            report.Add(summary);
        }

        return Result.Success(report);
    }
}