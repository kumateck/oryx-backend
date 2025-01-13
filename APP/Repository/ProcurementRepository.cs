using APP.Extensions;
using APP.IRepository;
using APP.Services.Email;
using APP.Services.Pdf;
using APP.Utils;
using AutoMapper;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;
using DOMAIN.Entities.Procurement.Manufacturers;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.PurchaseOrders.Request;
using DOMAIN.Entities.Shipments;
using DOMAIN.Entities.Shipments.Request;

namespace APP.Repository;

public class ProcurementRepository(ApplicationDbContext context, IMapper mapper, IEmailService emailService, IPdfService pdfService) : IProcurementRepository
{
    // ************* CRUD for Manufacturer *************

    public async Task<Result<Guid>> CreateManufacturer(CreateManufacturerRequest request, Guid userId)
    {
        var manufacturer = mapper.Map<Manufacturer>(request);
        manufacturer.CreatedById = userId;
        await context.Manufacturers.AddAsync(manufacturer);
        await context.SaveChangesAsync();

        return manufacturer.Id;
    }

    public async Task<Result<ManufacturerDto>> GetManufacturer(Guid manufacturerId)
    {
        var manufacturer = await context.Manufacturers
            .Include(m => m.Country)
            .Include(m => m.Materials)
            .FirstOrDefaultAsync(m => m.Id == manufacturerId);

        return manufacturer is null
            ? Error.NotFound("Manufacturer.NotFound", "Manufacturer not found")
            : mapper.Map<ManufacturerDto>(manufacturer);
    }
    
    public async Task<Result<Paginateable<IEnumerable<ManufacturerDto>>>> GetManufacturers(int page, int pageSize, string searchQuery)
    {
        var query = context.Manufacturers
            .Include(m => m.Country)
            .Include(m => m.Materials)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, m => m.Name, m => m.Address);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<ManufacturerDto>
        );
    }
    
    public async Task<Result<List<ManufacturerDto>>> GetManufacturersByMaterial(Guid materialId)
    {
       return mapper.Map<List<ManufacturerDto>>( await context.Manufacturers
            .Include(m => m.Materials).ThenInclude(m => m.Material)
            .Where(m => m.Materials.Any(ma => ma.MaterialId == materialId))
            .ToListAsync());
    }
    
    public async Task<Result> UpdateManufacturer(CreateManufacturerRequest request, Guid manufacturerId, Guid userId)
    {
        var existingManufacturer = await context.Manufacturers.Include(manufacturer => manufacturer.Materials).FirstOrDefaultAsync(m => m.Id == manufacturerId);
        if (existingManufacturer is null)
        {
            return Error.NotFound("Manufacturer.NotFound", "Manufacturer not found");
        }

        context.ManufacturerMaterials.RemoveRange(existingManufacturer.Materials);
        mapper.Map(request, existingManufacturer);
        existingManufacturer.LastUpdatedById = userId;
        context.Manufacturers.Update(existingManufacturer);
        await context.ManufacturerMaterials.AddRangeAsync(existingManufacturer.Materials);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    // Delete Manufacturer (soft delete)
    public async Task<Result> DeleteManufacturer(Guid manufacturerId, Guid userId)
    {
        var manufacturer = await context.Manufacturers.FirstOrDefaultAsync(m => m.Id == manufacturerId);
        if (manufacturer is null)
        {
            return Error.NotFound("Manufacturer.NotFound", "Manufacturer not found");
        }

        manufacturer.DeletedAt = DateTime.UtcNow;
        manufacturer.LastDeletedById = userId;

        context.Manufacturers.Update(manufacturer);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    // ************* CRUD for Supplier *************

    public async Task<Result<Guid>> CreateSupplier(CreateSupplierRequest request, Guid userId)
    {
        var existingSupplier = await context.Suppliers.FirstOrDefaultAsync(s => s.Name == request.Name);
        if(existingSupplier is not null) return Error.Validation("Supplier.Name", $"Supplier with name {request.Name} already exists");
        
        var supplier = mapper.Map<Supplier>(request);
        supplier.CreatedById = userId;
        await context.Suppliers.AddAsync(supplier);
        await context.SaveChangesAsync();

        return supplier.Id;
    }

    public async Task<Result<SupplierDto>> GetSupplier(Guid supplierId)
    {
        var supplier = await context.Suppliers
            .Include(s => s.AssociatedManufacturers).ThenInclude(sm => sm.Manufacturer)
            .Include(s => s.AssociatedManufacturers).ThenInclude(sm => sm.Material)
            .FirstOrDefaultAsync(s => s.Id == supplierId);

        return supplier is null
            ? Error.NotFound("Supplier.NotFound", "Supplier not found")
            : mapper.Map<SupplierDto>(supplier);
    }
    
    public async Task<Result<Paginateable<IEnumerable<SupplierDto>>>> GetSuppliers(int page, int pageSize, string searchQuery)
    {
        var query = context.Suppliers
            .Include(s => s.AssociatedManufacturers).ThenInclude(sm => sm.Manufacturer)
            .Include(s => s.AssociatedManufacturers).ThenInclude(sm => sm.Material)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, s => s.Name, s => s.ContactPerson);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<SupplierDto>
        );
    }
    public async Task<Result<IEnumerable<SupplierDto>>> GetSupplierByMaterial(Guid materialId)
    {
        return mapper.Map<List<SupplierDto>>( await context.Suppliers
            .Include(s => s.AssociatedManufacturers).ThenInclude(sm => sm.Manufacturer)
            .Include(s => s.AssociatedManufacturers).ThenInclude(sm => sm.Material)
            
            .Where(m => m.AssociatedManufacturers.Select(ma => ma.MaterialId).Contains(materialId))
            .ToListAsync());
    }
    
    public async Task<Result<IEnumerable<SupplierDto>>> GetSupplierByMaterialAndType(Guid materialId, SupplierType type)
    {
        return mapper.Map<List<SupplierDto>>( await context.Suppliers
            .Include(s => s.AssociatedManufacturers).ThenInclude(sm => sm.Manufacturer)
            .Include(s => s.AssociatedManufacturers).ThenInclude(sm => sm.Material)
            .Where(m => m.AssociatedManufacturers.Select(ma => ma.MaterialId).Contains(materialId) && m.Type == type)
            .ToListAsync());
    }

    public async Task<Result> UpdateSupplier(CreateSupplierRequest request, Guid supplierId, Guid userId)
    {
        var existingSupplier = await context.Suppliers.FirstOrDefaultAsync(s => s.Id == supplierId);
        if (existingSupplier is null)
        {
            return Error.NotFound("Supplier.NotFound", "Supplier not found");
        }

        mapper.Map(request, existingSupplier);
        existingSupplier.LastUpdatedById = userId;

        context.Suppliers.Update(existingSupplier);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteSupplier(Guid supplierId, Guid userId)
    {
        var supplier = await context.Suppliers.FirstOrDefaultAsync(s => s.Id == supplierId);
        if (supplier is null)
        {
            return Error.NotFound("Supplier.NotFound", "Supplier not found");
        }

        supplier.DeletedAt = DateTime.UtcNow;
        supplier.LastDeletedById = userId;

        context.Suppliers.Update(supplier);
        await context.SaveChangesAsync();
        return Result.Success();
    }
    
    // ************* CRUD for PurchaseOrder *************

    public async Task<Result<Guid>> CreatePurchaseOrder(CreatePurchaseOrderRequest request, Guid userId)
    {
        var purchaseOrder = mapper.Map<PurchaseOrder>(request);
        purchaseOrder.CreatedById = userId;
        await context.PurchaseOrders.AddAsync(purchaseOrder);
        await context.SaveChangesAsync();

        return purchaseOrder.Id;
    }

    public async Task<Result<PurchaseOrderDto>> GetPurchaseOrder(Guid purchaseOrderId)
    {
        var purchaseOrder = await context.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.Items).ThenInclude(i => i.Material)
            .Include(po => po.Items).ThenInclude(i => i.UoM)
            .FirstOrDefaultAsync(po => po.Id == purchaseOrderId);
        
        if (purchaseOrder is null)
            return Error.NotFound("PurchaseOrder.NotFound", "Purchase order not found");
        
        var result =  mapper.Map<PurchaseOrderDto>(purchaseOrder, opt => opt.Items[AppConstants.ModelType] = nameof(PurchaseOrder));
        foreach (var item in result.Items)
        {
            if (item.Material?.Id != null)
                item.Manufacturers = (await GetManufacturersByMaterial(item.Material.Id.Value)).Value;
        }
        return result;
    }

    public async Task<Result<Paginateable<IEnumerable<PurchaseOrderDto>>>> GetPurchaseOrders(int page, int pageSize, string searchQuery, PurchaseOrderStatus? status)
    {
        var query = context.PurchaseOrders
            .Include(po => po.Supplier)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, po => po.Code);
        }

        if (status.HasValue)
        {
            query = query.Where(po => po.Status == status);
        }
        
        var paginatedResult = await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize);
        var purchaseOrders = await paginatedResult.Data.ToListAsync();
        
        return new Paginateable<IEnumerable<PurchaseOrderDto>>
        {
            Data = mapper.Map<IEnumerable<PurchaseOrderDto>>(purchaseOrders, 
                opt => opt.Items[AppConstants.ModelType] = nameof(PurchaseOrder)),
            PageIndex = page,
            PageCount = paginatedResult.PageCount,
            TotalRecordCount = paginatedResult.TotalRecordCount,
            StartPageIndex = paginatedResult.StartPageIndex,
            StopPageIndex = paginatedResult.StopPageIndex
        };
    }

    public async Task<Result> UpdatePurchaseOrder(CreatePurchaseOrderRequest request, Guid purchaseOrderId, Guid userId)
    {
        var existingOrder = await context.PurchaseOrders.FirstOrDefaultAsync(po => po.Id == purchaseOrderId);
        if (existingOrder is null)
        {
            return Error.NotFound("PurchaseOrder.NotFound", "Purchase order not found");
        }

        mapper.Map(request, existingOrder);
        existingOrder.LastUpdatedById = userId;

        context.PurchaseOrders.Update(existingOrder);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeletePurchaseOrder(Guid purchaseOrderId, Guid userId)
    {
        var purchaseOrder = await context.PurchaseOrders.FirstOrDefaultAsync(po => po.Id == purchaseOrderId);
        if (purchaseOrder is null)
        {
            return Error.NotFound("PurchaseOrder.NotFound", "Purchase order not found");
        }

        purchaseOrder.DeletedAt = DateTime.UtcNow;
        purchaseOrder.LastDeletedById = userId;

        context.PurchaseOrders.Update(purchaseOrder);
        await context.SaveChangesAsync();
        return Result.Success();
    }
    
    public async Task<Result> SendPurchaseOrderToSupplier(SendPurchaseOrderRequest request, Guid purchaseOrderId)
    {
        var purchaseOrder =  await context.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.Items).ThenInclude(i => i.Material)
            .Include(po => po.Items).ThenInclude(i => i.UoM)
            .FirstOrDefaultAsync(po => po.Id == purchaseOrderId);
        
        if (purchaseOrder is null) return Error.NotFound("PurchaseOrder.NotFound", "Purchase order not found");
        purchaseOrder.ExpectedDeliveryDate = request.ExpectedDeliveryDate;
        purchaseOrder.Status = PurchaseOrderStatus.Completed;
        context.PurchaseOrders.Update(purchaseOrder);
        await context.SaveChangesAsync();
        
        var mailAttachments = new List<(byte[] fileContent, string fileName, string fileType)>();
        var fileContent = pdfService.GeneratePdfFromHtml(PdfTemplate.PurchaseOrderTemplate(purchaseOrder));
        mailAttachments.Add((fileContent, $"Purchase Order from Entrance",  "application/pdf"));

        try
        {
            emailService.SendMail(purchaseOrder.Supplier.Email, "Purchase Order From Entrance", "Please find attached to this email your final awarded quotation to draft a purchase order.", mailAttachments);
        }
        catch (Exception e)
        {
            return Error.Validation("Supplier.Quotation", e.Message);
        }
        
        return Result.Success();
    }
    
    public async Task<Result> SendProformaInvoiceToSupplier(Guid purchaseOrderId)
    {
        var purchaseOrder =  await context.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.Items).ThenInclude(i => i.Material)
            .Include(po => po.Items).ThenInclude(i => i.UoM)
            .FirstOrDefaultAsync(po => po.Id == purchaseOrderId);
        
        if (purchaseOrder is null) return Error.NotFound("PurchaseOrder.NotFound", "Purchase order not found");
        
        var mailAttachments = new List<(byte[] fileContent, string fileName, string fileType)>();
        var fileContent = pdfService.GeneratePdfFromHtml(PdfTemplate.ProformaInvoiceTemplate(purchaseOrder));
        mailAttachments.Add((fileContent, $"Proforma Invoice from Entrance",  "application/pdf"));

        try
        {
            emailService.SendMail(purchaseOrder.Supplier.Email, "Proforma Invoice From Entrance", "Please find attached a proforma invoice.", mailAttachments);
        }
        catch (Exception e)
        {
            return Error.Validation("Supplier.Quotation", e.Message);
        }
        
        purchaseOrder.Status = PurchaseOrderStatus.Delivered;
        context.PurchaseOrders.Update(purchaseOrder);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    // ************* CRUD for PurchaseOrderInvoice *************

    public async Task<Result<Guid>> CreatePurchaseOrderInvoice(CreatePurchaseOrderInvoiceRequest request, Guid userId)
    {
        var invoice = mapper.Map<PurchaseOrderInvoice>(request);
        invoice.CreatedById = userId;
        await context.PurchaseOrderInvoices.AddAsync(invoice);
        await context.SaveChangesAsync();

        return invoice.Id;
    }

    public async Task<Result<PurchaseOrderInvoiceDto>> GetPurchaseOrderInvoice(Guid invoiceId)
    {
        var invoice = await context.PurchaseOrderInvoices
            .Include(poi => poi.PurchaseOrder).ThenInclude(po => po.Supplier)
            .Include(poi => poi.BatchItems).ThenInclude(bi => bi.Manufacturer)
            .Include(poi => poi.Charges).ThenInclude(c => c.Currency)
            .FirstOrDefaultAsync(poi => poi.Id == invoiceId);

        return invoice is null
            ? Error.NotFound("PurchaseOrderInvoice.NotFound", "Invoice not found")
            : mapper.Map<PurchaseOrderInvoiceDto>(invoice);
    }
    
    public async Task<Result<Paginateable<IEnumerable<PurchaseOrderInvoiceDto>>>> GetPurchaseOrderInvoices(int page, int pageSize, string searchQuery)
    {
        var query = context.PurchaseOrderInvoices
            .Include(poi => poi.PurchaseOrder).ThenInclude(po => po.Supplier)
            .Include(poi => poi.BatchItems).ThenInclude(bi => bi.Manufacturer)
            .Include(poi => poi.Charges).ThenInclude(c => c.Currency)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, poi => poi.Code);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<PurchaseOrderInvoiceDto>
        );
    }

    public async Task<Result> UpdatePurchaseOrderInvoice(CreatePurchaseOrderInvoiceRequest request, Guid invoiceId, Guid userId)
    {
        var existingInvoice = await context.PurchaseOrderInvoices.FirstOrDefaultAsync(poi => poi.Id == invoiceId);
        if (existingInvoice is null)
        {
            return Error.NotFound("PurchaseOrderInvoice.NotFound", "Invoice not found");
        }

        mapper.Map(request, existingInvoice);
        existingInvoice.LastUpdatedById = userId;

        context.PurchaseOrderInvoices.Update(existingInvoice);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeletePurchaseOrderInvoice(Guid invoiceId, Guid userId)
    {
        var invoice = await context.PurchaseOrderInvoices.FirstOrDefaultAsync(poi => poi.Id == invoiceId);
        if (invoice is null)
        {
            return Error.NotFound("PurchaseOrderInvoice.NotFound", "Invoice not found");
        }

        invoice.DeletedAt = DateTime.UtcNow;
        invoice.LastDeletedById = userId;

        context.PurchaseOrderInvoices.Update(invoice);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    // ************* CRUD for BillingSheet *************

    public async Task<Result<Guid>> CreateBillingSheet(CreateBillingSheetRequest request, Guid userId)
    {
        var billingSheet = mapper.Map<BillingSheet>(request);
        billingSheet.CreatedById = userId;
        await context.BillingSheets.AddAsync(billingSheet);
        await context.SaveChangesAsync();

        return billingSheet.Id;
    }

    public async Task<Result<BillingSheetDto>> GetBillingSheet(Guid billingSheetId)
    {
        var billingSheet = await context.BillingSheets
            .Include(bs => bs.Supplier)
            .Include(bs => bs.Invoice).ThenInclude(poi => poi.PurchaseOrder)
            .FirstOrDefaultAsync(bs => bs.Id == billingSheetId);

        return billingSheet is null
            ? Error.NotFound("BillingSheet.NotFound", "Billing sheet not found")
            : mapper.Map<BillingSheetDto>(billingSheet);
    }
    
    public async Task<Result<Paginateable<IEnumerable<BillingSheetDto>>>> GetBillingSheets(int page, int pageSize, string searchQuery)
    {
        var query = context.BillingSheets
            .Include(bs => bs.Supplier)
            .Include(bs => bs.Invoice).ThenInclude(poi => poi.PurchaseOrder)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, bs => bs.Code, bs => bs.BillOfLading);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<BillingSheetDto>
        );
    }

    public async Task<Result> UpdateBillingSheet(CreateBillingSheetRequest request, Guid billingSheetId, Guid userId)
    {
        var existingBillingSheet = await context.BillingSheets.FirstOrDefaultAsync(bs => bs.Id == billingSheetId);
        if (existingBillingSheet is null)
        {
            return Error.NotFound("BillingSheet.NotFound", "Billing sheet not found");
        }

        mapper.Map(request, existingBillingSheet);
        existingBillingSheet.LastUpdatedById = userId;

        context.BillingSheets.Update(existingBillingSheet);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteBillingSheet(Guid billingSheetId, Guid userId)
    {
        var billingSheet = await context.BillingSheets.FirstOrDefaultAsync(bs => bs.Id == billingSheetId);
        if (billingSheet is null)
        {
            return Error.NotFound("BillingSheet.NotFound", "Billing sheet not found");
        }

        billingSheet.DeletedAt = DateTime.UtcNow;
        billingSheet.LastDeletedById = userId;

        context.BillingSheets.Update(billingSheet);
        await context.SaveChangesAsync();
        return Result.Success();
    }
    
     public async Task<Result<Guid>> CreateShipmentDocument(CreateShipmentDocumentRequest request, Guid userId)
    {
        var shipmentDocument = mapper.Map<ShipmentDocument>(request);
        shipmentDocument.CreatedById = userId;
        await context.ShipmentDocuments.AddAsync(shipmentDocument);
        await context.SaveChangesAsync();

        return shipmentDocument.Id;
    }

    public async Task<Result<ShipmentDocumentDto>> GetShipmentDocument(Guid shipmentDocumentId)
    {
        var shipmentDocument = await context.ShipmentDocuments
            .Include(s => s.PurchaseOrder)
            .FirstOrDefaultAsync(bs => bs.Id == shipmentDocumentId);

        return shipmentDocument is null
            ? Error.NotFound("BillingSheet.NotFound", "Billing sheet not found")
            : mapper.Map<ShipmentDocumentDto>(shipmentDocument, opt => opt.Items[AppConstants.ModelType] = nameof(ShipmentDocument));
    }
    
    public async Task<Result<Paginateable<IEnumerable<ShipmentDocumentDto>>>> GetShipmentDocuments(int page, int pageSize, string searchQuery)
    {
        var query = context.ShipmentDocuments
            .Include(s => s.PurchaseOrder)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, bs => bs.Code, bs => bs.InvoiceNumber);
        }
        
        var paginatedResult = await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize);
        var shipmentDocuments = await paginatedResult.Data.ToListAsync();
        
        return new Paginateable<IEnumerable<ShipmentDocumentDto>>
        {
            Data = mapper.Map<IEnumerable<ShipmentDocumentDto>>(shipmentDocuments, 
                opt => opt.Items[AppConstants.ModelType] = nameof(ShipmentDocument)),
            PageIndex = page,
            PageCount = paginatedResult.PageCount,
            TotalRecordCount = paginatedResult.TotalRecordCount,
            StartPageIndex = paginatedResult.StartPageIndex,
            StopPageIndex = paginatedResult.StopPageIndex
        };
    }

    public async Task<Result> UpdateShipmentDocument(CreateShipmentDocumentRequest request, Guid shipmentDocumentId, Guid userId)
    {
        var existingShipmentDocument = await context.ShipmentDocuments.FirstOrDefaultAsync(bs => bs.Id == shipmentDocumentId);
        if (existingShipmentDocument is null)
        {
            return Error.NotFound("ShipmentDocument.NotFound", "Shipment document not found");
        }

        mapper.Map(request, existingShipmentDocument);
        existingShipmentDocument.LastUpdatedById = userId;

        context.ShipmentDocuments.Update(existingShipmentDocument);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteShipmentDocument(Guid shipmentDocumentId, Guid userId)
    {
        var shipmentDocument = await context.ShipmentDocuments.FirstOrDefaultAsync(bs => bs.Id == shipmentDocumentId);
        if (shipmentDocument is null)
        {
            return Error.NotFound("ShipmentDocument.NotFound", "Shipment document not found");
        }

        shipmentDocument.DeletedAt = DateTime.UtcNow;
        shipmentDocument.LastDeletedById = userId;

        context.ShipmentDocuments.Update(shipmentDocument);
        await context.SaveChangesAsync();
        return Result.Success();
    }
    
    public async Task<Result<Guid>> CreateShipmentInvoice(CreateShipmentInvoice request, Guid userId)
    {
        var shipmentInvoice = mapper.Map<ShipmentInvoice>(request);
        shipmentInvoice.CreatedById = userId;
        await context.ShipmentInvoices.AddAsync(shipmentInvoice);
        await context.SaveChangesAsync();

        return shipmentInvoice.Id;
    }

    public async Task<Result<Guid>> CreateShipmentDiscrepancy(CreateShipmentDiscrepancy request, Guid userId)
    {
        var shipmentDiscrepancy = mapper.Map<ShipmentDiscrepancy>(request);
        shipmentDiscrepancy.CreatedById = userId;
        await context.ShipmentDiscrepancies.AddAsync(shipmentDiscrepancy);
        await context.SaveChangesAsync();

        return shipmentDiscrepancy.Id;
    }

    // Read operations for ShipmentInvoice and ShipmentDiscrepancy
    public async Task<Result<ShipmentInvoiceDto>> GetShipmentInvoice(Guid shipmentInvoiceId)
    {
        var shipmentInvoice = await context.ShipmentInvoices
            .Include(si => si.ShipmentDocument)
            .Include(si => si.Items)
                .ThenInclude(item => item.Material)
            .Include(si => si.Items)
                .ThenInclude(item => item.UoM)
            .FirstOrDefaultAsync(si => si.ShipmentDocumentId == shipmentInvoiceId);

        return shipmentInvoice is null
            ? Error.NotFound("ShipmentInvoice.NotFound", "Shipment invoice not found")
            : mapper.Map<ShipmentInvoiceDto>(shipmentInvoice);
    }

    public async Task<Result<ShipmentDiscrepancyDto>> GetShipmentDiscrepancy(Guid shipmentDiscrepancyId)
    {
        var shipmentDiscrepancy = await context.ShipmentDiscrepancies
            .Include(sd => sd.ShipmentDocument)
            .Include(sd => sd.Items)
                .ThenInclude(item => item.Material)
            .Include(sd => sd.Items)
                .ThenInclude(item => item.UoM)
            .FirstOrDefaultAsync(sd => sd.Id == shipmentDiscrepancyId);

        return shipmentDiscrepancy is null
            ? Error.NotFound("ShipmentDiscrepancy.NotFound", "Shipment discrepancy not found")
            : mapper.Map<ShipmentDiscrepancyDto>(shipmentDiscrepancy);
    }

    // Update operations for ShipmentInvoice and ShipmentDiscrepancy
    public async Task<Result> UpdateShipmentInvoice(CreateShipmentInvoice request, Guid shipmentInvoiceId, Guid userId)
    {
        var existingShipmentInvoice = await context.ShipmentInvoices
            .Include(si => si.Items)
            .FirstOrDefaultAsync(si => si.Id == shipmentInvoiceId);
        if (existingShipmentInvoice is null)
        {
            return Error.NotFound("ShipmentInvoice.NotFound", "Shipment invoice not found");
        }

        mapper.Map(request, existingShipmentInvoice);
        existingShipmentInvoice.LastUpdatedById = userId;

        context.ShipmentInvoices.Update(existingShipmentInvoice);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> UpdateShipmentDiscrepancy(CreateShipmentDiscrepancy request, Guid shipmentDiscrepancyId, Guid userId)
    {
        var existingShipmentDiscrepancy = await context.ShipmentDiscrepancies
            .Include(sd => sd.Items)
            .FirstOrDefaultAsync(sd => sd.Id == shipmentDiscrepancyId);
        if (existingShipmentDiscrepancy is null)
        {
            return Error.NotFound("ShipmentDiscrepancy.NotFound", "Shipment discrepancy not found");
        }

        mapper.Map(request, existingShipmentDiscrepancy);
        existingShipmentDiscrepancy.LastUpdatedById = userId;

        context.ShipmentDiscrepancies.Update(existingShipmentDiscrepancy);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    // Delete operations for ShipmentInvoice and ShipmentDiscrepancy
    public async Task<Result> DeleteShipmentInvoice(Guid shipmentInvoiceId, Guid userId)
    {
        var shipmentInvoice = await context.ShipmentInvoices.FirstOrDefaultAsync(si => si.Id == shipmentInvoiceId);
        if (shipmentInvoice is null)
        {
            return Error.NotFound("ShipmentInvoice.NotFound", "Shipment invoice not found");
        }

        shipmentInvoice.DeletedAt = DateTime.UtcNow;
        shipmentInvoice.LastDeletedById = userId;

        context.ShipmentInvoices.Update(shipmentInvoice);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteShipmentDiscrepancy(Guid shipmentDiscrepancyId, Guid userId)
    {
        var shipmentDiscrepancy = await context.ShipmentDiscrepancies.FirstOrDefaultAsync(sd => sd.Id == shipmentDiscrepancyId);
        if (shipmentDiscrepancy is null)
        {
            return Error.NotFound("ShipmentDiscrepancy.NotFound", "Shipment discrepancy not found");
        }

        shipmentDiscrepancy.DeletedAt = DateTime.UtcNow;
        shipmentDiscrepancy.LastDeletedById = userId;

        context.ShipmentDiscrepancies.Update(shipmentDiscrepancy);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}
