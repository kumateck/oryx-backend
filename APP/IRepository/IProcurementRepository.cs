using SHARED;
using APP.Utils;
using DOMAIN.Entities.Procurement.Manufacturers;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.PurchaseOrders.Request;
using DOMAIN.Entities.Shipments;
using DOMAIN.Entities.Shipments.Request;

namespace APP.IRepository
{
    public interface IProcurementRepository
    {
        // Manufacturer CRUD methods
        Task<Result<Guid>> CreateManufacturer(CreateManufacturerRequest request, Guid userId);
        Task<Result<ManufacturerDto>> GetManufacturer(Guid manufacturerId);
        Task<Result<Paginateable<IEnumerable<ManufacturerDto>>>> GetManufacturers(int page, int pageSize, string searchQuery);
        Task<Result<IEnumerable<ManufacturerDto>>> GetManufacturersByMaterial(Guid materialId);
        Task<Result> UpdateManufacturer(CreateManufacturerRequest request, Guid manufacturerId, Guid userId);
        Task<Result> DeleteManufacturer(Guid manufacturerId, Guid userId);

        // Supplier CRUD methods
        Task<Result<Guid>> CreateSupplier(CreateSupplierRequest request, Guid userId);
        Task<Result<SupplierDto>> GetSupplier(Guid supplierId);
        Task<Result<Paginateable<IEnumerable<SupplierDto>>>> GetSuppliers(int page, int pageSize, string searchQuery);
        Task<Result<IEnumerable<SupplierDto>>> GetSupplierByMaterial(Guid materialId);
        Task<Result<IEnumerable<SupplierDto>>> GetSupplierByMaterialAndType(Guid materialId,
            SupplierType type);
        Task<Result> UpdateSupplier(CreateSupplierRequest request, Guid supplierId, Guid userId);
        Task<Result> DeleteSupplier(Guid supplierId, Guid userId);
        
        // ************* PurchaseOrder *************
        Task<Result<Guid>> CreatePurchaseOrder(CreatePurchaseOrderRequest request, Guid userId);
        Task<Result<PurchaseOrderDto>> GetPurchaseOrder(Guid purchaseOrderId);
        Task<Result<Paginateable<IEnumerable<PurchaseOrderDto>>>> GetPurchaseOrders(int page, int pageSize, string searchQuery, PurchaseOrderStatus? status);
        Task<Result> UpdatePurchaseOrder(CreatePurchaseOrderRequest request, Guid purchaseOrderId, Guid userId);
        Task<Result> DeletePurchaseOrder(Guid purchaseOrderId, Guid userId);

        // ************* PurchaseOrderInvoice *************
        Task<Result<Guid>> CreatePurchaseOrderInvoice(CreatePurchaseOrderInvoiceRequest request, Guid userId);
        Task<Result<PurchaseOrderInvoiceDto>> GetPurchaseOrderInvoice(Guid invoiceId);
        Task<Result<Paginateable<IEnumerable<PurchaseOrderInvoiceDto>>>> GetPurchaseOrderInvoices(int page,
            int pageSize, string searchQuery);
        Task<Result> SendPurchaseOrderToSupplier(SendPurchaseOrderRequest request, Guid purchaseOrderId);
        Task<Result> SendProformaInvoiceToSupplier(Guid purchaseOrderId);
        Task<Result> UpdatePurchaseOrderInvoice(CreatePurchaseOrderInvoiceRequest request, Guid invoiceId, Guid userId);
        Task<Result> DeletePurchaseOrderInvoice(Guid invoiceId, Guid userId);

        // ************* BillingSheet *************
        Task<Result<Guid>> CreateBillingSheet(CreateBillingSheetRequest request, Guid userId);
        Task<Result<BillingSheetDto>> GetBillingSheet(Guid billingSheetId);
        Task<Result<Paginateable<IEnumerable<BillingSheetDto>>>> GetBillingSheets(int page, int pageSize,
            string searchQuery);
        Task<Result> UpdateBillingSheet(CreateBillingSheetRequest request, Guid billingSheetId, Guid userId);
        Task<Result> DeleteBillingSheet(Guid billingSheetId, Guid userId);
        
        //************* Shipment Document ************
        Task<Result<Guid>> CreateShipmentDocument(CreateShipmentDocumentRequest request, Guid userId);
        Task<Result<ShipmentDocumentDto>> GetShipmentDocument(Guid shipmentDocumentId);
        Task<Result<Paginateable<IEnumerable<ShipmentDocumentDto>>>> GetShipmentDocuments(int page,
            int pageSize, string searchQuery);
        Task<Result> UpdateShipmentDocument(CreateShipmentDocumentRequest request, Guid shipmentDocumentId,
            Guid userId);
        Task<Result> DeleteShipmentDocument(Guid shipmentDocumentId, Guid userId);
    }
}