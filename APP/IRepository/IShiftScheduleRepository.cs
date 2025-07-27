using APP.Utils;
using DOMAIN.Entities.ShiftAssignments;
using DOMAIN.Entities.ShiftSchedules;
using Microsoft.AspNetCore.Http;
using SHARED;

namespace APP.IRepository;

public interface IShiftScheduleRepository
{
    Task<Result<Guid>> CreateShiftSchedule(CreateShiftScheduleRequest request);
    Task<Result<Paginateable<IEnumerable<ShiftScheduleDto>>>> GetShiftSchedules(int page, int pageSize, string searchQuery);
    Task<Result<ShiftScheduleDto>> GetShiftSchedule(Guid id);
    Task<Result<IEnumerable<ShiftAssignmentDto>>> GetShiftScheduleDayView(Guid shiftScheduleId, DateTime date);
    
    Task<Result<IEnumerable<ShiftAssignmentDto>>> GetShiftScheduleRangeView(Guid shiftScheduleId, DateTime startDate, DateTime endDate);
    
    Task<Result> AssignEmployeesToShift(AssignShiftRequest request);
    Task<Result> UpdateShiftSchedule(Guid id, CreateShiftScheduleRequest request);

    Task<Result> UpdateShiftAssignment(Guid id, UpdateShiftAssignment request);
    Task<Result> DeleteShiftSchedule(Guid id, Guid userId);
    Task<Result> ImportShiftAssignmentsFromExcel(IFormFile file, Guid departmentId, Guid shiftId);
}