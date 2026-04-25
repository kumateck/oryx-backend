using SHARED;
using DOMAIN.Entities.StockAdjustments;

namespace APP.IRepository;

public interface IStockAdjustmentRepository
{
    Task<Result<StockAdjustmentSummaryDto>> CreateStockAdjustment(CreateStockAdjustmentRequest request, Guid userId);
}
