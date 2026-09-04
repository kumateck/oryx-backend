using APP.Utils;
using DOMAIN.Entities.ShiftAssignments;
using SHARED;

namespace APP.IRepository;

public interface IShiftCategoryRepository
{
    Task<Result<Guid>> CreateShiftCategory(CreateShiftCategoryRequest request);
    Task<Result<Paginateable<IEnumerable<ShiftCategoryDto>>>> GetShiftCategories(int page, int pageSize, string searchQuery);
    Task<Result<ShiftCategoryDto>> GetShiftCategory(Guid id);
    Task<Result> UpdateShiftCategory(Guid id, CreateShiftCategoryRequest request);
    Task<Result> DeleteShiftCategory(Guid id, Guid userId);
}
