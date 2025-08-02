/*using APP.IRepository;
using APP.Services.Email;
using APP.Services.Pdf;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Items.Requisitions;
using DOMAIN.Entities.Memos;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.VendorQuotations;
using INFRASTRUCTURE.Context;
using SHARED;

namespace APP.Repository;

public class InventoryProcurementRepository(
    ApplicationDbContext context,
    IMapper mapper,
    IPdfService pdfService,
    IEmailService emailService)
    : IInventoryProcurementRepository
{
    // --- CRUD for InventoryPurchaseRequisition ---

    public async Task<Result<Guid>> CreateInventoryPurchaseRequisition(CreateInventoryPurchaseRequisition request, Guid userId)
    {
        var requisition = mapper.Map<InventoryPurchaseRequisition>(request);
        requisition.Status = InventoryPurchaseRequisitionStatus.Pending;
        //requisition.Items = mapper.Map<List<InventoryPurchaseRequisitionItem>>(request.Items);
        requisition.CreatedById = userId;

        await context.InventoryPurchaseRequisitions.AddAsync(requisition);
        await context.SaveChangesAsync();
        return requisition.Id;
    }

    public async Task<Result> UpdateInventoryPurchaseRequisition(Guid id, CreateInventoryPurchaseRequisition request)
    {
        var requisition = await context.InventoryPurchaseRequisitions.FirstOrDefaultAsync(r => r.Id == id);
        if (requisition == null)
        {
            return RequisitionErrors.NotFound(id);
        }

        mapper.Map(request, requisition);
        // Assuming item updates are handled separately or in a more complex way
        // This is a simple update and might need more business logic
        context.InventoryPurchaseRequisitions.Update(requisition);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteInventoryPurchaseRequisition(Guid id)
    {
        var requisition = await context.InventoryPurchaseRequisitions.FirstOrDefaultAsync(r => r.Id == id);
        if (requisition == null)
        {
            return RequisitionErrors.NotFound(id);
        }

        requisition.DeletedAt = DateTime.UtcNow;
        context.InventoryPurchaseRequisitions.Update(requisition);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<InventoryPurchaseRequisitionDto>> GetInventoryPurchaseRequisition(Guid id)
    {
        var requisition = await context.InventoryPurchaseRequisitions
            .AsSplitQuery()
            .Include(r => r.Items).ThenInclude(i => i.Item)
            .Include(r => r.Items).ThenInclude(i => i.UoM)
            .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

        if (requisition == null)
        {
            return RequisitionErrors.NotFound(id);
        }
        return mapper.Map<InventoryPurchaseRequisitionDto>(requisition);
    }

    public async Task<Result<Paginateable<IEnumerable<InventoryPurchaseRequisitionDto>>>> GetInventoryPurchaseRequisitions(int page, int pageSize, string searchQuery)
    {
        var query = context.InventoryPurchaseRequisitions
            .AsSplitQuery()
            .Include(r => r.Items)
            .Where(r => r.DeletedAt == null)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(r => r.Code.Contains(searchQuery)); // Assuming Code is the search field
        }

        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, mapper.Map<InventoryPurchaseRequisitionDto>);
    }


    // --- Sourcing Logic ---

    public async Task<Result> CreateSourceRequisition(CreateSourceInventoryRequisition request, Guid userId)
    {
        var requisition = await context.InventoryPurchaseRequisitions
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == request.InventoryPurchaseRequisitionId);

        if (requisition is null)
            return RequisitionErrors.NotFound(request.InventoryPurchaseRequisitionId);

        var supplierGroupedItems = request.Items
            .SelectMany(item => item.Vendors.Select(vendor => new { item, vendor }))
            .GroupBy(x => x.vendor.VendorId);

        foreach (var supplierGroup in supplierGroupedItems)
        {
            var supplierId = supplierGroup.Key;

            var existingSourceRequisition = await context.SourceInventoryRequisitions
                .Include(sr => sr.Items)
                .FirstOrDefaultAsync(sr => sr.VendorId == supplierId && sr.SentQuotationRequestAt == null);

            if (existingSourceRequisition is not null)
            {
                foreach (var groupItem in supplierGroup)
                {
                    existingSourceRequisition.Items.Add(new SourceInventoryRequisitionItem
                    {
                        ItemId = groupItem.item.ItemId,
                        UoMId = groupItem.item.UoMId,
                        Quantity = groupItem.item.Quantity,
                    });
                }
                context.SourceInventoryRequisitions.Update(existingSourceRequisition);
            }
            else
            {
                var requisitionForSupplier = new SourceInventoryRequisition
                {
                    InventoryPurchaseRequisitionId = request.InventoryPurchaseRequisitionId,
                    VendorId = supplierId,
                    Items = supplierGroup.Select(x => new SourceInventoryRequisitionItem
                    {
                        ItemId = x.item.ItemId,
                        UoMId = x.item.UoMId,
                        Quantity = x.item.Quantity,
                    }).ToList(),
                };
                await context.SourceInventoryRequisitions.AddAsync(requisitionForSupplier);
            }
        }
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> CreateMarketRequisition(CreateMarketRequisition request, Guid userId)
    {
        var requisitionItem = await context.InventoryPurchaseRequisitionItems
            .FirstOrDefaultAsync(item => item.Id == request.InventoryPurchaseRequisitionItemId);

        if (requisitionItem is null)
            return RequisitionErrors.NotFound(request.InventoryPurchaseRequisitionItemId);

        var marketRequisition = mapper.Map<MarketRequisition>(request);
        await context.MarketRequisitions.AddAsync(marketRequisition);

        // Update the status of the item in the original requisition
        requisitionItem.Status = RequestStatus.Sourced; // Assuming you have this status
        context.InventoryPurchaseRequisitionItems.Update(requisitionItem);

        await context.SaveChangesAsync();
        return Result.Success();
    }
    
    public async Task<Result<Paginateable<IEnumerable<MarketRequisitionDto>>>> GetMarketRequisitions(int page, int pageSize)
    {
        var query = context.MarketRequisitions
            .AsSplitQuery()
            .Include(mr => mr.Item)
            .Include(mr => mr.UoM)
            .AsQueryable();

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<MarketRequisitionDto>
        );
    }

    public async Task<Result<List<SupplierPriceComparison>>> GetPriceComparisonOfMaterial(SupplierType supplierType)
    {
        if (supplierType == SupplierType.TrustedVendor)
        {
            var quotations = await context.SupplierQuotationItems
                .Include(s => s.Material)
                .Include(s => s.UoM)
                .Include(s => s.SupplierQuotation).ThenInclude(s => s.Supplier)
                .Where(s => s.QuotedPrice != null && s.Status == SupplierQuotationItemStatus.NotProcessed)
                .ToListAsync();

            return Result<List<SupplierPriceComparison>>.Success(quotations.GroupBy(s => new { s.Material, s.UoM })
                .Select(item => new SupplierPriceComparison
                {
                    Material = mapper.Map<CollectionItemDto>(item.Key.Material),
                    UoM = mapper.Map<UnitOfMeasureDto>(item.Key.UoM),
                    Quantity = item.Select(s => s.Quantity).First(),
                    SupplierPrices = item.Select(s => new SupplierPrice
                    {
                        Supplier = mapper.Map<CollectionItemDto>(s.SupplierQuotation.Supplier),
                        Price = s.QuotedPrice.GetValueOrDefault()
                    }).ToList()
                }).ToList());
        }
        else if (supplierType == SupplierType.OpenMarket)
        {
            var openMarketQuotes = await context.MarketRequisitionVendors
                .Include(mrv => mrv.MarketRequisition).ThenInclude(mr => mr.Item)
                .Include(mrv => mrv.MarketRequisition).ThenInclude(mr => mr.UoM)
                .Where(mrv => !mrv.Complete)
                .ToListAsync();

            return Result<List<SupplierPriceComparison>>.Success(openMarketQuotes.GroupBy(mrv => new { mrv.MarketRequisition.Item, mrv.MarketRequisition.UoM })
                .Select(itemGroup => new SupplierPriceComparison
                {
                    Material = mapper.Map<CollectionItemDto>(itemGroup.Key.Item),
                    UoM = mapper.Map<UnitOfMeasureDto>(itemGroup.Key.UoM),
                    Quantity = itemGroup.First().MarketRequisition.Quantity,
                    SupplierPrices = itemGroup.Select(mrv => new SupplierPrice
                    {
                        Supplier = new CollectionItemDto { Name = mrv.VendorName },
                        Price = mrv.PricePerUnit
                    }).ToList()
                }).ToList());
        }

        return Result<List<SupplierPriceComparison>>.Success(new List<SupplierPriceComparison>());
    }


    // --- Memo Creation Logic ---

    public async Task<Result> ProcessOpenMarketMemo(List<ProcessMemo> memos, Guid userId)
    {
        foreach (var memoRequest in memos)
        {
            var memo = new Memo
            {
                Code = await GenerateMemoCode(),
                EstimatedDeliveryDate = memoRequest.EstimatedDeliveryDate,
                DeliveryMode = memoRequest.DeliveryMode,
                TermsOfPayment = memoRequest.TermsOfPayment,
                Items = new List<MemoItem>()
            };

            foreach (var itemRequest in memoRequest.Items)
            {
                var marketRequisitionVendor = await context.MarketRequisitionVendors
                    .Include(mrv => mrv.MarketRequisition)
                    .FirstOrDefaultAsync(mrv => mrv.Id == itemRequest.MarketRequisitionVendorId);

                if (marketRequisitionVendor is null)
                    return Error.Validation("MarketRequisitionVendor", $"Market requisition vendor with ID {itemRequest.MarketRequisitionVendorId} not found.");

                memo.Items.Add(new MemoItem
                {
                    VendorId = marketRequisitionVendor.MarketRequisition.ItemId, // Assuming this is where VendorId lives
                    ItemId = marketRequisitionVendor.MarketRequisition.ItemId,
                    UoMId = marketRequisitionVendor.MarketRequisition.UoMId,
                    Quantity = marketRequisitionVendor.MarketRequisition.Quantity,
                    PricePerUnit = marketRequisitionVendor.PricePerUnit
                });

                marketRequisitionVendor.Complete = true;
                context.MarketRequisitionVendors.Update(marketRequisitionVendor);
            }
            await context.Memos.AddAsync(memo);
        }
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> ProcessTrustedVendorQuotationAndCreateMemo(List<ProcessMemo> memos, Guid userId)
    {
        foreach (var memoRequest in memos)
        {
            var memo = new Memo
            {
                Code = await GenerateMemoCode(),
                EstimatedDeliveryDate = memoRequest.EstimatedDeliveryDate,
                DeliveryMode = memoRequest.DeliveryMode,
                TermsOfPayment = memoRequest.TermsOfPayment,
                Items = new List<MemoItem>()
            };

            foreach (var itemRequest in memoRequest.Items)
            {
                var supplierQuotationItem = await context.SupplierQuotationItems
                    .Include(sqi => sqi.SupplierQuotation)
                    .FirstOrDefaultAsync(sqi => sqi.Id == itemRequest.SupplierQuotationItemId);

                if (supplierQuotationItem is null)
                    return Error.Validation("SupplierQuotationItem", $"Supplier quotation item with ID {itemRequest.SupplierQuotationItemId} not found.");

                memo.Items.Add(new MemoItem
                {
                    VendorId = supplierQuotationItem.SupplierQuotation.SupplierId,
                    ItemId = supplierQuotationItem.MaterialId,
                    UoMId = supplierQuotationItem.UoMId,
                    Quantity = supplierQuotationItem.Quantity,
                    PricePerUnit = supplierQuotationItem.QuotedPrice.GetValueOrDefault()
                });

                supplierQuotationItem.Status = SupplierQuotationItemStatus.Processed;
                context.SupplierQuotationItems.Update(supplierQuotationItem);
            }
            await context.Memos.AddAsync(memo);
        }
        await context.SaveChangesAsync();
        return Result.Success();
    }

    // --- Trusted Vendor Specific ---

    public async Task<Result> SendQuotationToSupplier(Guid supplierId)
    {
        var sourceRequisition = await context.SourceInventoryRequisitions
            .Include(sr => sr.Supplier)
            .Include(sr => sr.Items).ThenInclude(item => item.Item)
            .Include(sr => sr.Items).ThenInclude(item => item.UoM)
            .FirstOrDefaultAsync(s => s.SupplierId == supplierId && !s.SentQuotationRequestAt.HasValue);

        if (sourceRequisition is null || !sourceRequisition.Items.Any())
        {
            return Error.Validation("Supplier.Quotation", "No unsent items found for the specified supplier.");
        }

        var supplierQuotationDto = mapper.Map<SupplierQuotationRequest>(sourceRequisition);
        var fileContent = pdfService.GeneratePdfFromHtml(PdfTemplate.QuotationRequestTemplate(supplierQuotationDto));
        var mailAttachments = new List<(byte[] fileContent, string fileName, string fileType)>
        {
            (fileContent, "Quotation Request from Entrance.pdf", "application/pdf")
        };

        try
        {
            emailService.SendMail(sourceRequisition.Supplier.Email, "Sales Quote From Entrance", "Please find attached to this email a sales quote from us.", mailAttachments);
        }
        catch (Exception e)
        {
            // Log the exception
            return Error.Validation("Supplier.Quotation", "Failed to send email: " + e.Message);
        }

        sourceRequisition.SentQuotationRequestAt = DateTime.UtcNow;
        context.SourceInventoryRequisitions.Update(sourceRequisition);
        
        var supplierQuotation = new VendorQuotation
        {
            VendorId = sourceRequisition.SupplierId,
            SourceRequisitionId = sourceRequisition.Id,
            Items = sourceRequisition.Items.Select(i => new SupplierQuotationItem
            {
                MaterialId = i.ItemId,
                UoMId = i.UoMId,
                Quantity = i.Quantity
            }).ToList()
        };

        await context.SupplierQuotations.AddAsync(supplierQuotation);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<Paginateable<IEnumerable<SupplierQuotationDto>>>> GetVendorQuotations(int page, int pageSize, SupplierType supplierType, bool received)
    {
        var query = context.VendorQuotations
            .Include(s => s.Items).ThenInclude(s => s.Material)
            .Include(s => s.Items).ThenInclude(s => s.UoM)
            .Include(s => s.Supplier)
            .Where(s => s.Supplier.Type == supplierType)
            .AsQueryable();

        query = received ? query.Where(s => s.ReceivedQuotation) : query.Where(s => !s.ReceivedQuotation);

        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, mapper.Map<SupplierQuotationDto>);
    }

    public async Task<Result> ReceiveQuotationFromSupplier(List<SupplierQuotationResponseDto> supplierQuotationResponse, Guid supplierQuotationId)
    {
        var supplierQuotation = await context.VendorQuotations
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == supplierQuotationId);

        if (supplierQuotation == null)
        {
            return RequisitionErrors.NotFound(supplierQuotationId);
        }

        if (supplierQuotation.Items.Count == 0)
        {
            return Error.Validation("Supplier.Quotation", "No items found for this quotation.");
        }

        foreach (var item in supplierQuotation.Items)
        {
            var response = supplierQuotationResponse.FirstOrDefault(s => s.Id == item.Id);
            if (response != null)
            {
                item.QuotedPrice = response.Price;
            }
        }
        
        supplierQuotation.ReceivedQuotation = true;
        context.VendorQuotations.Update(supplierQuotation);
        context.VendorQuotationItems.UpdateRange(supplierQuotation.Items);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<SupplierQuotationDto>> GetSupplierQuotation(Guid supplierQuotationId)
    {
        var quotation = await context.SupplierQuotations
            .Include(s => s.Items).ThenInclude(s => s.Material)
            .Include(s => s.Items).ThenInclude(s => s.UoM)
            .Include(s => s.Supplier)
            .FirstOrDefaultAsync(s => s.Id == supplierQuotationId);

        if (quotation == null)
        {
            return RequisitionErrors.NotFound(supplierQuotationId);
        }
        return Result<SupplierQuotationDto>.Success(mapper.Map<SupplierQuotationDto>(quotation));
    }

    // --- Open Market Specific ---
    
    public async Task<Result<Paginateable<IEnumerable<MarketRequisitionVendorDto>>>> GetMarketRequisitionVendors(int page, int pageSize, bool complete)
    {
        var query = context.MarketRequisitionVendors
            .Include(mrv => mrv.MarketRequisition).ThenInclude(mr => mr.Item)
            .Include(mrv => mrv.MarketRequisition).ThenInclude(mr => mr.UoM)
            .Where(mrv => mrv.Complete == complete)
            .AsQueryable();

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<MarketRequisitionVendorDto>
        );
    }
    
    public async Task<Result> CreateMarketRequisitionVendor(CreateMarketRequisitionVendor request)
    {
        var marketRequisition = await context.MarketRequisitions.FirstOrDefaultAsync(mr => mr.Id == request.MarketRequisitionId);
        if (marketRequisition is null)
        {
            return RequisitionErrors.NotFound(request.MarketRequisitionId);
        }

        var marketRequisitionVendor = mapper.Map<MarketRequisitionVendor>(request);
        await context.MarketRequisitionVendors.AddAsync(marketRequisitionVendor);
        await context.SaveChangesAsync();
        return Result.Success();
    }
    
    public async Task<Result> ConfirmMarketRequisitionVendor(Guid marketRequisitionVendorId)
    {
        var vendor = await context.MarketRequisitionVendors.FirstOrDefaultAsync(v => v.Id == marketRequisitionVendorId);
        if (vendor is null)
        {
            return RequisitionErrors.NotFound(marketRequisitionVendorId);
        }

        vendor.Complete = true;
        context.MarketRequisitionVendors.Update(vendor);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    // --- Helper methods ---
    private async Task<string> GenerateMemoCode()
    {
        var latestMemo = await context.Memos
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync();

        var latestCode = latestMemo?.Code;
        if (string.IsNullOrEmpty(latestCode))
        {
            return "MEMO-000001";
        }

        var numberPart = int.Parse(latestCode.Split('-')[1]);
        var nextNumber = numberPart + 1;
        return $"MEMO-{nextNumber:D6}";
    }
}*/