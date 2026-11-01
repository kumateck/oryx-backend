using APP.Utils;
using DOMAIN.Entities.Materials.StockMigrations;
using Microsoft.AspNetCore.Http;
using SHARED;

namespace APP.IRepository;

public interface IStockMigrationRepository
{
    Task<Result<Guid>> Upload(IFormFile file, Guid departmentId, string massUnit, string volumeUnit, Guid userId);
    Task<Result<List<StockMigrationDto>>> List(Guid? departmentId);
    Task<Guid?> DepartmentOf(Guid id);
    Task<Result<StockMigrationDto>> Get(Guid id);
    Task<Result<Paginateable<IEnumerable<StockMigrationLineDto>>>> GetLines(Guid id, StockMigrationLineFilter filter);
    Task<Result> UpdateLine(Guid id, Guid lineId, UpdateStockMigrationLineRequest request);
    Task<Result> BulkUpdate(Guid id, BulkUpdateStockMigrationLinesRequest request);
    Task<Result> UpdateUnits(Guid id, UpdateStockMigrationUnitsRequest request);
    Task<Result> Recheck(Guid id);
    Task<Result<List<StockMigrationComparisonDto>>> Comparison(Guid id);
    Task<Result> Approve(Guid id, Guid userId);
    Task<Result> Discard(Guid id);
}
