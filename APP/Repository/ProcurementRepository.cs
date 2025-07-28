using APP.Extensions;
using APP.IRepository;
using APP.Services.Background;
using APP.Services.Email;
using APP.Services.Pdf;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Notifications;
using DOMAIN.Entities.Procurement.Distribution;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;
using DOMAIN.Entities.Procurement.Manufacturers;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.PurchaseOrders.Request;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Shipments;
using DOMAIN.Entities.Shipments.Request;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;
using Newtonsoft.Json;

namespace APP.Repository;

public class ProcurementRepository(ApplicationDbContext context, IMapper mapper, IEmailService emailService, IPdfService pdfService, IApprovalRepository approvalRepository, IBackgroundWorkerService backgroundWorkerService) : IProcurementRepository
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
            .AsSplitQuery()
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
            .AsSplitQuery()
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
            .AsSplitQuery()
            .Include(m => m.Materials).ThenInclude(m => m.Material)
            .Where(m => m.Materials.Any(ma => ma.MaterialId == materialId))
            .ToListAsync());
    }
    
    public async Task<Result<List<SupplierManufacturerDto>>> GetSupplierManufacturersByMaterial(Guid materialId, Guid supplierId)
    {
        return mapper.Map<List<SupplierManufacturerDto>>(await context.SupplierManufacturers
            .AsSplitQuery()
            .Include(m => m.Manufacturer)
            .Include(m => m.Material)
            .Where(m => m.MaterialId == materialId && m.SupplierId == supplierId)
            .ToListAsync());
    }
    
    public async Task<Result> UpdateManufacturer(CreateManufacturerRequest request, Guid manufacturerId, Guid userId)
    {
        var existingManufacturer = await context.Manufacturers
            .AsSplitQuery()
            .Include(manufacturer => manufacturer.Materials).FirstOrDefaultAsync(m => m.Id == manufacturerId);
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

        if (request.AssociatedManufacturers.GroupBy(m => new { m.ManufacturerId, m.MaterialId })
            .Any(g => g.Count() > 1))
            return Error.Validation("Suppler.Manufactures", "You have duplicate manufacturers supplying the same material");
        
        var supplier = mapper.Map<Supplier>(request);
        supplier.CreatedById = userId;
        await context.Suppliers.AddAsync(supplier);
        await context.SaveChangesAsync();

        return supplier.Id;
    }

    public async Task<Result<SupplierDto>> GetSupplier(Guid supplierId)
    {
        var supplier = await context.Suppliers
            .AsSplitQuery()
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
    
    public async Task<Result> UpdateSupplierStatus(Guid supplierId, SupplierStatus status, Guid userId)
    {
        var supplier = await context.Suppliers.FirstOrDefaultAsync(s => s.Id == supplierId);
        if (supplier is null)
        {
            return Error.NotFound("Supplier.NotFound", "Supplier not found");
        }
    
        supplier.Status = status;
        supplier.LastUpdatedById = userId;
        context.Suppliers.Update(supplier);
        await context.SaveChangesAsync();
        return Result.Success();
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
        var existingSupplier = await context.Suppliers.Include(supplier => supplier.AssociatedManufacturers).FirstOrDefaultAsync(s => s.Id == supplierId);
        if (existingSupplier is null)
        {
            return Error.NotFound("Supplier.NotFound", "Supplier not found");
        }
        
        if (request.AssociatedManufacturers.GroupBy(m => new { m.ManufacturerId, m.MaterialId })
            .Any(g => g.Count() > 1))
            return Error.Validation("Suppler.Manufactures", "You have duplicate manufacturers supplying the same material");

        context.SupplierManufacturers.RemoveRange(existingSupplier.AssociatedManufacturers);
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
        
        await approvalRepository.CreateInitialApprovalsAsync(nameof(PurchaseOrder), purchaseOrder.Id);

        return purchaseOrder.Id;
    }

    public async Task<Result<PurchaseOrderDto>> GetPurchaseOrder(Guid purchaseOrderId)
    {
        var purchaseOrder = await context.PurchaseOrders
            .AsSplitQuery()
            .Include(po => po.Supplier)
            .Include(po => po.Items).ThenInclude(i => i.Material)
            .Include(po => po.Items).ThenInclude(i => i.UoM)
            .Include(po=>po.TermsOfPayment)
            .Include(po=>po.DeliveryMode)
            .FirstOrDefaultAsync(po => po.Id == purchaseOrderId);
        
        if (purchaseOrder is null)
            return Error.NotFound("PurchaseOrder.NotFound", "Purchase order not found");
        
        var result =  mapper.Map<PurchaseOrderDto>(purchaseOrder, opt => opt.Items[AppConstants.ModelType] = nameof(PurchaseOrder));
        foreach (var item in result.Items)
        {
            if (item.Material?.Id != null)
                item.Manufacturers = (await GetSupplierManufacturersByMaterial(item.Material.Id.Value, purchaseOrder.SupplierId)).Value;
        }
        return result;
    }

    public async Task<Result<Paginateable<IEnumerable<PurchaseOrderDto>>>> GetPurchaseOrders(int page, int pageSize, string searchQuery, PurchaseOrderStatus? status, SupplierType? type)
    {
        var query = context.PurchaseOrders
            .AsSplitQuery()
            .Include(po => po.Supplier)
            .Include(po=>po.TermsOfPayment)
            .Include(po=>po.DeliveryMode)
            .AsQueryable();

        if (type.HasValue)
        {
            query = query.Where(po => po.Supplier.Type == type);
        }
        
        if (status.HasValue)
        {
            query = query.Where(po => po.Status == status);
        }

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, po => po.Code);
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

    public async Task<Result> RevisePurchaseOrder(Guid purchaseOrderId, List<CreatePurchaseOrderRevision> revisions)
    {
        var existingOrder = await context.PurchaseOrders
            .AsSplitQuery()
            .Include(p => p.SourceRequisition)
                .ThenInclude(sr => sr.Items)       
            .Include(po => po.RevisedPurchaseOrders)
            .Include(po => po.Items)
            .FirstOrDefaultAsync(po => po.Id == purchaseOrderId);

        if (existingOrder is null)
        {
            return Error.NotFound("PurchaseOrder.NotFound", "Purchase order not found");
        }

        var latestRevisionNumber = existingOrder.RevisedPurchaseOrders.Count != 0
            ? existingOrder.RevisedPurchaseOrders.Max(r => r.RevisionNumber) + 1
            : 1;

        var enrichedRevisions = new List<CreatePurchaseOrderRevision>();

        foreach (var revision in revisions)
        {
            PurchaseOrderItem poItem = null;
            if (revision.PurchaseOrderItemId.HasValue)
            {
                poItem = existingOrder.Items.FirstOrDefault(i => i.Id == revision.PurchaseOrderItemId);
                if (poItem is null)
                    return Error.NotFound("PurchaseOrderItem.NotFound", $"Purchase order with Id: {revision.PurchaseOrderItemId} item not found");
            }

            // Clone revision to fill before-values where needed
            var enrichedRevision = new EnrichedRevision
            {
                Type = revision.Type,
                PurchaseOrderItemId = revision.PurchaseOrderItemId,
                MaterialId = revision.MaterialId,
                UoMId = revision.UoMId,
                Quantity = revision.Quantity,
                Price = revision.Price,
                CurrencyId = revision.CurrencyId,
                RevisionNumber = latestRevisionNumber
            };

            switch (revision.Type)
            {
                case RevisedPurchaseOrderType.ReassignSuppler:
                    // Capture before-values
                    if (poItem is not null)
                    {
                        enrichedRevision.MaterialId = poItem.MaterialId;
                        enrichedRevision.UoMBeforeId = poItem.UoMId;
                        enrichedRevision.QuantityBefore = poItem.Quantity;
                        enrichedRevision.PriceBefore = poItem.Price;
                        enrichedRevision.CurrencyBeforeId = poItem.CurrencyId;
                        
                        await context.SupplierQuotationItems
                            .Where(i => i.Status == SupplierQuotationItemStatus.NotUsed 
                                        && i.MaterialId == poItem.MaterialId
                                        && i.PurchaseOrderId == purchaseOrderId)
                            .ExecuteUpdateAsync(setters =>
                                setters.SetProperty(p => p.Status, SupplierQuotationItemStatus.NotProcessed));
                        var poItemToDelete = await context.PurchaseOrderItems.FirstOrDefaultAsync(i  => i.Id == revision.PurchaseOrderItemId);
                        context.PurchaseOrderItems.Remove(poItemToDelete);
                    }
                    break;

                case RevisedPurchaseOrderType.ChangeSource:
                    if (poItem is not null)
                    {
                        enrichedRevision.MaterialId = poItem.MaterialId;
                        enrichedRevision.UoMBeforeId = poItem.UoMId;
                        enrichedRevision.QuantityBefore = poItem.Quantity;
                        enrichedRevision.PriceBefore = poItem.Price;
                        enrichedRevision.CurrencyBeforeId = poItem.CurrencyId;
                        
                        var requisitionId = existingOrder.SourceRequisition.Items
                            .FirstOrDefault(i => i.MaterialId == poItem.MaterialId)
                            ?.RequisitionId;

                        var requisition = await context.Requisitions
                            .Include(r => r.Items)
                            .FirstOrDefaultAsync(r => r.Id == requisitionId);

                        if (requisition is null)
                            return Error.NotFound("Requisition.NotFound", "Requisition not found");

                        var requisitionItem = requisition.Items.FirstOrDefault(r =>
                            r.MaterialId == poItem.MaterialId &&
                            r.Status == RequestStatus.Sourced);

                        if (requisitionItem is null)
                            return Error.NotFound("Requisition.NotFound", "Requisition Item not found");

                        requisitionItem.Status = RequestStatus.Pending;
                        context.RequisitionItems.Update(requisitionItem);
                        var poItemToDelete = await context.PurchaseOrderItems.FirstOrDefaultAsync(i  => i.Id == revision.PurchaseOrderItemId);
                        context.PurchaseOrderItems.Remove(poItemToDelete);
                    }
                    break;

                case RevisedPurchaseOrderType.AddItem:
                    if (!revision.MaterialId.HasValue || !revision.UoMId.HasValue || !revision.Quantity.HasValue || 
                        !revision.Price.HasValue || !revision.CurrencyId.HasValue)
                    {
                        return Error.Validation("PurchaseOrder.MissingFields", "One or more required fields are missing for AddItem.");
                    }

                    await context.PurchaseOrderItems.AddAsync(new PurchaseOrderItem
                    {
                        PurchaseOrderId = purchaseOrderId,
                        MaterialId = revision.MaterialId.Value,
                        UoMId = revision.UoMId.Value,
                        Quantity = revision.Quantity.Value,
                        Price = revision.Price.Value,
                        CurrencyId = revision.CurrencyId.Value
                    });
                    break;

                case RevisedPurchaseOrderType.UpdateItem:
                    if (!revision.UoMId.HasValue || !revision.Quantity.HasValue || !revision.Price.HasValue)
                    {
                        return Error.Validation("PurchaseOrder.MissingFields", "One or more required fields are missing for UpdateItem.");
                    }

                    if (poItem is not null)
                    {
                        enrichedRevision.UoMBeforeId = poItem.UoMId;
                        enrichedRevision.QuantityBefore = poItem.Quantity;
                        enrichedRevision.PriceBefore = poItem.Price;
                        enrichedRevision.CurrencyBeforeId = poItem.CurrencyId;

                        poItem.UoMId = revision.UoMId.Value;
                        poItem.Quantity = revision.Quantity.Value;
                        poItem.Price = revision.Price.Value;
                        context.PurchaseOrderItems.Update(poItem);
                    }
                    break;

                case RevisedPurchaseOrderType.RemoveItem:
                    if (poItem is not null)
                    {
                        enrichedRevision.MaterialId = poItem.MaterialId;
                        enrichedRevision.UoMBeforeId = poItem.UoMId;
                        enrichedRevision.QuantityBefore = poItem.Quantity;
                        enrichedRevision.PriceBefore = poItem.Price;
                        enrichedRevision.CurrencyBeforeId = poItem.CurrencyId;

                        var poItemToDelete = await context.PurchaseOrderItems.FirstOrDefaultAsync(i  => i.Id == revision.PurchaseOrderItemId);
                        context.PurchaseOrderItems.Remove(poItemToDelete);
                    }
                    break;
            }

            enrichedRevisions.Add(enrichedRevision);
        }

        // Map and persist
        var mappedRevisions = mapper.Map<List<RevisedPurchaseOrder>>(enrichedRevisions);
        foreach (var rev in mappedRevisions)
        {
            rev.RevisionNumber = latestRevisionNumber;
        }
        existingOrder.RevisionNumber = latestRevisionNumber;
        context.PurchaseOrders.Update(existingOrder);
        existingOrder.RevisedPurchaseOrders.AddRange(mappedRevisions);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> UpdatePurchaseOrder(UpdatePurchaseOrderRequest request, Guid purchaseOrderId, Guid userId)
    {
        var existingOrder = await context.PurchaseOrders
            .Include(po => po.RevisedPurchaseOrders)
            .FirstOrDefaultAsync(po => po.Id == purchaseOrderId);
        if (existingOrder is null)
        {
            return Error.NotFound("PurchaseOrder.NotFound", "Purchase order not found");
        }
    
        // var revisedPurchaseOrder = mapper.Map<RevisedPurchaseOrder>(request);
        // revisedPurchaseOrder.CreatedById = userId;
        // existingOrder.RevisedPurchaseOrders.Add(revisedPurchaseOrder);
    
        mapper.Map(request, existingOrder);
        existingOrder.LastUpdatedById = userId;
    
        context.PurchaseOrders.Update(existingOrder);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<Guid>> GetRequisitionIdForPurchaseOrderAndMaterial(Guid purchaseOrderId, Guid materialId)
    {
        var purchaseOrder = await context.PurchaseOrders
            .AsSplitQuery()
            .Include(p => p.SourceRequisition)
            .ThenInclude(s => s.Items)
            .FirstOrDefaultAsync(p => p.Id == purchaseOrderId);
        
        var sourceRequisition = purchaseOrder.SourceRequisition;
        
        return sourceRequisition.Items.Any(i => i.MaterialId == materialId) ? sourceRequisition.Items
            .First(i => i.MaterialId == materialId)
            .RequisitionId : Error.NotFound("Requisition.NotFound", "Could not find a requisition with this material and purchase order");
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
    
    public async Task<Result<Paginateable<IEnumerable<PurchaseOrderInvoiceDto>>>> GetPurchaseOrderInvoices(int page, int pageSize, string searchQuery, SupplierType? type)
    {
        var query = context.PurchaseOrderInvoices
            .Include(poi => poi.PurchaseOrder).ThenInclude(po => po.Supplier)
            .Include(poi => poi.BatchItems).ThenInclude(bi => bi.Manufacturer)
            .Include(poi => poi.Charges).ThenInclude(c => c.Currency)
            .AsQueryable();

        if (type.HasValue)
        {
            query = query.Where(q => q.PurchaseOrder.Supplier.Type == type);
        }

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

        await approvalRepository.CreateInitialApprovalsAsync(nameof(BillingSheet), billingSheet.Id);

        return billingSheet.Id;
    }

    public async Task<Result<BillingSheetDto>> GetBillingSheet(Guid billingSheetId)
    {
        var billingSheet = await context.BillingSheets
            .Include(bs => bs.Supplier)
            .Include(bs => bs.Invoice)
            .ThenInclude(i=>i.Items)
            .ThenInclude(ii=>ii.Material)
            .Include(bs => bs.Invoice)
            .ThenInclude(i=>i.Items)
            .ThenInclude(ii=>ii.Manufacturer)
            .Include(bs => bs.Invoice)
            .ThenInclude(i=>i.Items)
            .ThenInclude(ii=>ii.PurchaseOrder)
            .Include(bs=>bs.Charges)
            .FirstOrDefaultAsync(bs => bs.Id == billingSheetId);

        return billingSheet is null
            ? Error.NotFound("BillingSheet.NotFound", "Billing sheet not found")
            : mapper.Map<BillingSheetDto>(billingSheet);
    }
    
    public async Task<Result<Paginateable<IEnumerable<BillingSheetDto>>>> GetBillingSheets(int page, int pageSize, string searchQuery, BillingSheetStatus? status = null)
    {
        var query = context.BillingSheets
            .Include(bs => bs.Supplier)
            .Include(bs => bs.Invoice)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(q => q.Status == status.Value);
        }

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
        shipmentDocument.Type = DocType.Shipment;
        shipmentDocument.Status = ShipmentStatus.New;
        shipmentDocument.CreatedById = userId;
        await context.ShipmentDocuments.AddAsync(shipmentDocument);
        await context.SaveChangesAsync();

        return shipmentDocument.Id;
    }
     
     public async Task<Result> UpdateShipmentStatus(Guid shipmentId, ShipmentStatus status, Guid userId)
     {
         var shipmentDocument = await context.ShipmentDocuments.FirstOrDefaultAsync(sd => sd.Id == shipmentId);
         if (shipmentDocument is null)
         {
             return Error.NotFound("ShipmentDocument.NotFound", "Shipment document not found");
         }
     
         shipmentDocument.Status = status;
         shipmentDocument.LastUpdatedById = userId;
         shipmentDocument.UpdatedAt = DateTime.UtcNow;
     
         switch (status)
         {
             case ShipmentStatus.Cleared:
                 shipmentDocument.ClearedAt = DateTime.UtcNow;
                 break;
             case ShipmentStatus.InTransit:
                 shipmentDocument.TransitStartedAt = DateTime.UtcNow;
                 break;
             case ShipmentStatus.Arrived:
                 shipmentDocument.ArrivedAt = DateTime.UtcNow;
                 break;
         }
     
         context.ShipmentDocuments.Update(shipmentDocument);
         await context.SaveChangesAsync();
         return Result.Success();
     }

    public async Task<Result<ShipmentDocumentDto>> GetShipmentDocumentV0(Guid shipmentDocumentId)
    {
        var shipmentDocument = await context.ShipmentDocuments
            .AsSplitQuery()
            .Include(s => s.ShipmentInvoice)
            .ThenInclude(s => s.Items)
            .FirstOrDefaultAsync(bs => bs.Id == shipmentDocumentId);

        return shipmentDocument is null
            ? Error.NotFound("ShipmentDocument.NotFound", "Shipment document not found")
            : mapper.Map<ShipmentDocumentDto>(shipmentDocument, opt => opt.Items[AppConstants.ModelType] = nameof(ShipmentDocument));
    }
    
    public async Task<Result<Paginateable<IEnumerable<ShipmentDocumentDto>>>> GetShipmentDocumentsV0(int page, int pageSize, string searchQuery)
    {
        var query = context.ShipmentDocuments
            .Include(s => s.ShipmentInvoice)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, bs => bs.Code);
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

    public async Task<Result> UpdateShipmentDocumentV0(CreateShipmentDocumentRequest request, Guid shipmentDocumentId, Guid userId)
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

    public async Task<Result> DeleteShipmentDocumentV0(Guid shipmentDocumentId, Guid userId)
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
    
    public async Task<Result<ShipmentDocumentDto>> GetShipmentDocument(Guid shipmentDocumentId)
    {
        var shipmentDocument = await context.ShipmentDocuments
            .AsSplitQuery()
            .Include(s => s.ShipmentInvoice)
            .ThenInclude(s => s.Items)
            .FirstOrDefaultAsync(bs => bs.Id == shipmentDocumentId && bs.Type == DocType.Shipment);
    
        return shipmentDocument is null
            ? Error.NotFound("ShipmentDocument.NotFound", "Shipment document not found")
            : mapper.Map<ShipmentDocumentDto>(shipmentDocument, opt => opt.Items[AppConstants.ModelType] = nameof(ShipmentDocument));
    }
    
    public async Task<Result<Paginateable<IEnumerable<ShipmentDocumentDto>>>> GetShipmentDocuments(int page, int pageSize, string searchQuery)
    {
        var query = context.ShipmentDocuments
            .Include(s => s.ShipmentInvoice)
            .Where(s => s.Type == DocType.Shipment)
            .AsQueryable();
    
        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, bs => bs.Code);
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
        var existingShipmentDocument = await context.ShipmentDocuments.FirstOrDefaultAsync(bs => bs.Id == shipmentDocumentId && bs.Type == DocType.Shipment);
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
        var shipmentDocument = await context.ShipmentDocuments.FirstOrDefaultAsync(bs => bs.Id == shipmentDocumentId && bs.Type == DocType.Shipment);
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
    
    public async Task<Result<Guid>> CreateWayBill(CreateShipmentDocumentRequest request, Guid userId)
    {
        var wayBill = mapper.Map<ShipmentDocument>(request);
        wayBill.Type = DocType.Waybill;
        wayBill.CreatedById = userId;
        await context.ShipmentDocuments.AddAsync(wayBill);
        await context.SaveChangesAsync();

        return wayBill.Id;
    }
    
    public async Task<Result<ShipmentDocumentDto>> GetWaybillDocument(Guid shipmentDocumentId)
    {
        var shipmentDocument = await context.ShipmentDocuments
            .AsSplitQuery()
            .Include(s => s.ShipmentInvoice)
            .ThenInclude(s => s.Items)
            .FirstOrDefaultAsync(bs => bs.Id == shipmentDocumentId && bs.Type == DocType.Waybill);
    
        return shipmentDocument is null
            ? Error.NotFound("ShipmentDocument.NotFound", "Waybill document not found")
            : mapper.Map<ShipmentDocumentDto>(shipmentDocument, opt => opt.Items[AppConstants.ModelType] = nameof(ShipmentDocument));
    }
    
    public async Task<Result<Paginateable<IEnumerable<ShipmentDocumentDto>>>> GetWaybillDocuments(int page, int pageSize, string searchQuery, ShipmentStatus? status = null)
    {
        var query = context.ShipmentDocuments
            .Include(s => s.ShipmentInvoice)
            .Where(s => s.Type == DocType.Waybill)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }
    
        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, bs => bs.Code);
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
    
    public async Task<Result> UpdateWaybillDocument(CreateShipmentDocumentRequest request, Guid shipmentDocumentId, Guid userId)
    {
        var existingShipmentDocument = await context.ShipmentDocuments.FirstOrDefaultAsync(bs => bs.Id == shipmentDocumentId && bs.Type == DocType.Waybill);
        if (existingShipmentDocument is null)
        {
            return Error.NotFound("ShipmentDocument.NotFound", "Waybill document not found");
        }
    
        mapper.Map(request, existingShipmentDocument);
        existingShipmentDocument.LastUpdatedById = userId;
    
        context.ShipmentDocuments.Update(existingShipmentDocument);
        await context.SaveChangesAsync();
        return Result.Success();
    }
    
    public async Task<Result> DeleteWaybillDocument(Guid shipmentDocumentId, Guid userId)
    {
        var shipmentDocument = await context.ShipmentDocuments.FirstOrDefaultAsync(bs => bs.Id == shipmentDocumentId && bs.Type == DocType.Waybill);
        if (shipmentDocument is null)
        {
            return Error.NotFound("ShipmentDocument.NotFound", "Waybill document not found");
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
            .Include(si => si.Items)
                .ThenInclude(item => item.Material)
            .Include(si => si.Items)
                .ThenInclude(item => item.UoM)
            .Include(si => si.Items)
                .ThenInclude(si => si.Manufacturer)
            .Include(si => si.Items)
            .ThenInclude(si => si.PurchaseOrder)
            .AsSplitQuery()
            .FirstOrDefaultAsync(si => si.Id == shipmentInvoiceId);

        return shipmentInvoice is null
            ? Error.NotFound("ShipmentInvoice.NotFound", "Shipment invoice not found")
            : mapper.Map<ShipmentInvoiceDto>(shipmentInvoice);
    }
    
    public async Task<Result<ShipmentInvoiceDto>> GetShipmentInvoiceByShipmentDocument(Guid shipmentDocumentId)
    {
        var shipmentDocument = await context.ShipmentDocuments
            .Include(s => s.ShipmentInvoice)
            .FirstOrDefaultAsync(bs => bs.Id == shipmentDocumentId);

        if (shipmentDocument is not { ShipmentInvoiceId: not null })
        {
            return Error.NotFound("ShipmentDocument.NotFound", "Shipment document not found");
        }
        
        var shipmentInvoice = await context.ShipmentInvoices
            .Include(si => si.Items)
            .ThenInclude(item => item.Material)
            .Include(si => si.Items)
            .ThenInclude(item => item.UoM)
            .Include(si => si.Items)
            .ThenInclude(si => si.Manufacturer)
            .Include(si => si.Items)
            .ThenInclude(si => si.PurchaseOrder)
            .AsSplitQuery()
            .FirstOrDefaultAsync(si => si.Id == shipmentDocument.ShipmentInvoiceId);
        
        return shipmentInvoice is null
            ? Error.NotFound("ShipmentInvoice.NotFound", "Shipment invoice not found")
            : mapper.Map<ShipmentInvoiceDto>(shipmentInvoice);
    }
    
    public async Task<Result<Paginateable<IEnumerable<ShipmentInvoiceDto>>>> GetShipmentInvoices(int page, int pageSize, string searchQuery)
    {
        var query = context.ShipmentInvoices
            .Include(si => si.Items)
            .ThenInclude(item => item.Material)
            .Include(si => si.Items)
            .ThenInclude(item => item.UoM)
            .Include(si => si.Items)
            .ThenInclude(si => si.Manufacturer)
            .Include(si => si.Items)
            .ThenInclude(si => si.PurchaseOrder)
            .AsSplitQuery()
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, bs => bs.Code);
        }
        
        var paginatedResult = await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize);
        var shipmentInvoices = await paginatedResult.Data.ToListAsync();
        
        return new Paginateable<IEnumerable<ShipmentInvoiceDto>>
        {
            Data = mapper.Map<IEnumerable<ShipmentInvoiceDto>>(shipmentInvoices),
            PageIndex = page,
            PageCount = paginatedResult.PageCount,
            TotalRecordCount = paginatedResult.TotalRecordCount,
            StartPageIndex = paginatedResult.StartPageIndex,
            StopPageIndex = paginatedResult.StopPageIndex
        };
    }
    public async Task<Result<IEnumerable<ShipmentInvoiceDto>>> GetUnattachedShipmentInvoices()
    {
        var unattachedShipmentInvoices = await context.ShipmentInvoices
            .Where(si => !context.ShipmentDocuments.Any(sd => sd.ShipmentInvoiceId == si.Id))
            .Include(si => si.Items)
            .ThenInclude(item => item.Material)
            .Include(si => si.Items)
            .ThenInclude(item => item.UoM)
            .Include(si => si.Items)
            .ThenInclude(item => item.Manufacturer)
            .Include(si => si.Items)
            .ThenInclude(item => item.PurchaseOrder)
            .AsSplitQuery()
            .ToListAsync();

        return
            mapper.Map<List<ShipmentInvoiceDto>>(unattachedShipmentInvoices);
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
    
    public async Task<Result> MarkShipmentInvoiceAsPaid(Guid shipmentInvoiceId, DateTime? paidAt, Guid userId)
    {
        var existingShipmentInvoice = await context.ShipmentInvoices
            .Include(si => si.Items)
            .FirstOrDefaultAsync(si => si.Id == shipmentInvoiceId);
        if (existingShipmentInvoice is null)
        {
            return Error.NotFound("ShipmentInvoice.NotFound", "Shipment invoice not found");
        }

        existingShipmentInvoice.PaidAt = paidAt ?? DateTime.UtcNow;
        existingShipmentInvoice.LastUpdatedById = userId;
        context.ShipmentInvoices.Update(existingShipmentInvoice);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> MarkMultipleShipmentInvoicesAsPaid(List<Guid> shipmentIds, DateTime? paidAt, Guid userId)
    {
        paidAt ??= DateTime.UtcNow;
        
        shipmentIds ??= [];

        if (shipmentIds.Count == 0)
        {
            return Error.NotFound("ShipmentId.Empty", "No shipments ids were provided.");
        }
        
        await context.ShipmentInvoices
            .Where(s => shipmentIds.Contains(s.Id))
            .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.PaidAt, paidAt)
                    .SetProperty(p => p.LastUpdatedById, userId));
        
        await context.BillingSheets
            .Where(b => shipmentIds.Contains(b.InvoiceId))
            .ExecuteUpdateAsync(setters=> setters
                .SetProperty(p => p.LastUpdatedById, userId)
                .SetProperty(p  => p.Status, BillingSheetStatus.Paid));
        
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

    public async Task<Result<List<SupplierDto>>> GetSupplierForPurchaseOrdersNotLinkedOrPartiallyUsed()
    {
        var linkedPurchaseOrderIds = await context.ShipmentInvoiceItems
            .Select(sii => sii.PurchaseOrderId)
            .Distinct()
            .ToListAsync(); // Get all PO IDs that have been used in shipment invoice items

        // Get purchase orders that are NOT linked at all
        var notLinkedPurchaseOrders = await context.PurchaseOrders
            .Include(p => p.Supplier)
            .Where(po => !linkedPurchaseOrderIds.Contains(po.Id))
            .ToListAsync();

        // Find purchase orders that have been partially used
        var partiallyUsedPurchaseOrders = await context.PurchaseOrders
            .Include(po => po.Supplier)
            .Where(po =>
                    po.Items.Any(poi => context.ShipmentInvoiceItems.Any(sii => sii.PurchaseOrderId == po.Id && sii.MaterialId == poi.MaterialId)) // At least one item used
                    && 
                    po.Items.Any(poi => !context.ShipmentInvoiceItems.Any(sii => sii.PurchaseOrderId == po.Id && sii.MaterialId == poi.MaterialId)) // At least one item NOT used
            )
            .ToListAsync();

        // Combine the results
        var suppliers = notLinkedPurchaseOrders
            .Concat(partiallyUsedPurchaseOrders)
            .Select(p => p.Supplier)
            .DistinctBy(s => s.Id)
            .ToList();
        
        return mapper.Map<List<SupplierDto>>(suppliers);
    }
    
    public async Task<Result<List<PurchaseOrderDto>>> GetSupplierPurchaseOrdersNotLinkedOrPartiallyUsedAsync(Guid supplierId)
    {
        var allSupplierPurchaseOrderIds = await context.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.Items)
            .Where(po => po.SupplierId == supplierId)
            .Select(po => po.Id)
            .ToListAsync(); // Get all purchase order IDs for the supplier

        var linkedPurchaseOrderIds = await context.ShipmentInvoiceItems
            .Where(sii => allSupplierPurchaseOrderIds.Contains(sii.PurchaseOrderId))
            .Select(sii => sii.PurchaseOrderId)
            .Distinct()
            .ToListAsync(); // Get all supplier PO IDs that are linked to shipment invoice items

        // Get supplier purchase orders that are NOT linked at all
        var notLinkedPurchaseOrders = await context.PurchaseOrders
            .Where(po => po.SupplierId == supplierId && !linkedPurchaseOrderIds.Contains(po.Id))
            .ToListAsync();

        // Find supplier purchase orders that have been partially used
        var partiallyUsedPurchaseOrders = await context.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.Items)
            .Where(po => po.SupplierId == supplierId &&
                         po.Items.Any(poi => context.ShipmentInvoiceItems.Any(sii => sii.PurchaseOrderId == po.Id && sii.MaterialId == poi.MaterialId)) && // At least one item used
                         po.Items.Any(poi => !context.ShipmentInvoiceItems.Any(sii => sii.PurchaseOrderId == po.Id && sii.MaterialId == poi.MaterialId))) // At least one item NOT used
            .ToListAsync();

        var resultPurchaseOrders = notLinkedPurchaseOrders
            .Concat(partiallyUsedPurchaseOrders)
            .Distinct()
            .ToList();

        var result = mapper.Map<List<PurchaseOrderDto>>(resultPurchaseOrders, opt => opt.Items[AppConstants.ModelType] = nameof(PurchaseOrder));

        foreach (var po in result)
        {
            foreach (var item in po.Items)
            {
                if (item.Material?.Id != null)
                    item.Manufacturers = (await GetSupplierManufacturersByMaterial(item.Material.Id.Value, po.Supplier.Id)).Value;
            }
        }
        return result;
    }
    
    public async Task<Result<List<MaterialDto>>> GetMaterialsByPurchaseOrderIdsAsync(List<Guid> purchaseOrderIds)
    {
        if (purchaseOrderIds == null || purchaseOrderIds.Count == 0)
            return Error.Failure("PurchaseOrder.Materials", "No Purchase Order IDs provided.");

        var materials = await context.ShipmentInvoiceItems
            .Where(sii => purchaseOrderIds.Contains(sii.PurchaseOrderId))
            .Select(sii => sii.Material)
            .Distinct()
            .ToListAsync();

        return mapper.Map<List<MaterialDto>>(materials);
    }

    public async Task<Result> MarkShipmentAsArrived(Guid shipmentDocumentId, Guid userId)
    {
        var shipmentDocument = await context.ShipmentDocuments
            .AsSplitQuery()
            .Include(shipmentDocument => shipmentDocument.ShipmentInvoice)
            .ThenInclude(shipmentInvoice => shipmentInvoice.Items).FirstOrDefaultAsync(sd => sd.Id == shipmentDocumentId);
        if (shipmentDocument is null)
        {
            return Error.NotFound("ShipmentDocument.NotFound", "Shipment document not found");
        }

        shipmentDocument.ArrivedAt = DateTime.UtcNow;
        shipmentDocument.Status = ShipmentStatus.Arrived;
        shipmentDocument.LastUpdatedById = userId;

        context.ShipmentDocuments.Update(shipmentDocument);
        await context.SaveChangesAsync();

        var purchaseOrdersIds = shipmentDocument.ShipmentInvoice.Items.Select(s => s.PurchaseOrderId).Distinct().ToList();
        var purchaseOrders = await context.PurchaseOrders
            .AsSplitQuery()
            .Where(p => purchaseOrdersIds.Contains(p.Id))
            .Include(purchaseOrder => purchaseOrder.SourceRequisition)
            .ThenInclude(sourceRequisition => sourceRequisition.Items).ToListAsync();
        var sourceRequisitions = purchaseOrders.Select(p => p.SourceRequisition);
        List<Guid> departmentIds = [];
        List<User> creators = [];
        foreach (var sourceRequisition in sourceRequisitions)
        {
            var requisitionIds = sourceRequisition.Items.Select(i => i.RequisitionId).ToList();
            var requisition = await context.Requisitions
                .AsSplitQuery()
                .Include(r => r.CreatedBy)
                .Where(r => requisitionIds.Contains(r.Id)).ToListAsync();
            creators.AddRange(requisition.Select(r => r.CreatedBy));
            departmentIds.AddRange(requisition.Select(r => r.DepartmentId));
        }
        
        departmentIds = departmentIds.Distinct().ToList();
        creators = creators.Distinct().ToList();
        foreach (var departmentId in departmentIds)
        {
            backgroundWorkerService.EnqueueNotification(
                $"Shipment for Invoice {shipmentDocument.ShipmentInvoice.Code} has arrived.",
                NotificationType.ShipmentArrived, departmentId);
        }
        
        backgroundWorkerService.EnqueueNotification(
            $"Shipment for Invoice {shipmentDocument.ShipmentInvoice.Code} has arrived.",
            NotificationType.ShipmentArrived, null, creators);

        return Result.Success();
    }
    
    public async Task<Result<Paginateable<IEnumerable<ShipmentDocumentDto>>>> GetArrivedShipments(int page, int pageSize, string searchQuery, bool excludeCompletedDistribution)
    {
        var query = context.ShipmentDocuments
            .Include(shipmentDoc => shipmentDoc.ShipmentInvoice)
            .ThenInclude(shipmenInvoice => shipmenInvoice.Items)
            .ThenInclude(items=>items.Manufacturer)
            .Include(shipmentDoc => shipmentDoc.ShipmentInvoice)
            .ThenInclude(shipmenInvoice => shipmenInvoice.Items)
            .ThenInclude(items=>items.Material)
            .Include(shipmentDoc => shipmentDoc.ShipmentInvoice)
            .ThenInclude(shipmenInvoice => shipmenInvoice.Supplier)
            .Where(sd => sd.Status == ShipmentStatus.Arrived)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, bs => bs.Code);
        }

        if (excludeCompletedDistribution)
        {
            query = query.Where(q => q.CompletedDistributionAt.HasValue);
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

    public async Task<Result<MaterialDistributionDto>> GetMaterialDistribution(Guid shipmentDocumentId)
    {
        try
        {
            var shipmentDocument = await context.ShipmentDocuments
                .Include(s => s.ShipmentInvoice)
                .FirstOrDefaultAsync(bs => bs.Id == shipmentDocumentId);

            var invoices = await context.ShipmentInvoices
                .AsSplitQuery()
                .Include(s=>s.Items.Where(i=>!i.Distributed))
                .ThenInclude(item=>item.Material)
                .Include(s=>s.Items.Where(i=>!i.Distributed))
                .ThenInclude(items=>items.Manufacturer)
                .Include(s=>s.Supplier)
                .Include(s=>s.Items.Where(i=>!i.Distributed))
                .ThenInclude(s => s.PurchaseOrder).ThenInclude(p => p.SourceRequisition).ThenInclude(sr => sr.Items)
                .FirstOrDefaultAsync(s => s.Id == shipmentDocument.ShipmentInvoiceId);
            
            var materialDistribution = new MaterialDistributionDto();

            var distributionShipmentInvoiceItems = await GroupInvoiceItemsBasedOnMaterial(invoices);

            foreach (var item in distributionShipmentInvoiceItems)
            {
                var materialDistributionSection = new MaterialDistributionSection
                {
                    Material = mapper.Map<MaterialDto>(item.Material),
                    TotalQuantity = item.ReceivedQuantity,
                    UoM = mapper.Map<UnitOfMeasureDto>(item.UoM)
                };
                
                var requisitionMaterialRequests = await (
                    from r in context.RequisitionItems
                    join si in context.ShipmentInvoiceItems on r.MaterialId equals item.MaterialId
                    join po in context.PurchaseOrders on si.PurchaseOrderId equals po.Id
                    join sr in context.SourceRequisitionItems on po.SourceRequisitionId equals sr.SourceRequisitionId
                    join sd in context.ShipmentDocuments on si.ShipmentInvoiceId equals sd.ShipmentInvoiceId
                    where sr.RequisitionId == r.RequisitionId
                          && r.MaterialId == item.MaterialId
                          && (r.Quantity - r.QuantityReceived) != 0
                          && sd.Id == shipmentDocumentId // Ensuring linkage to the shipment document
                    select r
                ).Distinct().ToListAsync();

                foreach (var requisitionItem in requisitionMaterialRequests)
                {
                    var department = await GetRequisitionDepartment(requisitionItem.RequisitionId);
                    var distributionRequisitionItem = new DistributionRequisitionItem
                    {
                        Department = mapper.Map<DepartmentDto>(department),
                        RequisitionItem = mapper.Map<RequisitionItemDto>(requisitionItem),
                        QuantityRequested = requisitionItem.Quantity
                    };

                    materialDistributionSection.Items.Add(distributionRequisitionItem);
                }

                ProcessMaterialDistributions(materialDistributionSection, mapper.Map<List<ShipmentInvoiceItemDto>>(item.ShipmentInvoiceItems.ToList()));
                materialDistribution.Sections.Add(materialDistributionSection);
            }

            return materialDistribution;

        }
        catch(Exception ex )
        {
            return Error.Failure("500",ex.Message);
        }
    }
    
    private async Task<List<DistributionShipmentInvoiceItemDto>> GroupInvoiceItemsBasedOnMaterial(ShipmentInvoice invoices)
    {
        var groupedItems = invoices.Items
            .GroupBy(item => item.MaterialId)
            .Select(group => new DistributionShipmentInvoiceItemDto
            {
                MaterialId = group.Key,
                Material = group.First().Material,
                ReceivedQuantity = group.Sum(item => item.ReceivedQuantity),
                ShipmentInvoiceItems = group.ToList(),
                UoM = group.First().UoM
            })
            .ToList();

        return await Task.FromResult(groupedItems);
    }
    
    public async Task<Result> ConfirmDistribution(Guid shipmentDocumentId, Guid materialId)
    {
        
        var shipmentDocument = await context.ShipmentDocuments
            .Include(s => s.ShipmentInvoice).ThenInclude(shipmentInvoice => shipmentInvoice.Items)
            .FirstOrDefaultAsync(bs => bs.Id == shipmentDocumentId);

        if (shipmentDocument is null)
        {
            return Error.NotFound("ShipmentDocument.NotFound", "Shipment document not found");
        }
        
        var materialDistributionResult = await GetMaterialDistribution(shipmentDocumentId);
        if (!materialDistributionResult.IsSuccess)
        {
            return Error.NotFound("MaterialDistribution.NotFound", "Material distribution not found for this shipment document.");
        }

        var materialDistribution = materialDistributionResult.Value.Sections.First(s => s.Material.Id == materialId);

        foreach (var item in materialDistribution.Items)
        {
            var requisitionItem = await context.RequisitionItems.Include(r=>r.Material).FirstOrDefaultAsync(r => r.Id == item.RequisitionItem.Id);
            if (requisitionItem is null)
            {
                return Error.NotFound("RequisitionItem.NotFound", "Requisition item not found");
            }
            
            requisitionItem.QuantityReceived += item.QuantityAllocated;
            context.RequisitionItems.Update(requisitionItem);

            Warehouse departmentWarehouse = null;
            if (requisitionItem.Material.Kind == MaterialKind.Package)
            {
                departmentWarehouse = context.Warehouses
                    .IgnoreQueryFilters()
                    .Include(warehouse => warehouse.ArrivalLocation).FirstOrDefault(w => w.DepartmentId == item.Department.Id && w.Type == WarehouseType.PackagedStorage);
            }
            if(requisitionItem.Material.Kind == MaterialKind.Raw)
            {
                departmentWarehouse = context.Warehouses
                    .IgnoreQueryFilters()
                    .Include(warehouse => warehouse.ArrivalLocation).FirstOrDefault(w => w.DepartmentId == item.Department.Id && w.Type == WarehouseType.RawMaterialStorage);
            }
            
            if(departmentWarehouse is null) return  Error.NotFound("Warehouse.NotFound", "Warehouse department not found");
            
            if (departmentWarehouse.ArrivalLocation == null)
            {
                departmentWarehouse.ArrivalLocation = new WarehouseArrivalLocation
                {
                    WarehouseId = departmentWarehouse.Id,
                    Name = "Default Arrival Location",
                    FloorName = "Ground Floor",
                    Description = "Automatically created arrival location"
                };
                await context.WarehouseArrivalLocations.AddAsync(departmentWarehouse.ArrivalLocation);
            }
                
            var distributedRequisitionMaterial = new DistributedRequisitionMaterial
            {
                RequisitionItemId = requisitionItem.Id,
                MaterialId = requisitionItem.MaterialId,
                ShipmentInvoiceId = shipmentDocument.ShipmentInvoiceId,
                UomId = requisitionItem.UoMId,
                Quantity = item.QuantityAllocated,
                Status = DistributedRequisitionMaterialStatus.Distributed,
                DistributedAt = DateTime.UtcNow,
                MaterialItemDistributions = item.Distributions.Select(d => new MaterialItemDistribution
                {
                    ShipmentInvoiceItemId = d.ShipmentInvoiceItem.Id,
                    Quantity = d.Quantity,
                }).ToList(),
                WarehouseArrivalLocationId = departmentWarehouse.ArrivalLocation.Id
                        
            };
            await context.DistributedRequisitionMaterials.AddAsync(distributedRequisitionMaterial);
        }

        var distributions = materialDistribution.Items.SelectMany(i => i.Distributions).ToList();
        var invoiceItems = await context.ShipmentInvoiceItems
            .Where(si => distributions.Select(d => d.ShipmentInvoiceItem.Id).Contains(si.Id) && !si.Distributed)
            .ToListAsync();
        
        foreach (var item in invoiceItems)
        {
            item.Distributed = true;
        }
        
        context.ShipmentInvoiceItems.UpdateRange(invoiceItems);
        await context.SaveChangesAsync();

        
        var allItemsDistributed = (await context.ShipmentDocuments
            .Include(s => s.ShipmentInvoice).ThenInclude(si => si.Items)
            .FirstOrDefaultAsync(s => s.Id == shipmentDocumentId))?.ShipmentInvoice?.Items.All(item => item.Distributed);

        if (allItemsDistributed ?? false)
        {
            shipmentDocument.CompletedDistributionAt = DateTime.UtcNow;
        }
        
        await context.SaveChangesAsync();
        return Result.Success();
    }
    
    public async Task<Result> ConfirmDistribution(Guid shipmentDocumentId)
    {
        var shipmentDocument = await context.ShipmentDocuments
            .Include(s => s.ShipmentInvoice).ThenInclude(shipmentInvoice => shipmentInvoice.Items)
            .FirstOrDefaultAsync(bs => bs.Id == shipmentDocumentId);

        if (shipmentDocument is null)
        {
            return Error.NotFound("ShipmentDocument.NotFound", "Shipment document not found");
        }

        var materialDistributionResult = await GetMaterialDistribution(shipmentDocumentId);
        if (!materialDistributionResult.IsSuccess)
        {
            return Error.NotFound("MaterialDistribution.NotFound", "Material distribution not found for this shipment document.");
        }

        var materialDistributions = materialDistributionResult.Value.Sections;
        var distributedMaterials = new List<DistributedRequisitionMaterial>();
        var distributedInvoiceItems = new HashSet<Guid>();

        foreach (var section in materialDistributions)
        {
            foreach (var item in section.Items)
            {
                var requisitionItem = await context.RequisitionItems
                    .Include(r => r.Material)
                    .FirstOrDefaultAsync(r => r.Id == item.RequisitionItem.Id);

                if (requisitionItem is null)
                {
                    return Error.NotFound("RequisitionItem.NotFound", "Requisition item not found.");
                }

                // Update received quantity
                requisitionItem.QuantityReceived += item.QuantityAllocated;
                context.RequisitionItems.Update(requisitionItem);

                // Determine the correct warehouse for this department
                var departmentWarehouse = requisitionItem.Material.Kind == MaterialKind.Raw ? await context.Warehouses
                    .IgnoreQueryFilters()
                    .Include(warehouse => warehouse.ArrivalLocation)
                    .FirstOrDefaultAsync(w => w.DepartmentId == item.Department.Id && w.Type == WarehouseType.RawMaterialStorage) : 
                    await context.Warehouses
                        .IgnoreQueryFilters()
                        .Include(warehouse => warehouse.ArrivalLocation)
                        .FirstOrDefaultAsync(w => w.DepartmentId == item.Department.Id && w.Type == WarehouseType.PackagedStorage);

                if (departmentWarehouse != null)
                {
                    if (departmentWarehouse.ArrivalLocation == null)
                    {
                        departmentWarehouse.ArrivalLocation = new WarehouseArrivalLocation
                        {
                            WarehouseId = departmentWarehouse.Id,
                            Name = "Default Arrival Location",
                            FloorName = "Ground Floor",
                            Description = "Automatically created arrival location"
                        };
                        await context.WarehouseArrivalLocations.AddAsync(departmentWarehouse.ArrivalLocation);
                    }

                    // Create distributed material record
                    var distributedRequisitionMaterial = new DistributedRequisitionMaterial
                    {
                        RequisitionItemId = requisitionItem.Id,
                        MaterialId = requisitionItem.MaterialId,
                        ShipmentInvoiceId = shipmentDocument.ShipmentInvoiceId,
                        UomId = requisitionItem.UoMId,
                        Quantity = item.QuantityAllocated,
                        Status = DistributedRequisitionMaterialStatus.Distributed,
                        DistributedAt = DateTime.UtcNow,
                        MaterialItemDistributions = item.Distributions.Select(d => new MaterialItemDistribution
                        {
                            ShipmentInvoiceItemId = d.ShipmentInvoiceItem.Id,
                            Quantity = d.Quantity,
                        }).ToList(),
                        WarehouseArrivalLocationId = departmentWarehouse.ArrivalLocation.Id
                    };

                    distributedMaterials.Add(distributedRequisitionMaterial);

                    // Mark related shipment invoice items as distributed
                    foreach (var distribution in item.Distributions)
                    {
                        distributedInvoiceItems.Add(distribution.ShipmentInvoiceItem.Id);
                    }
                }
            }
        }

        // Bulk insert all distributed materials
        await context.DistributedRequisitionMaterials.AddRangeAsync(distributedMaterials);

        // Mark all invoice items as distributed
        var invoiceItems = await context.ShipmentInvoiceItems
            .Where(si => si.ShipmentInvoiceId == shipmentDocument.ShipmentInvoiceId && !si.Distributed)
            .ToListAsync();

        foreach (var item in invoiceItems)
        {
            if (distributedInvoiceItems.Contains(item.Id))
            {
                item.Distributed = true;
            }
        }

        // If all items in the shipment invoice are distributed, mark the shipment as complete
        var allItemsDistributed = shipmentDocument.ShipmentInvoice.Items.All(i => i.Distributed);
        if (allItemsDistributed)
        {
            shipmentDocument.CompletedDistributionAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
        return Result.Success();
    }

    

    /*public async Task<Result> ConfirmDistribution(MaterialDistributionSectionRequest section, Guid userId)
    {
        var invoiceItems = await context.ShipmentInvoiceItems
            .Where(s => section.ShipmentInvoiceItemIds.Contains(s.Id))
            .ToListAsync();
        
        var manufacturers = await context.Manufacturers.Where(m => section.ManufacturerIds.Contains(m.Id)).ToListAsync();

        if (invoiceItems.Count == 0)
        {
            return Error.NotFound("ShipmentInvoiceItem.NotFound", "Shipment invoice item not found");
        }
        
        var shipmentDocument = await context.ShipmentDocuments
            .Include(s => s.ShipmentInvoice).ThenInclude(shipmentInvoice => shipmentInvoice.Items)
            .FirstOrDefaultAsync(bs => bs.ShipmentInvoiceId == invoiceItems.FirstOrDefault().ShipmentInvoiceId);

        if (shipmentDocument is null)
        {
            return Error.NotFound("ShipmentDocument.NotFound", "Shipment document not found");
        }
        
        foreach (var item in section.Items)
        {
            var requisitionItem = await context.RequisitionItems.Include(r=>r.Material).FirstOrDefaultAsync(r => r.Id == item.RequisitionItemId);
            if (requisitionItem is null)
            {
                return Error.NotFound("RequisitionItem.NotFound", "Requisition item not found");
            }
            requisitionItem.QuantityReceived += item.QuantityAllocated;
            Warehouse departmentWarehouse = null;
            if (requisitionItem.Material.Kind == MaterialKind.Package)
            {
                departmentWarehouse = context.Warehouses
                    .Include(warehouse => warehouse.ArrivalLocation).FirstOrDefault(w => w.DepartmentId == item.DepartmentId && w.Type == WarehouseType.PackagedStorage);
            }
            if(requisitionItem.Material.Kind == MaterialKind.Raw)
            {
                departmentWarehouse = context.Warehouses
                    .Include(warehouse => warehouse.ArrivalLocation).FirstOrDefault(w => w.DepartmentId == item.DepartmentId && w.Type == WarehouseType.RawMaterialStorage);
            }
            
            if (departmentWarehouse != null)
            {
                var warehouse = departmentWarehouse;
                if (warehouse.ArrivalLocation == null)
                {
                    warehouse.ArrivalLocation = new WarehouseArrivalLocation
                    {
                        WarehouseId = warehouse.Id,
                        Name = "Default Arrival Location",
                        FloorName = "Ground Floor",
                        Description = "Automatically created arrival location"
                    };
                    await context.WarehouseArrivalLocations.AddAsync(warehouse.ArrivalLocation);
                }
                
                var distributedRequisitionMaterial = new DistributedRequisitionMaterial
                    {
                        RequisitionItemId = requisitionItem.Id,
                        MaterialId = requisitionItem.MaterialId,
                        Manufacturers = manufacturers,
                        SupplierId = section.SupplierId,
                        ShipmentInvoiceId = invoiceItems.FirstOrDefault()?.ShipmentInvoiceId,
                        ShipmentInvoiceItems = invoiceItems,
                        UomId = requisitionItem.UomId,
                        Quantity = item.QuantityAllocated,
                        Status = DistributedRequisitionMaterialStatus.Distributed,
                        DistributedAt = DateTime.UtcNow,
                        WarehouseArrivalLocationId = warehouse.ArrivalLocation.Id,
                        CreatedById = userId
                    };
                await context.DistributedRequisitionMaterials.AddAsync(distributedRequisitionMaterial);
            }
        }
        
        foreach (var item in invoiceItems)
        {
            item.Distributed = true;
        }
        
        var allItemsDistributed = shipmentDocument.ShipmentInvoice.Items.All(item => item.Distributed);

        if (allItemsDistributed)
        {
            shipmentDocument.CompletedDistributionAt = DateTime.UtcNow;
        }
        
        await context.SaveChangesAsync();
        return Result.Success();
    } */

    private void ProcessMaterialDistributions(MaterialDistributionSection materialDistributionSection, 
    List<ShipmentInvoiceItemDto> shipmentInvoiceItems)
    {
        // Step 1: Calculate allocations based on available stock
        var totalRequestedQuantity = materialDistributionSection.Items.Sum(i => i.QuantityRequested);

        foreach (var item in materialDistributionSection.Items)
        {
            // Allocate proportionally based on total available quantity
            item.QuantityAllocated = Math.Round(item.QuantityRequested / totalRequestedQuantity * materialDistributionSection.TotalQuantity, 2);

            // Track the shortfall (for display purposes)
            item.QuantityRemaining = Math.Round(item.QuantityRequested - item.QuantityAllocated, 2);
            if (item.QuantityRemaining < 0)
            {
                item.QuantityRemaining = 0; // Ensure no negative values
            }
        }

        // Step 2: Sort shipment invoices by creation date (FIFO order)
        shipmentInvoiceItems = shipmentInvoiceItems.OrderBy(r => r.CreatedAt).ToList();

        // Step 3: Distribute `QuantityAllocated` across shipments
        foreach (var itemDistribution in materialDistributionSection.Items)
        {
            var remainingToAllocate = itemDistribution.QuantityAllocated; // This is what needs to be distributed

            foreach (var shipmentInvoiceItem in shipmentInvoiceItems)
            {
                if (remainingToAllocate <= 0)
                    break; // Stop allocating when the full allocated amount has been distributed

                var availableQuantity = shipmentInvoiceItem.ReceivedQuantity;
                var quantityToAllocate = Math.Min(remainingToAllocate, availableQuantity); 
                
                var shipmentInvoiceCopy = JsonConvert.DeserializeObject<ShipmentInvoiceItemDto>(
                    JsonConvert.SerializeObject(shipmentInvoiceItem)
                );

                if (quantityToAllocate > 0)
                {
                    itemDistribution.Distributions.Add(new MaterialItemDistributionDto
                    {
                        ShipmentInvoiceItem = shipmentInvoiceCopy,
                        Quantity = quantityToAllocate
                    });

                    // Reduce the remaining allocation and shipment stock
                    remainingToAllocate -= quantityToAllocate;
                    shipmentInvoiceItem.ReceivedQuantity -= quantityToAllocate;
                }
            }
        }
    }
    

    private async Task<Department> GetRequisitionDepartment(Guid requisitionId)
    {
        // var requisition = await context.Requisitions
        //     .Include(r => r.RequestedBy)
        //     .FirstOrDefaultAsync(r => r.Id == requisitionId);
        // return requisition.RequestedBy.Department;
        
        var requisition = await context.Requisitions
            .Include(r => r.Department)
            .FirstOrDefaultAsync(r => r.Id == requisitionId);
        return requisition.Department;
    }

    public async Task<List<Guid>> GetDepartmentIdsFromPurchaseOrder(Guid purchaseOrderId)
    {
        var purchaseOrder = await context.PurchaseOrders
            .FirstOrDefaultAsync(p => p.Id == purchaseOrderId);
        if (purchaseOrder is null) return [];
        
        var sourceRequisition = await context.SourceRequisitions
            .AsSplitQuery()
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == purchaseOrder.SourceRequisitionId);
        if (sourceRequisition is null) return [];
        
        
        var requisitionIds = sourceRequisition.Items.Select(i => i.RequisitionId).Distinct().ToList();
        
        return await context.Requisitions.Where(r => requisitionIds.Contains(r.Id))
            .Select(r => r.DepartmentId)
            .Distinct()
            .ToListAsync();
    }
}

