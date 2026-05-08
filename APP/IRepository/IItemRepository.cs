using APP.Utils;
using DOMAIN.Entities.ItemShipments;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.ItemTransactionLogs;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.PurchaseOrders.Request;
using DOMAIN.Entities.Shipments;
using SHARED;

namespace APP.IRepository;

public interface IItemRepository
{
    Task<Result<Guid>> CreateItem(CreateItemsRequest request);
    Task<Result> UploadItems(ImportItemsRequest itemFile);
    Task<Result<Paginateable<IEnumerable<ItemDto>>>> GetItems(int page, int pageSize, string searchQuery, Store? store);
    Task<Result<ItemDto>> GetItem(Guid id);
    Task<Result> UpdateItem(Guid id, CreateItemsRequest request);
    Task<Result> DeleteItem(Guid id, Guid userId);
    Task<Result<List<ItemTransactionLogDto>>> GetItemTransactions(string itemCode, string transactionType);

    // Item Shipment Invoice
    Task<Result<Guid>> CreateItemShipmentInvoice(CreateItemShipmentInvoice request, Guid userId);
    Task<Result<ItemShipmentInvoiceDto>> GetItemShipmentInvoice(Guid invoiceId);
    Task<Result<Paginateable<IEnumerable<ItemShipmentInvoiceDto>>>> GetItemShipmentInvoices(int page, int pageSize, string searchQuery);
    Task<Result<IEnumerable<ItemShipmentInvoiceDto>>> GetUnattachedItemShipmentInvoices();
    Task<Result> UpdateItemShipmentInvoice(CreateItemShipmentInvoice request, Guid invoiceId, Guid userId);
    Task<Result> MarkItemShipmentInvoiceAsPaid(Guid invoiceId, DateTime? paidAt, Guid userId);
    Task<Result> DeleteItemShipmentInvoice(Guid invoiceId, Guid userId);

    // Item Shipment Document
    Task<Result<Guid>> CreateItemShipmentDocument(CreateItemShipmentDocumentRequest request, Guid userId);
    Task<Result<ItemShipmentDocumentDto>> GetItemShipmentDocument(Guid shipmentDocumentId);
    Task<Result<Paginateable<IEnumerable<ItemShipmentDocumentDto>>>> GetItemShipmentDocuments(int page, int pageSize, string searchQuery, bool? onlyApproved);
    Task<Result> UpdateItemShipmentDocument(CreateItemShipmentDocumentRequest request, Guid shipmentDocumentId, Guid userId);
    Task<Result> DeleteItemShipmentDocument(Guid shipmentDocumentId, Guid userId);
    Task<Result> MarkItemShipmentAsArrived(Guid shipmentDocumentId, Guid userId);

    // Item Waybill
    Task<Result<Guid>> CreateItemWaybill(CreateItemShipmentDocumentRequest request, Guid userId);
    Task<Result<ItemShipmentDocumentDto>> GetItemWaybill(Guid waybillId);
    Task<Result<Paginateable<IEnumerable<ItemShipmentDocumentDto>>>> GetItemWaybills(int page, int pageSize, string searchQuery, ShipmentStatus? status);
    Task<Result> UpdateItemWaybill(CreateItemShipmentDocumentRequest request, Guid waybillId, Guid userId);
    Task<Result> DeleteItemWaybill(Guid waybillId, Guid userId);

    // Item Billing Sheet
    Task<Result<Guid>> CreateItemBillingSheet(CreateItemBillingSheetRequest request, Guid userId);
    Task<Result<ItemBillingSheetDto>> GetItemBillingSheet(Guid billingSheetId);
    Task<Result<ItemBillingSheetDto>> GetItemBillingSheetByInvoice(Guid invoiceId);
    Task<Result<Paginateable<IEnumerable<ItemBillingSheetDto>>>> GetItemBillingSheets(int page, int pageSize, string searchQuery, BillingSheetStatus? status);
    Task<Result> UpdateItemBillingSheet(UpdateItemBillingSheetRequest request, Guid billingSheetId, Guid userId);
    Task<Result> AddChargesToItemBillingSheet(List<CreateBillingSheetCharge> request, Guid billingSheetId, Guid userId);
    Task<Result> MarkItemBillingSheetChargeAsPaid(MarkBillingSheetCharge request, Guid userId);
    Task<Result> DeleteItemBillingSheet(Guid billingSheetId, Guid userId);
}