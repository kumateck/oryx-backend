using APP.Utils;
using DOMAIN.Entities.ShiftSchedules;
using SHARED;

namespace APP.IRepository;

public interface IShiftScheduleRepository
{
    Task<Result<Guid>> CreateShiftSchedule(CreateShiftScheduleRequest request, Guid userId);
    Task<Result<Paginateable<IEnumerable<ShiftScheduleDto>>>> GetShiftSchedules(int page, int pageSize, string searchQuery);
    Task<Result<ShiftScheduleDto>> GetShiftSchedule(Guid id);
    Task<Result> UpdateShiftSchedule(Guid id, CreateShiftScheduleRequest request, Guid userId);
    Task<Result> DeleteShiftSchedule(Guid id, Guid userId);
    
    
}