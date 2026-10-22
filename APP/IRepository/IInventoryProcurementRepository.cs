using APP.Utils;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.Items.Requisitions;
using DOMAIN.Entities.Memos;
using DOMAIN.Entities.StockEntries;
using DOMAIN.Entities.VendorQuotations;
using SHARED;

namespace APP.IRepository;

public interface IInventoryProcurementRepository
{
    // CRUD for InventoryPurchaseRequisition
    Task<Result<Guid>> CreateInventoryPurchaseRequisition(CreateInventoryPurchaseRequisition request, Guid userId);
    Task<Result> UpdateInventoryPurchaseRequisition(Guid id, CreateInventoryPurchaseRequisition request, Guid userId);
    Task<Result> DeleteInventoryPurchaseRequisition(Guid id, Guid userId);
    Task<Result<InventoryPurchaseRequisitionDto>> GetInventoryPurchaseRequisition(Guid id);
    Task<Result<Paginateable<IEnumerable<InventoryPurchaseRequisitionDto>>>> GetInventoryPurchaseRequisitions(int page,
        int pageSize, string searchQuery, InventoryPurchaseRequisitionStatus? status, IReadOnlyCollection<Store> readableStores, Store? store);
    Task<List<Store>> PurchaseRequisitionStores(Guid id);

    // Sourcing Logic
    Task<Result> CreateSourceRequisition(CreateSourceInventoryRequisition request, Guid userId);
    Task<Result> CreateMarketRequisition(List<CreateMarketRequisition> request, Guid userId);
    Task<Result<Paginateable<IEnumerable<MarketRequisitionDto>>>> GetMarketRequisitions(int page, int pageSize, bool? closed = null);
    Task<Result<List<VendorPriceComparison>>> GetPriceComparisonOfItem(InventoryRequisitionSource source);

    // Memo Creation Logic
    Task<Result> ProcessOpenMarketMemo(List<CreateMemoItem> memos, Guid userId);
    Task<Result> ProcessTrustedVendorMemo(List<CreateMemoItem> memos, Guid userId);

    // Trusted Vendor Specific
    Task<Result> SendQuotationToVendor(Guid vendorId);
    Task<Result<Paginateable<IEnumerable<VendorQuotationDto>>>> GetVendorQuotations(int page, int pageSize, bool received);
    Task<Result> ReceiveQuotationFromVendor(List<VendorQuotationResponseDto> vendorQuotationResponse, Guid vendorQuotationId, Guid userId);
    Task<Result<VendorQuotationDto>> GetVendorQuotation(Guid vendorQuotationId);

    // Open Market Specific
    Task<Result<Paginateable<IEnumerable<MarketRequisitionVendorDto>>>> GetMarketRequisitionVendors(int page, int pageSize, bool complete, Guid? marketRequisitionId = null);
    Task<Result> CreateMarketRequisitionVendor(CreateMarketRequisitionVendor request);
    Task<Result> ConfirmMarketRequisitionVendor(Guid marketRequisitionVendorId);
    Task<Result<Paginateable<IEnumerable<MemoDto>>>> GetMemos(int page, int pageSize,
         string searchQuery = null,
         MemoStatus? status = null);
    Task<Result> MarkMemoItemAsPaid(Guid memoItemId, Guid userId, DateTime? purchasedAt = null);

    Task<Result> ApproveItem(Guid stockEntryId);
    Task<Result> RejectItem(Guid stockEntryId);

    Task<Result<Paginateable<IEnumerable<StockEntryDto>>>> GetStockEntries(ApprovalStatus status, int page,
        int pageSize, Store store);
    Task<Result> UploadStockItems(ImportItemsRequest itemsRequest);
    Task<Result<MemoDto>> GetMemo(Guid id);

    // Helper methods
    Task<string> GenerateMemoCode();
    Task<Result<MarketRequisitionDto>> GetMarketRequisition(Guid id);
}