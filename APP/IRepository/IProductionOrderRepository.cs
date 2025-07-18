using APP.Utils;
using DOMAIN.Entities.ProductionOrders;
using SHARED;

namespace APP.IRepository;

public interface IProductionOrderRepository
{
    Task<Result<Guid>> CreateProductionOrder(CreateProductionOrderRequest request);

    Task<Result<Paginateable<IEnumerable<ProductionOrderDto>>>> GetProductionOrders(int page, int pageSize,
        string searchQuery);
    Task<Result<ProductionOrderDto>> GetProductionOrder(Guid id);
    Task<Result> UpdateProductionOrder(Guid id, CreateProductionOrderRequest request);
    Task<Result> DeleteProductionOrder(Guid id, Guid userId);
}