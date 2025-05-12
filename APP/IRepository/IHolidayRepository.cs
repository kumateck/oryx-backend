using APP.Utils;
using DOMAIN.Entities.Holidays;
using SHARED;

namespace APP.IRepository;

public interface IHolidayRepository
{
    Task<Result<Guid>> CreateHoliday(CreateHolidayRequest request);
    
    Task<Result<Paginateable<IEnumerable<HolidayDto>>>> GetHolidays(int page, int pageSize, string searchQuery);
    
    Task<Result<HolidayDto>> GetHoliday(Guid id);
    
    Task<Result> UpdateHoliday(CreateHolidayRequest request, Guid id);
    
    Task<Result> DeleteHoliday(Guid id, Guid userId);
}