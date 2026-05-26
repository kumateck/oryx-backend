using System.Globalization;
using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.LeaveRequests;
using DOMAIN.Entities.ShiftAssignments;
using DOMAIN.Entities.ShiftSchedules;
using DOMAIN.Entities.ShiftTypes;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using SHARED;
using SHARED.Requests;

namespace APP.Repository;

public class ShiftScheduleRepository(ApplicationDbContext context, IMapper mapper) : IShiftScheduleRepository
{
    public async Task<Result<Guid>> CreateShiftSchedule(CreateShiftScheduleRequest request)
    {
        var shiftSchedule = await context.ShiftSchedules
            .FirstOrDefaultAsync(s => s.ScheduleName == request.ScheduleName
                                      && s.DepartmentId == request.DepartmentId
                                      && s.StartDate == request.StartDate);
        if (shiftSchedule is not null)
        {
            return Error.Validation("ShiftSchedule.Exists", "Shift schedule already exists.");
        }

        if (request.StartDate == DateTime.Today)
        {
            return Error.Validation("ShiftSchedule.Today", "Shift schedule cannot start on today.");
        }

        var shiftTypes = await context.ShiftTypes
            .Where(shift => request.ShiftTypeIds.Contains(shift.Id)).ToListAsync();

        if (shiftTypes.Count != request.ShiftTypeIds.Count)
        {
            return Error.Validation("ShiftSchedule.InvalidShiftTypes", "One or more shift type IDs are invalid.");
        }

        var department = await context.Departments.FirstOrDefaultAsync(d => d.Id == request.DepartmentId);
        if (department is null)
        {
            return Error.Validation("Department.NotFound", "Department not found.");
        }


        var shiftScheduleEntity = mapper.Map<ShiftSchedule>(request);

        shiftScheduleEntity.ShiftTypes = shiftTypes;
        shiftScheduleEntity.ScheduleStatus = ScheduleStatus.New;

        shiftScheduleEntity.EndDate = ComputeEndDate(request.StartDate, shiftScheduleEntity.Frequency);

        await context.ShiftSchedules.AddAsync(shiftScheduleEntity);
        await context.SaveChangesAsync();

        return shiftScheduleEntity.Id;
    }

    private static DateTime ComputeEndDate(DateTime startDate, ScheduleFrequency frequency)
    {
        return frequency switch
        {
            ScheduleFrequency.Week => startDate.AddDays(6),
            ScheduleFrequency.Biweek => startDate.AddDays(13),
            ScheduleFrequency.Month => startDate.AddMonths(1).AddDays(-1),
            ScheduleFrequency.Quarter => startDate.AddMonths(3).AddDays(-1),
            ScheduleFrequency.Half => startDate.AddMonths(6).AddDays(-1),
            ScheduleFrequency.Year => startDate.AddYears(1).AddDays(-1),
            _ => startDate
        };
    }

    public async Task<Result<Paginateable<IEnumerable<ShiftScheduleDto>>>> GetShiftSchedules(int page, int pageSize, string searchQuery, ScheduleStatus? status = null, ScheduleFrequency? frequency = null)
    {
        var query = context.ShiftSchedules
            .Include(schedule => schedule.Department)
            .Include(schedule => schedule.ShiftTypes)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(s => s.ScheduleStatus == status.Value);
        }

        if (frequency.HasValue)
        {
            query = query.Where(s => s.Frequency == frequency.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.Department.Name);
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            if (Enum.TryParse<ScheduleFrequency>(searchQuery, true, out var parsedFrequency))
            {
                query = query.Where(q => q.Frequency == parsedFrequency);
            }

        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            if (Enum.TryParse<ScheduleStatus>(searchQuery, true, out var parsedStatus))
            {
                query = query.Where(q => q.ScheduleStatus == parsedStatus);
            }
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<ShiftScheduleDto>);
    }

    public async Task<Result<ShiftScheduleDto>> GetShiftSchedule(Guid id)
    {
        var shiftSchedule = await context.ShiftSchedules
            .Include(schedule => schedule.Department)
            .Include(schedule => schedule.ShiftTypes).FirstOrDefaultAsync(s => s.Id == id);

        return shiftSchedule is null ?
            Error.NotFound("ShiftSchedule.NotFound", "Shift schedule is not found") :
            Result.Success(mapper.Map<ShiftScheduleDto>(shiftSchedule));
    }

    public async Task<Result<List<ShiftScheduleDto>>> GetShiftScheduleByDepartment(Guid departmentId)
    {
        var shiftSchedule = await context.ShiftSchedules
            .Include(schedule => schedule.Department)
            .Include(schedule => schedule.ShiftTypes)
            .Where(s => s.DepartmentId == departmentId)
            .ToListAsync();

        return mapper.Map<List<ShiftScheduleDto>>(shiftSchedule);
    }

    public async Task<Result<IEnumerable<ShiftAssignmentDto>>> GetShiftScheduleRangeView(Guid shiftScheduleId, DateTime startDate, DateTime endDate)
    {
        var schedule = await context.ShiftAssignments
            .Where(s => s.ShiftScheduleId == shiftScheduleId)
            .Include(s => s.Employee)
            .ThenInclude(e => e.Designation)
            .Include(s => s.Employee)
            .ThenInclude(e => e.Department)
            .Include(sa => sa.ShiftType)
            .Include(sa => sa.ShiftCategory)
            .Include(sa => sa.ShiftSchedules).ToListAsync();

        var grouped = schedule
            .GroupBy(s => new
            {
                s.ShiftCategoryId,
                s.ShiftTypeId,
                s.ShiftScheduleId,
                s.ScheduleDate
            })
            .Select(g => new ShiftAssignmentDto
            {
                ScheduleDate = g.Key.ScheduleDate,
                ShiftCategory = g.Select(s => new ShiftCategoryDto
                {
                    Id = s.ShiftCategoryId,
                    Name = s.ShiftCategory?.Name
                }).FirstOrDefault(),

                ShiftType = g.Select(s => new MinimalShiftTypeDto
                {
                    ShiftTypeId = s.ShiftTypeId,
                    ShiftName = s.ShiftType?.ShiftName
                }).FirstOrDefault(),

                ShiftSchedule = g.Select(s => new MinimalShiftScheduleDto
                {
                    ScheduleId = s.ShiftScheduleId,
                    ScheduleName = s.ShiftSchedules.ScheduleName,
                    StartDate = s.ShiftSchedules.StartDate,
                    EndDate = s.ShiftSchedules.EndDate
                }).FirstOrDefault(),

                Employees = g.Select(s => new MinimalEmployeeInfoDto
                {
                    EmployeeId = s.Employee.Id,
                    FirstName = s.Employee.FirstName,
                    LastName = s.Employee.LastName,
                    StaffNumber = s.Employee.StaffNumber,
                    Type = s.Employee.Type.ToString(),
                    Department = s.Employee.Department?.Name,
                    Designation = s.Employee.Designation?.Name
                }).Distinct().ToList()
            }).ToList();

        return Result.Success(grouped.AsEnumerable());
    }

    public async Task<Result<IEnumerable<ShiftAssignmentDto>>> GetShiftScheduleDayView(Guid shiftScheduleId, DateTime date)
    {
        var schedule = await context.ShiftAssignments
            .Where(s => s.ShiftScheduleId == shiftScheduleId)
            .Include(s => s.Employee)
            .ThenInclude(e => e.Designation)
            .Include(s => s.Employee)
            .ThenInclude(e => e.Department)
            .Include(sa => sa.ShiftType)
            .Include(sa => sa.ShiftCategory)
            .Include(sa => sa.ShiftSchedules).ToListAsync();

        var grouped = schedule
            .GroupBy(s => new
            {
                s.ShiftScheduleId,
                s.ShiftCategoryId,
                s.ShiftTypeId,
                s.ScheduleDate
            })
            .Select(g => new ShiftAssignmentDto
            {
                ScheduleDate = g.Key.ScheduleDate,
                ShiftCategory = g.Select(s => new ShiftCategoryDto
                {
                    Id = s.ShiftCategoryId,
                    Name = s.ShiftCategory?.Name
                }).FirstOrDefault(),

                ShiftType = g.Select(s => new MinimalShiftTypeDto
                {
                    ShiftTypeId = s.ShiftTypeId,
                    ShiftName = s.ShiftType?.ShiftName
                }).FirstOrDefault(),

                ShiftSchedule = g.Select(s => new MinimalShiftScheduleDto
                {
                    ScheduleId = s.ShiftScheduleId,
                    ScheduleName = s.ShiftSchedules.ScheduleName,
                    StartDate = s.ShiftSchedules.StartDate,
                    EndDate = s.ShiftSchedules.EndDate
                }).FirstOrDefault(),

                Employees = g.Select(s => new MinimalEmployeeInfoDto
                {
                    EmployeeId = s.Employee.Id,
                    FirstName = s.Employee.FirstName,
                    LastName = s.Employee.LastName,
                    StaffNumber = s.Employee.StaffNumber,
                    Type = s.Employee.Type.ToString(),
                    Department = s.Employee.Department?.Name,
                    Designation = s.Employee.Designation?.Name
                }).Distinct().ToList()
            }).ToList();

        return Result.Success(grouped.AsEnumerable());
    }

    public async Task<Result> AssignEmployeesToShift(AssignShiftRequest request)
    {
        var shiftSchedule = await context.ShiftSchedules
            .Include(s => s.ShiftTypes)
            .Include(s => s.Employees)
            .FirstOrDefaultAsync(s => s.Id == request.ShiftScheduleId);

        if (shiftSchedule is null)
            return Error.NotFound("Shift.NotFound", "Shift schedule not found.");

        var shiftType = await context.ShiftTypes
            .FirstOrDefaultAsync(st => st.Id == request.ShiftTypeId);

        if (shiftType == null)
            return Error.NotFound("ShiftType.NotFound", "Shift type not found.");

        var shiftCategory = await context.ShiftCategories
            .FirstOrDefaultAsync(sc => sc.Id == request.ShiftCategoryId);

        if (shiftCategory == null)
            return Error.NotFound("ShiftCategory.NotFound", "Shift category not found.");

        var existingAssignment = await context.ShiftAssignments
            .FirstOrDefaultAsync(sa =>
                sa.ShiftScheduleId == request.ShiftScheduleId &&
                sa.ShiftTypeId == request.ShiftTypeId &&
                sa.ShiftCategoryId == request.ShiftCategoryId);

        if (existingAssignment is not null)
            return Error.Conflict("ShiftAssignment.Exists", "Shift assignment for this category already exists.");

        var employeeIds = request.EmployeeIds.Distinct().ToList();


        var startDate = shiftSchedule.StartDate.Date;
        var endDate = shiftSchedule.EndDate.Date;

        var leaveRequests = await context.LeaveRequests
            .Where(l =>
                employeeIds.Contains(l.EmployeeId) &&
                l.LeaveStatus == LeaveStatus.Approved &&
                l.EndDate.Date >= startDate &&
                l.StartDate.Date <= endDate)
            .Select(l => l.EmployeeId)
            .ToListAsync();

        var shiftAssignments = await context.ShiftAssignments
            .Where(sa =>
                employeeIds.Contains(sa.EmployeeId) &&
                sa.ShiftSchedules.StartDate <= endDate &&
                sa.ShiftSchedules.EndDate >= startDate)
            .Include(sa => sa.ShiftSchedules)
            .ThenInclude(ss => ss.ShiftTypes)
            .ToListAsync();

        var conflictingEmployees = shiftAssignments
            .Where(sa => sa.ShiftSchedules.ShiftTypes.Any(existing =>
                shiftSchedule.ShiftTypes.Any(newShift =>
                    ConvertTime(existing.StartTime) < ConvertTime(newShift.EndTime) &&
                    ConvertTime(existing.EndTime) > ConvertTime(newShift.StartTime))))
            .Select(sa => sa.EmployeeId)
            .ToHashSet();

        var availableEmployees = employeeIds
            .Where(id => !leaveRequests.Contains(id) && !conflictingEmployees.Contains(id))
            .ToList();

        if (availableEmployees.Count == 0)
            return Error.Validation("Employees.NotFound", "No valid employees could be assigned due to leave or conflicts.");

        var assignments = new List<ShiftAssignment>();

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            assignments.AddRange(availableEmployees.Select(id => new ShiftAssignment
            {
                Id = Guid.NewGuid(),
                EmployeeId = id,
                ShiftScheduleId = shiftSchedule.Id,
                ShiftCategoryId = request.ShiftCategoryId,
                ShiftTypeId = request.ShiftTypeId,
                ScheduleDate = date
            }));
        }

        await context.ShiftAssignments.AddRangeAsync(assignments);
        await context.SaveChangesAsync();

        shiftSchedule.ScheduleStatus = ScheduleStatus.Assigned;
        context.ShiftSchedules.Update(shiftSchedule);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> ImportShiftAssignmentsFromExcel(
    IFormFile file, Guid departmentId, Guid shiftId)
    {
        var department = await context.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == departmentId);

        if (department is null)
            return Error.NotFound(
                "Department.NotFound",
                "Department not found.");

        var shiftSchedule = await context.ShiftSchedules
            .Include(s => s.ShiftTypes)
            .FirstOrDefaultAsync(s => s.Id == shiftId);

        if (shiftSchedule is null)
            return Error.NotFound(
                "ShiftSchedule.NotFound",
                "Shift schedule not found.");

        var startDate = shiftSchedule.StartDate.Date;
        var endDate = shiftSchedule.EndDate.Date;

        if (startDate > endDate)
            return Error.Validation(
                "ShiftSchedule.InvalidDateRange",
                "Shift schedule has an invalid date range.");

        if (file == null || file.Length == 0)
            return UploadErrors.EmptyFile;

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position = 0;

        ExcelPackage.License.SetNonCommercialPersonal("Oryx");

        using var package = new ExcelPackage(stream);

        var worksheet = package.Workbook.Worksheets.FirstOrDefault();

        if (worksheet == null)
            return UploadErrors.WorksheetNotFound;

        if (worksheet.Dimension == null)
            return Error.Validation(
                "Worksheet.Empty",
                "The uploaded worksheet is empty.");

        var headers = new Dictionary<string, int>(
            StringComparer.OrdinalIgnoreCase);

        for (var col = 1; col <= worksheet.Dimension.End.Column; col++)
        {
            var header = worksheet.Cells[1, col].Text.Trim();

            if (!string.IsNullOrWhiteSpace(header))
                headers[header] = col;
        }

        var requiredHeaders = new[]
        {
            "STAFF ID",
            "CATEGORY",
            "SHIFT TYPE"
        };

        foreach (var header in requiredHeaders)
        {
            if (!headers.ContainsKey(header))
                return UploadErrors.MissingRequiredHeader(header);
        }

        // PRELOAD DATA

        var employees = await context.Employees
            .Where(e => e.DepartmentId == departmentId)
            .ToDictionaryAsync(
                e => e.StaffNumber.Trim(),
                StringComparer.OrdinalIgnoreCase);

        var shiftCategories = await context.ShiftCategories
            .ToDictionaryAsync(
                c => c.Name.Trim(),
                StringComparer.OrdinalIgnoreCase);

        var shiftTypes = await context.ShiftTypes
            .ToDictionaryAsync(
                t => t.ShiftName.Trim(),
                StringComparer.OrdinalIgnoreCase);

        var employeeIds = employees.Values
            .Select(e => e.Id)
            .ToList();

        var approvedLeaves = await context.LeaveRequests
            .Where(l =>
                employeeIds.Contains(l.EmployeeId) &&
                l.LeaveStatus == LeaveStatus.Approved &&
                l.EndDate.Date >= startDate &&
                l.StartDate.Date <= endDate)
            .ToListAsync();

        var existingAssignments = await context.ShiftAssignments
            .Where(sa =>
                employeeIds.Contains(sa.EmployeeId) &&
                sa.ShiftSchedules.StartDate <= endDate &&
                sa.ShiftSchedules.EndDate >= startDate)
            .Include(sa => sa.ShiftSchedules)
            .ThenInclude(ss => ss.ShiftTypes)
            .ToListAsync();

        var existingDailyAssignments = await context.ShiftAssignments
            .Where(sa =>
                employeeIds.Contains(sa.EmployeeId) &&
                sa.ScheduleDate >= startDate &&
                sa.ScheduleDate <= endDate)
            .Select(sa => new
            {
                sa.EmployeeId,
                sa.ScheduleDate,
                sa.ShiftScheduleId
            })
            .ToListAsync();

        var existingAssignmentSet = existingDailyAssignments
            .Select(x => $"{x.EmployeeId}_{x.ScheduleDate:yyyyMMdd}_{x.ShiftScheduleId}")
            .ToHashSet();

        var assignments = new List<ShiftAssignment>();
        var skipped = new List<string>();

        string GetCell(int row, string header)
            => worksheet.Cells[row, headers[header]].Text.Trim();

        static TimeSpan ParseTime(string time)
        {
            return TimeSpan.Parse(time);
        }

        static bool HasTimeOverlap(
            TimeSpan existingStart,
            TimeSpan existingEnd,
            TimeSpan currentStart,
            TimeSpan currentEnd)
        {
            // Handle overnight shifts

            if (existingEnd <= existingStart)
                existingEnd = existingEnd.Add(TimeSpan.FromDays(1));

            if (currentEnd <= currentStart)
                currentEnd = currentEnd.Add(TimeSpan.FromDays(1));

            return existingStart < currentEnd &&
                   existingEnd > currentStart;
        }

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        try
        {
            for (var row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                var staffIdStr = GetCell(row, "STAFF ID");
                var shiftCategoryName = GetCell(row, "CATEGORY");
                var shiftTypeName = GetCell(row, "SHIFT TYPE");

                if (string.IsNullOrWhiteSpace(staffIdStr))
                {
                    skipped.Add($"Row {row} - STAFF ID is empty");
                    continue;
                }

                if (!employees.TryGetValue(staffIdStr, out var employee))
                {
                    skipped.Add(
                        $"{staffIdStr} - Employee not found in selected department");

                    continue;
                }

                if (!shiftCategories.TryGetValue(
                        shiftCategoryName,
                        out var shiftCategory))
                {
                    skipped.Add(
                        $"{staffIdStr} - Shift category '{shiftCategoryName}' not found");

                    continue;
                }

                if (!shiftTypes.TryGetValue(
                        shiftTypeName,
                        out var shiftType))
                {
                    skipped.Add(
                        $"{staffIdStr} - Shift type '{shiftTypeName}' not found");

                    continue;
                }

                // LEAVE CHECK

                var hasLeave = approvedLeaves.Any(l =>
                    l.EmployeeId == employee.Id);

                if (hasLeave)
                {
                    skipped.Add(
                        $"{staffIdStr} - Employee is on approved leave during schedule period");

                    continue;
                }

                // CONFLICT CHECK

                var employeeAssignments = existingAssignments
                    .Where(sa => sa.EmployeeId == employee.Id)
                    .ToList();

                var hasConflict = employeeAssignments.Any(sa =>
                    sa.ShiftSchedules != null &&
                    sa.ShiftSchedules.ShiftTypes.Any(existing =>
                        shiftSchedule.ShiftTypes.Any(current =>
                            HasTimeOverlap(
                                ParseTime(existing.StartTime),
                                ParseTime(existing.EndTime),
                                ParseTime(current.StartTime),
                                ParseTime(current.EndTime)))));

                if (hasConflict)
                {
                    skipped.Add(
                        $"{staffIdStr} - Schedule conflict with existing assignment");

                    continue;
                }

                for (var date = startDate;
                     date <= endDate;
                     date = date.AddDays(1))
                {
                    var duplicateKey =
                        $"{employee.Id}_{date:yyyyMMdd}_{shiftSchedule.Id}";

                    if (existingAssignmentSet.Contains(duplicateKey))
                        continue;

                    assignments.Add(new ShiftAssignment
                    {
                        Id = Guid.NewGuid(),
                        EmployeeId = employee.Id,
                        ShiftScheduleId = shiftSchedule.Id,
                        ShiftCategoryId = shiftCategory.Id,
                        ShiftTypeId = shiftType.Id,
                        ScheduleDate = date,
                        CreatedAt = DateTime.UtcNow
                    });

                    existingAssignmentSet.Add(duplicateKey);
                }
            }

            if (assignments.Count == 0)
            {
                return Error.Validation(
                    "Assignments.NoneCreated",
                    "No valid assignments could be made due to conflicts or missing data.");
            }

            await context.ShiftAssignments.AddRangeAsync(assignments);

            shiftSchedule.ScheduleStatus = ScheduleStatus.Assigned;

            await context.SaveChangesAsync();

            await transaction.CommitAsync();

            var distinctEmployees = assignments
                .Select(a => a.EmployeeId)
                .Distinct()
                .Count();

            var message =
                $"Successfully imported {assignments.Count} assignments " +
                $"for {distinctEmployees} employees.";

            if (skipped.Count > 0)
            {
                message +=
                    $" Skipped {skipped.Count} rows: " +
                    $"{string.Join("; ", skipped)}";
            }

            return Result.Success(message);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            return Error.Failure(
                "ShiftAssignment.ImportFailed",
                $"An error occurred while importing shift assignments: {ex}.");
        }
    }

    private static TimeOnly ConvertTime(string time)
    {
        return TimeOnly.ParseExact(time, "hh:mm tt", CultureInfo.InvariantCulture);
    }

    public async Task<Result> UpdateShiftSchedule(Guid id, CreateShiftScheduleRequest request)
    {
        var shiftSchedule = await context.ShiftSchedules.Include(s => s.ShiftTypes).FirstOrDefaultAsync(s => s.Id == id);
        if (shiftSchedule is null)
        {
            return Error.NotFound("ShiftSchedule.NotFound", "Shift schedule is not found");
        }

        if (shiftSchedule.StartDate < DateTime.Today)
        {
            return Error.Validation("ShiftSchedule.NotModifiable", "Shift schedule is already running and cannot be modified.");
        }

        var shiftTypes = await context.ShiftTypes
            .Where(shift => request.ShiftTypeIds.Contains(shift.Id)).ToListAsync();
        if (shiftTypes.Count != request.ShiftTypeIds.Count)
        {
            return Error.Validation("ShiftSchedule.InvalidShiftTypes", "One or more shift type IDs are invalid.");
        }

        shiftSchedule.ShiftTypes.Clear();
        shiftSchedule.ShiftTypes.AddRange(shiftTypes);
        mapper.Map(request, shiftSchedule);

        context.ShiftSchedules.Update(shiftSchedule);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> UpdateShiftAssignment(Guid id, UpdateShiftAssignment request)
    {
        var shiftSchedule = await context.ShiftSchedules
            .Include(s => s.ShiftTypes)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (shiftSchedule == null)
            return Error.NotFound("Shift.NotFound", "Shift schedule not found.");

        var shiftDay = request.ScheduleDate.Date;

        // 🔹 Remove employees from shift
        bool? isRemovable = request.RemoveEmployeeIds.Count != 0;

        if (isRemovable == true)
        {
            var assignmentsToRemove = await context.ShiftAssignments
                .Where(sa =>
                    request.RemoveEmployeeIds.Contains(sa.EmployeeId) &&
                    sa.ShiftScheduleId == id).ToListAsync();

            if (assignmentsToRemove.Count != 0)
                context.ShiftAssignments.RemoveRange(assignmentsToRemove);
        }

        // 🔹 Add new employees to shift
        bool? isAdded = request.AddEmployeeIds.Count != 0;

        if (isAdded == true)
        {
            var employeeIds = request.AddEmployeeIds.Distinct().ToList();

            // Check leave, holidays, conflicts
            var employeesOnLeave = await context.LeaveRequests
                .Where(l =>
                    employeeIds.Contains(l.EmployeeId) &&
                    l.LeaveStatus == LeaveStatus.Approved &&
                    l.StartDate.Date <= shiftDay &&
                    l.EndDate.Date >= shiftDay)
                .Select(l => l.EmployeeId).ToListAsync();

            var isHoliday = await context.Holidays.AnyAsync(h => h.Date.Date == shiftDay);

            var conflictingAssignments = await context.ShiftAssignments
                .Include(sa => sa.ShiftSchedules)
                .ThenInclude(ss => ss.ShiftTypes)
                .Where(sa =>
                    employeeIds.Contains(sa.EmployeeId)).ToListAsync();

            var conflictingIds = conflictingAssignments
                .Where(sa => sa.ShiftSchedules.ShiftTypes.Any(existing =>
                    shiftSchedule.ShiftTypes.Any(newShift =>
                        ConvertTime(existing.StartTime) < ConvertTime(newShift.EndTime) &&
                        ConvertTime(existing.EndTime) > ConvertTime(newShift.StartTime))))
                .Select(sa => sa.EmployeeId)
                .ToList();

            var validNewAssignments = employeeIds
                .Where(eid =>
                    !employeesOnLeave.Contains(eid) &&
                    !isHoliday &&
                    !conflictingIds.Contains(eid))
                .Select(empId => new ShiftAssignment
                {
                    Id = Guid.NewGuid(),
                    EmployeeId = empId,
                    ShiftScheduleId = shiftSchedule.Id,
                    ShiftCategoryId = request.ShiftCategoryId
                })
                .ToList();

            await context.ShiftAssignments.AddRangeAsync(validNewAssignments);
        }

        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteShiftSchedule(Guid id, Guid userId)
    {
        var shiftSchedule = await context.ShiftSchedules.FirstOrDefaultAsync(s => s.Id == id);

        if (shiftSchedule is null)
        {
            return Error.NotFound("ShiftSchedule.NotFound", "Shift schedule is not found");
        }
        shiftSchedule.LastDeletedById = userId;
        shiftSchedule.DeletedAt = DateTime.UtcNow;
        shiftSchedule.ScheduleStatus = ScheduleStatus.Cancelled;

        context.ShiftSchedules.Update(shiftSchedule);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}