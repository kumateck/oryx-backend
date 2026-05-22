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
    Task<Result<Paginateable<IEnumerable<StockAdjustmentSummaryDto>>>> GetStockAdjustments(
        int page,
        int pageSize,
        string searchQuery,
        bool? approved = null,
        StockAdjustmentTarget? targetType = null
    );
    Task<Result> ApplyStockAdjustment(Guid adjustmentId, Guid userId);
}
