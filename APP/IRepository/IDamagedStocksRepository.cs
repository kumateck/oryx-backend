using APP.Utils;
using DOMAIN.Entities.DamagedStocks;
using SHARED;

namespace APP.IRepository;

public interface IDamagedStocksRepository
{
    Task<Result<Guid>> CreateDamagedStocks(CreateDamagedStockRequest request, Guid userId);
    Task<Result<Paginateable<IEnumerable<DamagedStockDto>>>> GetDamagedStocks(int page, int pageSize, string searchQuery,
        IReadOnlyCollection<DOMAIN.Entities.Items.Store> readableStores, DOMAIN.Entities.Items.Store? store, DamageStatus? damageStatus);
    Task<Result<DamagedStockDto>> GetDamagedStock(Guid id);
    Task<Result> UpdateDamagedStocks(Guid id, CreateDamagedStockRequest request, Guid userId);
    Task<Result> DeleteDamagedStocks(Guid id, Guid userId);
}