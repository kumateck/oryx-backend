using APP.Utils;
using DOMAIN.Entities.ShiftTypes;
using SHARED;

namespace APP.IRepository;

public interface IShiftTypeRepository
{
    Task<Result<Guid>> CreateShiftType(CreateShiftTypeRequest request);
    
    Task<Result<Paginateable<IEnumerable<ShiftTypeDto>>>> GetShiftTypes(int page, int pageSize, string searchQuery);
    
    Task<Result<ShiftTypeDto>> GetShiftType(Guid id);
    
    Task<Result> UpdateShiftType(Guid id, CreateShiftTypeRequest request);
    
    Task<Result> DeleteShiftType(Guid id, Guid userId);
}