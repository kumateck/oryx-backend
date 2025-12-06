using APP.Utils;
using DOMAIN.Entities.Invoices;
using DOMAIN.Entities.ProductionOrders;
using DOMAIN.Entities.ProformaInvoices;
using SHARED;

namespace APP.IRepository;
public interface IProductionOrderRepository
{
    // Production Orders
    Task<Result<Guid>> CreateProductionOrder(CreateProductionOrderRequest request);
    Task<Result<Paginateable<IEnumerable<ProductionOrderDto>>>>
        GetProductionOrders(int page, int pageSize, string searchQuery, ProductionOrderStatus? status);
    Task<Result<ProductionOrderDetailDto>> GetProductionOrder(Guid id);
    Task<Result> UpdateProductionOrder(Guid id, CreateProductionOrderRequest request);
    Task<Result> DeleteProductionOrder(Guid id, Guid userId);


    // Proforma Invoices
    Task<Result<Guid>> CreateProformaInvoice(CreateProformaInvoice request);
    Task<Result> SendProformaInvoiceToCustomer(Guid proformaInvoiceId, Guid userId);
    Task<Result<Paginateable<IEnumerable<ProformaInvoiceDto>>>> GetProformaInvoices(int page, 
        int pageSize, 
        string searchQuery, 
        ProformaInvoiceStatus? status = null,
        bool? approved = null);
    Task<Result<ProformaInvoiceDto>> GetProformaInvoice(Guid id);
    Task<Result> UpdateProformaInvoice(Guid id, CreateProformaInvoice request);
    Task<Result> DeleteProformaInvoice(Guid id, Guid userId);

    // In IProductionOrderRepository
    Task<Result<Guid>> CreateInvoice(CreateInvoice request);
    Task<Result<Paginateable<IEnumerable<InvoiceDto>>>> GetInvoices(int page, int pageSize, string searchQuery);
    Task<Result<InvoiceDto>> GetInvoice(Guid id);
    Task<Result> UpdateInvoice(Guid id, CreateInvoice request);
    Task<Result> DeleteInvoice(Guid id, Guid userId);
    Task<Result> AllocateProduct(AllocateProductionOrderRequest request);
    Task<Result> MarkAllocationProductionOrderAsLoaded(Guid id);
    Task<Result> CreateWaybillFromProductionOrder(CreateProductionOrderWaybill request, Guid id);
    Task<Result<Paginateable<IEnumerable<ProductionOrderWaybillDto>>>> GetProductionOrderWaybills(
        int page,
        int pageSize,
        string searchQuery,
        Guid? allocateProductionOrderId = null);
    Task<Result<ProductionOrderWaybillDto>> GetProductionOrderWaybill(Guid id);
    Task<Result> SendWaybillToCustomer(Guid id);
    Task<Result> MarkAllocationProductionOrderAsDelivered(Guid id);

    Task<Result<Guid>> CreateProductOrderAllocation(AllocateProductionOrderRequest request);
    Task<Result<Paginateable<IEnumerable<AllocateProductionOrderDto>>>> GetProductAllocations(
        bool? onlyApproved, int page,
        int pageSize, string searchQuery, Guid? productionOrderId);
    Task<Result<AllocateProductionOrderDto>> GetProductAllocation(Guid id);
}