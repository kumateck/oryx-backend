using APP.Services.Documents;
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
using SHARED;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace APP.Repository;

public partial class InventoryProcurementRepository(
    ApplicationDbContext context,
    IMapper mapper,
    IPdfService pdfService,
    IEmailService emailService)
    : IInventoryProcurementRepository
{
    // --- CRUD for InventoryPurchaseRequisition ---

    // --- Sourcing Logic ---

    public async Task<Result<Paginateable<IEnumerable<MarketRequisitionDto>>>> GetMarketRequisitions(int page, int pageSize, bool? closed = null)
    {
        var query = context.MarketRequisitions.AsSplitQuery()
            .Include(mr => mr.Item).Include(mr => mr.UoM)
            .Include(mr => mr.InventoryPurchaseRequisitionItem).ThenInclude(i => i.InventoryPurchaseRequisition)
            .AsQueryable();
        if (closed.HasValue)
            query = query.Where(mr => context.MarketRequisitionVendors.Any(v => v.MarketRequisitionId == mr.Id && v.Complete) == closed.Value);
        var result = await PaginationHelper.GetPaginatedResultAsync(query.OrderByDescending(mr => mr.CreatedAt), page, pageSize, mapper.Map<MarketRequisitionDto>);
        // The page's rows are a deferred projection; materialize them so the added details are what is returned.
        var rows = result.Data.ToList();
        await DescribeMarket(rows);
        result.Data = rows;
        return result;
    }

    private async Task DescribeMarket(List<MarketRequisitionDto> rows)
    {
        var ids = rows.Select(r => r.Id).ToList();
        var vendors = await context.MarketRequisitionVendors.Where(v => ids.Contains(v.MarketRequisitionId))
            .GroupBy(v => v.MarketRequisitionId).Select(g => new { g.Key, Count = g.Count(), Closed = g.Any(v => v.Complete) }).ToListAsync();
        var requisitions = await context.MarketRequisitions.Where(m => ids.Contains(m.Id))
            .Select(m => new { m.Id, m.InventoryPurchaseRequisitionItem.InventoryPurchaseRequisitionId, m.InventoryPurchaseRequisitionItem.InventoryPurchaseRequisition.Code }).ToListAsync();
        foreach (var row in rows)
        {
            var v = vendors.FirstOrDefault(x => x.Key == row.Id);
            row.VendorCount = v?.Count ?? 0;
            row.Closed = v?.Closed ?? false;
            var r = requisitions.FirstOrDefault(x => x.Id == row.Id);
            row.RequisitionCode = r?.Code;
            row.InventoryPurchaseRequisitionId = r?.InventoryPurchaseRequisitionId;
        }
    }

    public async Task<Result<List<VendorPriceComparison>>> GetPriceComparisonOfItem(InventoryRequisitionSource source)
    {
        if (source == InventoryRequisitionSource.TrustedVendor)
        {
            var quotations = await context.VendorQuotationItems
                .AsSplitQuery()
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
                        Vendor = new CollectionItemDto { Id = mrv.Id, Name = mrv.VendorName },
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

    // --- Trusted Vendor Specific ---

    public async Task<Result> SendQuotationToVendor(Guid vendorId)
    {
        var sourceRequisition = await context.SourceInventoryRequisitions
            .AsSplitQuery()
            .Include(sr => sr.Vendor).ThenInclude(v => v.Currency)
            .Include(sr => sr.InventoryPurchaseRequisition)
            .Include(sr => sr.Items).ThenInclude(item => item.Item)
            .Include(sr => sr.Items).ThenInclude(item => item.UoM)
            .FirstOrDefaultAsync(s => s.VendorId == vendorId && !s.SentQuotationRequestAt.HasValue);

        if (sourceRequisition is null || sourceRequisition.Items.Count == 0)
        {
            return Error.Validation("Vendor.Quotation", 
                "No unsent items found for the specified vendor.");
        }

        var vendorQuotationDto = mapper.Map<VendorQuotationRequest>(sourceRequisition);
        // The same document the desktop app prints for this request, in the body and as a PDF.
        var email = ProcurementDocuments.VendorQuotationRequest(vendorQuotationDto,
            sourceRequisition.InventoryPurchaseRequisition?.Code);

        try
        {
            emailService.SendMail(sourceRequisition.Vendor.Name, sourceRequisition.Vendor.Email,
                email.Subject, email.RenderBody(), email.Attachments(pdfService));
        }
        catch (Exception e)
        {
            // Log the exception
            return Error.Validation("Vendor.Quotation", "Failed to send email: " + e.Message);
        }

        sourceRequisition.SentQuotationRequestAt = DateTime.UtcNow;
        context.SourceInventoryRequisitions.Update(sourceRequisition);

        var vendorQuotation = await context.VendorQuotations
            .Include(vq => vq.Items)
            .FirstOrDefaultAsync(vq => vq.SourceInventoryRequisitionId == sourceRequisition.Id);

        if (vendorQuotation is null)
        {
            vendorQuotation = new VendorQuotation
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
        }

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

    public async Task<Result<Paginateable<IEnumerable<MarketRequisitionVendorDto>>>> GetMarketRequisitionVendors(int page, int pageSize, bool complete, Guid? marketRequisitionId = null)
    {
        var query = context.MarketRequisitionVendors
            .AsSplitQuery()
            .Include(mrv => mrv.MarketRequisition).ThenInclude(mr => mr.Item)
            .Include(mrv => mrv.MarketRequisition).ThenInclude(mr => mr.UoM)
            .Include(m => m.TermsOfPayment)
            .Where(mrv => marketRequisitionId.HasValue ? mrv.MarketRequisitionId == marketRequisitionId : mrv.Complete == complete)
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
        var marketRequisitionExists = await context.MarketRequisitions
            .AnyAsync(mr => mr.Id == request.MarketRequisitionId);

        if (!marketRequisitionExists)
        {
            return RequisitionErrors.NotFound(request.MarketRequisitionId);
        }

        if (await context.MarketRequisitionVendors.AnyAsync(v => v.MarketRequisitionId == request.MarketRequisitionId && v.Complete))
        {
            return Error.Validation("MarketRequisition.Complete", "A vendor has already been chosen for this open-market item.");
        }

        if (request.Vendors == null || request.Vendors.Count == 0)
        {
            return Error.Validation(
                "Vendors.Empty",
                "At least one vendor must be provided."
            );
        }
        
        var today = DateTime.UtcNow.Date;
        
        foreach (var vendor in request.Vendors)
        {
            if (vendor.PricePerUnit <= 0)
            {
                return Error.Validation(
                    "Vendor.InvalidPrice",
                    $"Price per unit must be greater than zero for vendor '{vendor.VendorName}'."
                );
            }
        }
        
        var duplicateInRequest = request.Vendors
            .GroupBy(v => v.VendorName.Trim().ToLower())
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicateInRequest != null)
        {
            return Error.Validation(
                "Vendor.DuplicateInRequest",
                $"Vendor '{duplicateInRequest.Key}' appears more than once in the request."
            );
        }
        
        var existingVendorNames = await context.MarketRequisitionVendors
            .Where(v => v.MarketRequisitionId == request.MarketRequisitionId)
            .Select(v => v.VendorName.ToLower())
            .ToListAsync();

        var duplicateExistingVendor = request.Vendors
            .FirstOrDefault(v => existingVendorNames.Contains(v.VendorName.ToLower()));

        if (duplicateExistingVendor != null)
        {
            return Error.Validation(
                "Vendor.AlreadyExists",
                $"Vendor '{duplicateExistingVendor.VendorName}' already exists for this market requisition."
            );
        }
        
        var entities = request.Vendors.Select(vendor =>
        {
            var entity = mapper.Map<MarketRequisitionVendor>(vendor);
            entity.MarketRequisitionId = request.MarketRequisitionId;
            return entity;
        }).ToList();

        await context.MarketRequisitionVendors.AddRangeAsync(entities);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<Paginateable<IEnumerable<StockEntryDto>>>> GetStockEntries(ApprovalStatus status, int page, int pageSize, Store store)
    {
        var query = context.StockEntries
            .Include(s => s.Item).ThenInclude(i => i.UnitOfMeasure)
            .Include(s => s.Item).ThenInclude(i => i.ItemCategory)
            .Include(s => s.Memo)
            .Include(s => s.CreatedBy)
            .Where(s => s.Status == status && s.Item.Store == store).AsQueryable();
        return await PaginationHelper
            .GetPaginatedResultAsync(
            query, page, pageSize, mapper.Map<StockEntryDto>);
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


    public async Task<Result<Paginateable<IEnumerable<MemoDto>>>> GetMemos(int page, 
        int pageSize, 
        string searchQuery = null,
        MemoStatus? status = null)
    {
        var query = context.Memos
            .AsSplitQuery()
            .Include(m => m.CreatedBy)
            .Include(m => m.Items)
            .ThenInclude(mi => mi.Item)
            .Include(m => m.Items)
            .ThenInclude(mi => mi.UoM)
            .Include(m => m.Items)
            .ThenInclude(mi => mi.VendorQuotationItem)
            .ThenInclude(vqi => vqi.VendorQuotation)
            .ThenInclude(vq => vq.Vendor)
            .Include(m => m.Items)
            .ThenInclude(mi => mi.MarketRequisitionVendor)
            .ThenInclude(mrv => mrv.MarketRequisition)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, m => m.Code);
        }

        if (status != null)
        {
            query = query.Where(m => m.Status == status);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query.OrderByDescending(m => m.CreatedAt),
            page,
            pageSize,
            mapper.Map<MemoDto>
        );
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

    public async Task<Result<MarketRequisitionDto>> GetMarketRequisition(Guid id)
    {
        var marketRequisition = await context.MarketRequisitions.AsSplitQuery()
            .Include(mr => mr.Item).Include(mr => mr.UoM)
            .Include(mr => mr.InventoryPurchaseRequisitionItem).ThenInclude(i => i.InventoryPurchaseRequisition)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (marketRequisition is null) return Error.NotFound("MarketRequisition", "Market requisition not found.");
        var dto = mapper.Map<MarketRequisitionDto>(marketRequisition);
        await DescribeMarket([dto]);
        return dto;
    }
}