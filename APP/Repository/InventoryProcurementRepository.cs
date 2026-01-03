using APP.Extensions;
using APP.IRepository;
using APP.Services.Email;
using APP.Services.Pdf;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.Items.Requisitions;
using DOMAIN.Entities.ItemTransactionLogs;
using DOMAIN.Entities.Memos;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.StockEntries;
using DOMAIN.Entities.VendorQuotations;
using INFRASTRUCTURE.Context;
using MassTransit.Initializers;
using SHARED;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

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
        var requisition = await context.InventoryPurchaseRequisitions
            .AsSplitQuery()
            .Include(inventoryPurchaseRequisition => inventoryPurchaseRequisition.Items)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (requisition == null)
        {
            return RequisitionErrors.NotFound(id);
        }

        context.InventoryPurchaseRequisitionItems.RemoveRange(requisition.Items);

        mapper.Map(request, requisition);

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
            .FirstOrDefaultAsync(r => r.Id == id);

        if (requisition == null)
        {
            return RequisitionErrors.NotFound(id);
        }
        return mapper.Map<InventoryPurchaseRequisitionDto>(requisition);
    }

    public async Task<Result<Paginateable<IEnumerable<InventoryPurchaseRequisitionDto>>>> GetInventoryPurchaseRequisitions(int page, int pageSize,
        string searchQuery, InventoryPurchaseRequisitionStatus status = InventoryPurchaseRequisitionStatus.Pending)
    {
        var query = context.InventoryPurchaseRequisitions
            .AsSplitQuery()
            .Include(r => r.Items)
            .Where(r => r.DeletedAt == null && r.Status == status)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(r => r.Code.Contains(searchQuery)); // Assuming Code is the search field
        }

        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, mapper.Map<InventoryPurchaseRequisitionDto>);
    }


    // --- Sourcing Logic ---

    public async Task<Result> CreateSourceRequisition(
        CreateSourceInventoryRequisition request,
        Guid userId)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var requisition = await context.InventoryPurchaseRequisitions
                .Include(r => r.Items)
                .FirstOrDefaultAsync(r =>
                    r.Id == request.InventoryPurchaseRequisitionId);

            if (requisition is null)
                return RequisitionErrors.NotFound(request.InventoryPurchaseRequisitionId);

            var vendorGroupedItems = request.Items
                .SelectMany(i => i.Vendors.Select(v => new { i, v }))
                .GroupBy(x => x.v.VendorId);

            foreach (var vendorGroup in vendorGroupedItems)
            {
                var vendorId = vendorGroup.Key;

                var sourceRequisition = await context.SourceInventoryRequisitions
                    .Include(sr => sr.Items)
                    .Include(sr => sr.Vendor)
                    .FirstOrDefaultAsync(sr =>
                        sr.VendorId == vendorId &&
                        sr.SentQuotationRequestAt == null);

                if (sourceRequisition == null)
                {
                    sourceRequisition = new SourceInventoryRequisition
                    {
                        InventoryPurchaseRequisitionId = request.InventoryPurchaseRequisitionId,
                        VendorId = vendorId,
                        Items = vendorGroup.Select(x =>
                            new SourceInventoryRequisitionItem
                            {
                                ItemId = x.i.ItemId,
                                UoMId = x.i.UoMId,
                                Quantity = x.i.Quantity
                            }).ToList()
                    };

                    await context.SourceInventoryRequisitions.AddAsync(sourceRequisition);
                }
                else
                {
                    foreach (var gi in vendorGroup)
                    {
                        sourceRequisition.Items.Add(new SourceInventoryRequisitionItem
                        {
                            ItemId = gi.i.ItemId,
                            UoMId = gi.i.UoMId,
                            Quantity = gi.i.Quantity
                        });
                    }
                }

                await context.SaveChangesAsync();

                var sendResult = await SendQuotationToVendor(sourceRequisition.VendorId);

                if (!sendResult.IsSuccess)
                    return sendResult;
            }

            requisition.Status = InventoryPurchaseRequisitionStatus.Complete;
            context.InventoryPurchaseRequisitions.Update(requisition);

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Result.Success();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Result> CreateMarketRequisition(CreateMarketRequisition request, Guid userId)
    {
        if (request.InventoryPurchaseRequisitionItemId == null || request.InventoryPurchaseRequisitionItemId.Count == 0)
            return Error.Validation("Requisition.EmptyItems", "No requisition items provided.");

        var requisitionItems = await context.InventoryPurchaseRequisitionItems
            .Where(item => request.InventoryPurchaseRequisitionItemId.Contains(item.Id))
            .ToListAsync();

        if (requisitionItems.Count != request.InventoryPurchaseRequisitionItemId.Count)
            return Error.NotFound("Requisition.ItemNotFound", "One or more requisition items were not found.");

        var marketRequisitions = new List<MarketRequisition>();

        foreach (var requisitionItem in requisitionItems)
        {
            var marketRequisition = mapper.Map<MarketRequisition>(request);

            // Ensure correct linkage
            marketRequisition.InventoryPurchaseRequisitionItemId = requisitionItem.Id;

            marketRequisitions.Add(marketRequisition);

            requisitionItem.Status = RequestStatus.Sourced;
        }

        await context.MarketRequisitions.AddRangeAsync(marketRequisitions);
        context.InventoryPurchaseRequisitionItems.UpdateRange(requisitionItems);

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

    public async Task<Result<List<VendorPriceComparison>>> GetPriceComparisonOfItem(InventoryRequisitionSource source)
    {
        if (source == InventoryRequisitionSource.TrustedVendor)
        {
            var quotations = await context.VendorQuotationItems
                .Include(s => s.Item)
                .Include(s => s.UoM)
                .Include(s => s.VendorQuotation).ThenInclude(s => s.Vendor)
                .Where(s => s.QuotedPrice != null && s.Status == VendorQuotationItemStatus.NotProcessed)
                .ToListAsync();

            return quotations.GroupBy(s => new { s.Item, s.UoM })
                .Select(item => new VendorPriceComparison
                {
                    Item = mapper.Map<CollectionItemDto>(item.Key.Item),
                    UoM = mapper.Map<UnitOfMeasureDto>(item.Key.UoM),
                    Quantity = item.Select(s => s.Quantity).First(),
                    VendorPrices = item.Select(s => new VendorPrice
                    {
                        Vendor = mapper.Map<CollectionItemDto>(s.VendorQuotation.Vendor),
                        VendorQuotationItem = mapper.Map<VendorQuotationItemDto>(s),
                    }).ToList()
                }).ToList();
        }

        if (source == InventoryRequisitionSource.OpenMarket)
        {
            var openMarketQuotes = await context.MarketRequisitionVendors
                .AsSplitQuery()
                .Include(mrv => mrv.MarketRequisition).ThenInclude(mr => mr.Item)
                .Include(mrv => mrv.MarketRequisition).ThenInclude(mr => mr.UoM)
                .Include(mrv => mrv.TermsOfPayment)
                .Where(mrv => !mrv.Complete)
                .ToListAsync();

            return openMarketQuotes.GroupBy(mrv => new { mrv.MarketRequisition.Item, mrv.MarketRequisition.UoM })
                .Select(itemGroup => new VendorPriceComparison
                {
                    Item = mapper.Map<CollectionItemDto>(itemGroup.Key.Item),
                    UoM = mapper.Map<UnitOfMeasureDto>(itemGroup.Key.UoM),
                    Quantity = itemGroup.First().MarketRequisition.Quantity,
                    VendorPrices = itemGroup.Select(mrv => new VendorPrice
                    {
                        Vendor = new CollectionItemDto { Name = mrv.VendorName },
                        PricePerUnit = mrv.PricePerUnit,
                        VendorName = mrv.VendorName,
                        VendorAddress = mrv.VendorAddress,
                        VendorPhoneNumber = mrv.VendorPhoneNumber,
                        ModeOfPayment = mrv.ModeOfPayment,
                        OpenMarketTermsOfPayment = mapper.Map<CollectionItemDto>(mrv.TermsOfPayment),
                        DeliveryMode = mrv.DeliveryMode,
                        EstimatedDeliveryDate = mrv.EstimatedDeliveryDate,
                    }).ToList()
                }).ToList();
        }

        return new List<VendorPriceComparison>();
    }

    // public async Task<Result> CreateInventoryProformaInvoice(CreateInventoryProformaInvoiceRequest request)
    // {
    //     // return;
    // }

    // --- Memo Creation Logic ---

    public async Task<Result> ProcessOpenMarketMemo(List<CreateMemoItem> memos, Guid userId)
    {
        var memo = new Memo
        {
            Code = await GenerateMemoCode(),
            Items = []
        };

        foreach (var itemRequest in memos)
        {

            if (itemRequest.MarketRequisitionVendorId.HasValue)
            {
                var marketRequisitionVendor = await context.MarketRequisitionVendors
                    .AsSplitQuery()
                    .Include(marketRequisitionVendor => marketRequisitionVendor.MarketRequisition).ThenInclude(mr => mr.Item)
                    .Include(marketRequisitionVendor => marketRequisitionVendor.MarketRequisition).ThenInclude(mr => mr.UoM)
                    .FirstOrDefaultAsync(mrv => mrv.Id == itemRequest.MarketRequisitionVendorId);

                if (marketRequisitionVendor is null)
                    return Error.Validation("MarketRequisitionVendor", $"marketRequisitionVendor with ID {itemRequest.MarketRequisitionVendorId} not found.");


                if (itemRequest.ItemId == marketRequisitionVendor.MarketRequisition.ItemId ||
                    itemRequest.UoMId == marketRequisitionVendor.MarketRequisition.UoMId ||
                    itemRequest.Quantity == marketRequisitionVendor.MarketRequisition.Quantity)
                {
                    return Error.Validation("ItemRequest", "Item request not matching open market requisitions.");
                }


                memo.Items.Add(new MemoItem
                {
                    MarketRequisitionVendorId = marketRequisitionVendor.Id,
                    ItemId = itemRequest.ItemId,
                    UoMId = itemRequest.UoMId,
                    Quantity = itemRequest.Quantity
                });
            }

            if (itemRequest.MarketRequisitionVendorId.HasValue)
            {
                var marketRequisitionVendor = await context.MarketRequisitionVendors
                    .Include(mrv => mrv.MarketRequisition)
                    .FirstOrDefaultAsync(mrv => mrv.Id == itemRequest.MarketRequisitionVendorId);

                if (marketRequisitionVendor is null)
                    return Error.Validation("MarketRequisitionVendor", $"Market requisition vendor with ID {itemRequest.MarketRequisitionVendorId} not found.");

                memo.Items.Add(new MemoItem
                {
                    MarketRequisitionVendorId = marketRequisitionVendor.Id,
                    ItemId = marketRequisitionVendor.MarketRequisition.ItemId,
                    UoMId = marketRequisitionVendor.MarketRequisition.UoMId,
                    Quantity = marketRequisitionVendor.MarketRequisition.Quantity,
                    PricePerUnit = marketRequisitionVendor.PricePerUnit
                });

                marketRequisitionVendor.Complete = true;
                context.MarketRequisitionVendors.Update(marketRequisitionVendor);
            }

        }
        await context.Memos.AddAsync(memo);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> ProcessTrustedVendorMemo(List<CreateMemoItem> memos, Guid userId)
    {
        var memo = new Memo
        {
            Code = await GenerateMemoCode(),
            Items = []
        };

        foreach (var itemRequest in memos)
        {
            var vendorQuotationItem = await context.VendorQuotationItems
                .FirstOrDefaultAsync(sqi => sqi.Id == itemRequest.VendorQuotationItemId);

            if (vendorQuotationItem is null)
                return Error.Validation("VendorQuotationItem", $"Vendor quotation item with ID {itemRequest.VendorQuotationItemId} not found.");

            if (itemRequest.ItemId == vendorQuotationItem.ItemId ||
                itemRequest.UoMId == vendorQuotationItem.UoMId ||
                itemRequest.Quantity == vendorQuotationItem.Quantity)
            {
                return Error.Validation("ItemRequest", "Item request not matching vendor quotation.");
            }

            memo.Items.Add(new MemoItem
            {
                VendorQuotationItemId = vendorQuotationItem.Id,
                ItemId = itemRequest.ItemId,
                UoMId = itemRequest.UoMId,
                Quantity = itemRequest.Quantity,
                PricePerUnit = vendorQuotationItem.QuotedPrice.GetValueOrDefault()
            });

            vendorQuotationItem.Status = VendorQuotationItemStatus.Processed;
            context.VendorQuotationItems.Update(vendorQuotationItem);
        }

        await context.Memos.AddAsync(memo);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    // --- Trusted Vendor Specific ---

    public async Task<Result> SendQuotationToVendor(Guid vendorId)
    {
        var sourceRequisition = await context.SourceInventoryRequisitions
            .Include(sr => sr.Vendor)
            .Include(sr => sr.Items).ThenInclude(item => item.Item)
            .Include(sr => sr.Items).ThenInclude(item => item.UoM)
            .FirstOrDefaultAsync(s => s.VendorId == vendorId && !s.SentQuotationRequestAt.HasValue);

        if (sourceRequisition is null || sourceRequisition.Items.Count == 0)
        {
            return Error.Validation("Vendor.Quotation", "No unsent items found for the specified vendor.");
        }

        var vendorQuotationDto = mapper.Map<VendorQuotationRequest>(sourceRequisition);
        var fileContent = pdfService.GeneratePdfFromHtml(PdfTemplate.QuotationRequestTemplateVendor(vendorQuotationDto));
        var mailAttachments = new List<(byte[] fileContent, string fileName, string fileType)>
        {
            (fileContent, "Quotation Request from Entrance.pdf", "application/pdf")
        };

        try
        {
            emailService.SendMail(sourceRequisition.Vendor.Name, sourceRequisition.Vendor.Email,
                "Sales Quote From Entrance", "Please find attached to this email a sales quote from us.",
                mailAttachments);
        }
        catch (Exception e)
        {
            // Log the exception
            return Error.Validation("Vendor.Quotation", "Failed to send email: " + e.Message);
        }

        sourceRequisition.SentQuotationRequestAt = DateTime.UtcNow;
        context.SourceInventoryRequisitions.Update(sourceRequisition);

        var vendorQuotation = new VendorQuotation
        {
            VendorId = sourceRequisition.VendorId,
            SourceInventoryRequisitionId = sourceRequisition.Id,
            Items = sourceRequisition.Items.Select(i => new VendorQuotationItem
            {
                ItemId = i.ItemId,
                UoMId = i.UoMId,
                Quantity = i.Quantity
            }).ToList()
        };

        await context.VendorQuotations.AddAsync(vendorQuotation);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<Paginateable<IEnumerable<VendorQuotationDto>>>> GetVendorQuotations(int page,
        int pageSize,
        bool received)
    {
        var query = context.VendorQuotations
            .AsSplitQuery()
            .Include(s => s.Items).ThenInclude(s => s.Item)
            .Include(s => s.Items).ThenInclude(s => s.UoM)
            .Include(s => s.Vendor)
            .AsQueryable();

        query = received ? query.Where(s => s.ReceivedQuotation) :
            query.Where(s => !s.ReceivedQuotation);

        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, mapper.Map<VendorQuotationDto>);
    }

    public async Task<Result> ReceiveQuotationFromVendor(
        List<VendorQuotationResponseDto> vendorQuotationResponse,
        Guid vendorQuotationId)
    {
        if (vendorQuotationResponse == null || vendorQuotationResponse.Count == 0)
            return Error.Validation("Vendor.Quotation", "No quotation responses provided.");

        // Get all VendorQuotationItems for this quotation
        var items = await context.VendorQuotationItems
            .Where(vqi => vqi.VendorQuotationId == vendorQuotationId)
            .ToListAsync();

        if (items.Count == 0)
            return Error.Validation("Vendor.Quotation", "No items found for this quotation.");

        var responseLookup = vendorQuotationResponse.ToDictionary(r => r.Id);

        foreach (var item in items)
        {
            if (!responseLookup.TryGetValue(item.ItemId, out var response))
            {
                return Error.Validation(
                    "Vendor.Quotation.MissingItem",
                    $"Missing quoted price for item {item.ItemId}"
                );
            }

            item.QuotedPrice = response.Price;
        }

        var quotation = await context.VendorQuotations
            .FirstOrDefaultAsync(vq => vq.Id == vendorQuotationId);

        if (quotation == null)
            return Error.NotFound("Vendor.Quotation", "Vendor quotation not found.");

        quotation.ReceivedQuotation = true;

        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<VendorQuotationDto>> GetVendorQuotation(Guid vendorQuotationId)
    {
        var quotation = await context.VendorQuotations
            .AsSplitQuery()
            .Include(s => s.Items).ThenInclude(s => s.Item)
            .Include(s => s.Items).ThenInclude(s => s.UoM)
            .Include(s => s.Vendor)
            .FirstOrDefaultAsync(s => s.Id == vendorQuotationId);

        if (quotation == null)
        {
            return RequisitionErrors.NotFound(vendorQuotationId);
        }
        return mapper.Map<VendorQuotationDto>(quotation);
    }

    // --- Open Market Specific ---

    public async Task<Result<Paginateable<IEnumerable<MarketRequisitionVendorDto>>>> GetMarketRequisitionVendors(int page, int pageSize, bool complete)
    {
        var query = context.MarketRequisitionVendors
            .AsSplitQuery()
            .Include(mrv => mrv.MarketRequisition).ThenInclude(mr => mr.Item)
            .Include(mrv => mrv.MarketRequisition).ThenInclude(mr => mr.UoM)
            .Include(m => m.TermsOfPayment)
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
        var marketRequisition = await context.MarketRequisitions
            .FirstOrDefaultAsync(mr => mr.Id == request.MarketRequisitionId);

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
        var vendor = await context.MarketRequisitionVendors
            .FirstOrDefaultAsync(v => v.Id == marketRequisitionVendorId);

        if (vendor is null)
        {
            return RequisitionErrors.NotFound(marketRequisitionVendorId);
        }

        vendor.Complete = true;
        context.MarketRequisitionVendors.Update(vendor);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<List<StockEntryDto>>> GetStockEntries(ApprovalStatus status)
    {
        var stockEntries = await context.StockEntries
            .Where(s => s.Status == status).ToListAsync();
        return mapper.Map<List<StockEntryDto>>(stockEntries);
    }

    public async Task<Result<MemoDto>> GetMemo(Guid id)
    {
        var memo = await context.Memos
            .AsSplitQuery()
            .Include(m => m.Items)
            .ThenInclude(mi => mi.Item)
            .Include(m => m.Items)
            .ThenInclude(mi => mi.UoM)
            .Include(m => m.Items)
            .ThenInclude(mi => mi.VendorQuotationItem)
            .ThenInclude(vqi => vqi.VendorQuotation)
            .Include(m => m.Items)
            .ThenInclude(mi => mi.MarketRequisitionVendor)
            .ThenInclude(mrv => mrv.MarketRequisition)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (memo == null)
            return Error.NotFound("Memo", "Memo not found");

        return mapper.Map<MemoDto>(memo);
    }


    public async Task<Result<Paginateable<IEnumerable<MemoDto>>>> GetMemos(int page, int pageSize, string searchQuery = null)
    {
        var query = context.Memos
            .AsSplitQuery()
            .Include(m => m.Items)
            .ThenInclude(mi => mi.Item)
            .Include(m => m.Items)
            .ThenInclude(mi => mi.UoM)
            .Include(m => m.Items)
            .ThenInclude(mi => mi.VendorQuotationItem)
            .ThenInclude(vqi => vqi.VendorQuotation)
            .Include(m => m.Items)
            .ThenInclude(mi => mi.MarketRequisitionVendor)
            .ThenInclude(mrv => mrv.MarketRequisition)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, m => m.Code);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query.OrderByDescending(m => m.CreatedAt),
            page,
            pageSize,
            mapper.Map<MemoDto>
        );
    }

    public async Task<Result> MarkMemoItemAsPaid(Guid memoItemId, DateTime? purchasedAt = null)
    {
        var memoItem = await context.MemoItems
            .AsSplitQuery()
            .Include(memoItem => memoItem.Memo)
            .FirstOrDefaultAsync(m => m.Id == memoItemId);

        if (memoItem == null)
            return Error.NotFound("Memo", "Memo item not found.");

        if (memoItem.PurchasedAt.HasValue)
            return Error.Validation("Memo", "Memo item is already marked as paid.");

        memoItem.PurchasedAt = purchasedAt ?? DateTime.UtcNow;
        context.MemoItems.Update(memoItem);
        await context.SaveChangesAsync();

        var stockEntry = new StockEntry
        {
            ItemId = memoItem.ItemId,
            MemoId = memoItem.MemoId,
            Quantity = memoItem.Quantity
        };

        await context.StockEntries.AddAsync(stockEntry);
        await context.SaveChangesAsync();

        var allItemsPaid = await context.MemoItems
            .Where(mi => mi.MemoId == memoItem.MemoId)
            .AllAsync(mi => mi.PurchasedAt.HasValue || mi.Id == memoItemId);

        if (allItemsPaid && !memoItem.Memo.Paid)
        {
            memoItem.Memo.Paid = true;
        }

        context.MemoItems.Update(memoItem);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> UploadStockItems(ImportItemsRequest itemsRequest)
    {
        var file = itemsRequest.ItemFile;
        const string memoCode = "MEMO-00256001";

        if (file == null || file.Length == 0)
            return Error.Validation(
                "ItemsUpload.EmptyFile",
                "No file uploaded.");

        var extension = Path.GetExtension(file.FileName);
        if (extension != ".xlsx" && extension != ".xls")
            return Error.Validation(
                "ItemsUpload.InvalidFileType",
                "Only .xlsx or .xls files are allowed.");

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);

        ExcelPackage.License.SetNonCommercialPersonal("Oryx");

        using var package = new ExcelPackage(stream);
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();

        if (worksheet?.Dimension == null || worksheet.Dimension.End.Row < 2)
            return Error.Validation(
                "ItemsUpload.Empty",
                "The uploaded Excel file contains no data.");

        var itemsLookup = await context.Items
            .ToDictionaryAsync(i => i.Code, i => i.Id);

        var memosLookup = await context.Memos
            .ToDictionaryAsync(m => m.Code, m => m.Id);

        var errors = new List<string>();
        var stockEntries = new List<StockEntry>();

        var lastRow = worksheet.Dimension.End.Row;
        while (lastRow >= 2 && string.IsNullOrWhiteSpace(worksheet.Cells[lastRow, 1].Text))
            lastRow--;

        for (var row = 2; row <= lastRow; row++)
        {
            var itemCode = worksheet.Cells[row, 3].Text?.Trim();
            var quantityText = worksheet.Cells[row, 4].Text?.Trim();

            if (string.IsNullOrWhiteSpace(itemCode))
            {
                errors.Add($"Row {row}:Item Code is required.");
                continue;
            }

            if (!itemsLookup.TryGetValue(itemCode, out var itemId))
            {
                errors.Add($"Row {row}: Item with code '{itemCode}' does not exist.");
                continue;
            }

            if (!memosLookup.TryGetValue(memoCode, out var memoId))
            {
                errors.Add($"Row {row}: Memo with code '{memoCode}' does not exist.");
                continue;
            }

            if (!int.TryParse(quantityText, out var quantity) || quantity <= 0)
            {
                errors.Add($"Row {row}: Quantity must be a number greater than 0.");
                continue;
            }

            stockEntries.Add(new StockEntry
            {
                ItemId = itemId,
                MemoId = memoId,
                Quantity = quantity
            });
        }

        if (errors.Count != 0)
        {
            return Error.Validation(
                "ItemsUpload.ValidationErrors",
                string.Join(Environment.NewLine, errors));
        }

        if (stockEntries.Count == 0)
        {
            return Error.Validation(
                "ItemsUpload.NoneAdded",
                "No valid stock entries were found in the file.");
        }

        await context.StockEntries.AddRangeAsync(stockEntries);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> ApproveItem(Guid stockEntryId)
    {
        var stockEntry = await context.StockEntries
            .Include(stockEntry => stockEntry.Item)
            .FirstOrDefaultAsync(s => s.Id == stockEntryId);
        if (stockEntry == null) return Error.NotFound("StockEntry", "Stock entry not found.");

        stockEntry.Status = ApprovalStatus.Approved;
        context.StockEntries.Update(stockEntry);
        await context.SaveChangesAsync();

        var lastTransaction = await context.ItemTransactionLogs
            .OrderByDescending(i => i.CreatedAt)
            .FirstOrDefaultAsync(i => i.ItemCode == stockEntry.Item.Code);

        var newTransaction = new ItemTransactionLog
        {
            Id = Guid.NewGuid(),
            ItemCode = stockEntry.Item.Code,
            Credit = stockEntry.Quantity,
            TransactionType = "Stock Entry",
            Debit = 0,
            TotalBalance = (lastTransaction?.TotalBalance ?? 0) + stockEntry.Quantity
        };

        await context.ItemTransactionLogs.AddAsync(newTransaction);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> RejectItem(Guid stockEntryId)
    {
        var stockEntry = await context.StockEntries.FirstOrDefaultAsync(s => s.Id == stockEntryId);
        if (stockEntry == null) return Error.NotFound("StockEntry", "Stock entry not found.");

        stockEntry.Status = ApprovalStatus.Rejected;
        context.StockEntries.Update(stockEntry);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    // --- Helper methods ---
    public async Task<string> GenerateMemoCode()
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
}