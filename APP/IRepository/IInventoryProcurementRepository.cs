/*using APP.Utils;
using DOMAIN.Entities.Items.Requisitions;
using DOMAIN.Entities.Memos;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Requisitions.Request;
using SHARED;

namespace APP.IRepository;

public interface IInventoryProcurementRepository
{
    // CRUD for InventoryPurchaseRequisition
    Task<Result<Guid>> CreateInventoryPurchaseRequisition(CreateInventoryPurchaseRequisition request, Guid userId);
    Task<Result> UpdateInventoryPurchaseRequisition(Guid id, CreateInventoryPurchaseRequisition request);
    Task<Result> DeleteInventoryPurchaseRequisition(Guid id);
    Task<Result<InventoryPurchaseRequisitionDto>> GetInventoryPurchaseRequisition(Guid id);
    Task<Result<Paginateable<IEnumerable<InventoryPurchaseRequisitionDto>>>> GetInventoryPurchaseRequisitions(int page, int pageSize, string searchQuery);

    // Sourcing Logic
    Task<Result> CreateSourceRequisition(CreateSourceInventoryRequisition request, Guid userId);
    Task<Result> CreateMarketRequisition(CreateMarketRequisition request, Guid userId);
    Task<Result<Paginateable<IEnumerable<MarketRequisitionDto>>>> GetMarketRequisitions(int page, int pageSize);
    Task<Result<List<SupplierPriceComparison>>> GetPriceComparisonOfMaterial(SupplierType supplierType);

    // Memo Creation Logic
    Task<Result> ProcessOpenMarketMemo(List<ProcessMemo> memos, Guid userId);
    Task<Result> ProcessTrustedVendorQuotationAndCreateMemo(List<ProcessMemo> memos, Guid userId);

    // Trusted Vendor Specific
    Task<Result> SendQuotationToSupplier(Guid supplierId);
    Task<Result<Paginateable<IEnumerable<SupplierQuotationDto>>>> GetSupplierQuotations(int page, int pageSize, SupplierType supplierType, bool received);
    Task<Result> ReceiveQuotationFromSupplier(List<SupplierQuotationResponseDto> supplierQuotationResponse, Guid supplierQuotationId);
    Task<Result<SupplierQuotationDto>> GetSupplierQuotation(Guid supplierQuotationId);

    // Open Market Specific
    Task<Result<Paginateable<IEnumerable<MarketRequisitionVendorDto>>>> GetMarketRequisitionVendors(int page, int pageSize, bool complete);
    Task<Result> CreateMarketRequisitionVendor(CreateMarketRequisitionVendor request);
    Task<Result> ConfirmMarketRequisitionVendor(Guid marketRequisitionVendorId);
    
    // Helper methods
    Task<string> GenerateMemoCode();
}*/