using APP.Utils;
using DOMAIN.Entities.StockAdjustments;
using SHARED;

namespace APP.IRepository;

public interface IStockAdjustmentRepository
{
    Task<Result<StockAdjustmentSummaryDto>> CreateStockAdjustment(
        CreateStockAdjustmentRequest request,
        Guid userId
    );
    Task<Result<Paginateable<IEnumerable<StockAdjustmentSummaryDto>>>> GetStockAdjustmentHistory(
        int page,
        int pageSize,
        string searchQuery
    );
    Task<Result> ApplyStockAdjustment(Guid adjustmentId, Guid userId);
}
