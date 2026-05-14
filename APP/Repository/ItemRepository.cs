using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.ItemShipments;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.ItemTransactionLogs;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.PurchaseOrders.Request;
using DOMAIN.Entities.Shipments;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using SHARED;

namespace APP.Repository;

public class ItemRepository(ApplicationDbContext context, IMapper mapper) : IItemRepository
{
    public async Task<Result<Guid>> CreateItem(CreateItemsRequest request)
    {
        var item = await context.Items
            .FirstOrDefaultAsync(i => (i.Code == request.Code || i.Name == request.Name)
                                      && i.Store == request.Store);
        if (item != null) return Error.Validation("Item.Exists", "Item already exists for this department");

        if (request.ItemCategoryId.HasValue)
        {
            var itemCategory = await context.ItemCategories.FirstOrDefaultAsync(ic => ic.Id == request.ItemCategoryId);
            if (itemCategory == null) return Error.NotFound("ItemCategory.NotFound", "Item category not found");

        }

        item = mapper.Map<Item>(request);
        await context.Items.AddAsync(item);
        await context.SaveChangesAsync();
        return item.Id;
    }

    public async Task<Result> UploadItems(ImportItemsRequest itemsRequest)
    {
        var file = itemsRequest.ItemFile;

        if (file == null || file.Length == 0)
            return Error.Validation("ItemsUpload.EmptyFile", "No file uploaded.");

        var extension = Path.GetExtension(file.FileName);
        if (extension != ".xlsx" && extension != ".xls")
        {
            return Error.Validation(
                "ItemsUpload.InvalidFileType",
                "Invalid file type. Only .xlsx or .xls files are allowed."
            );
        }

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);

        ExcelPackage.License.SetNonCommercialPersonal("Oryx");

        using var package = new ExcelPackage(stream);
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();

        if (worksheet?.Dimension == null || worksheet.Dimension.End.Row < 2)
        {
            return Error.Validation(
                "ItemsFile.Empty",
                "The uploaded Excel file is empty or does not contain any records."
            );
        }

        var uomLookup = await context.UnitOfMeasures
            .Select(u => new
            {
                u.Id,
                Key = $"{u.Name}|{u.Symbol}"
            })
            .ToDictionaryAsync(x => x.Key, x => x.Id);

        var categoryLookup = await context.ItemCategories
            .Select(c => new { c.Id, c.Name })
            .ToDictionaryAsync(c => c.Name, c => c.Id);

        var existingItemCodes = await context.Items
            .Select(i => i.Code)
            .ToHashSetAsync();

        var itemsToUpload = new List<Item>();

        var lastRow = worksheet.Dimension.End.Row;
        while (lastRow >= 2 && string.IsNullOrWhiteSpace(worksheet.Cells[lastRow, 1].Text))
        {
            lastRow--;
        }

        for (var row = 2; row <= lastRow; row++)
        {
            var storeText = worksheet.Cells[row, 1].Text?.Replace(" ", "").Trim();
            var itemName = worksheet.Cells[row, 2].Text?.Trim();
            var itemCode = worksheet.Cells[row, 3].Text?.Trim();
            var categoryName = worksheet.Cells[row, 4].Text?.Trim();
            var uomText = worksheet.Cells[row, 5].Text?.Trim();
            var classificationText = worksheet.Cells[row, 6].Text?.Replace(" ", "").Trim();
            var minText = worksheet.Cells[row, 7].Text?.Trim();
            var reorderText = worksheet.Cells[row, 8].Text?.Trim();
            var maxText = worksheet.Cells[row, 9].Text?.Trim();

            if (string.IsNullOrWhiteSpace(storeText) ||
                string.IsNullOrWhiteSpace(itemCode) ||
                string.IsNullOrWhiteSpace(uomText) ||
                string.IsNullOrWhiteSpace(classificationText))
            {
                return Error.Validation(
                    "ItemUpload.MissingFields",
                    $"Missing required fields at row {row}."
                );
            }

            if (!TryParseStrictEnum(storeText, out Store store))
            {
                return Error.Validation(
                    "ItemUpload.InvalidStore",
                    $"Invalid store '{storeText}' at row {row}."
                );
            }

            if (!TryParseStrictEnum(classificationText, out InventoryClassification classification))
            {
                return Error.Validation(
                    "ItemUpload.InvalidClassification",
                    $"Invalid classification '{classificationText}' at row {row}."
                );
            }

            if (existingItemCodes.Contains(itemCode))
                continue;

            if (!uomText.Contains('(') || !uomText.EndsWith(')'))
            {
                return Error.Validation(
                    "ItemUpload.InvalidUoMFormat",
                    $"Invalid Unit of Measure format '{uomText}' at row {row}."
                );
            }

            var name = uomText[..uomText.IndexOf('(')].Trim();
            var symbol = uomText[(uomText.IndexOf('(') + 1)..^1].Trim();
            var uomKey = $"{name}|{symbol}";

            if (!uomLookup.TryGetValue(uomKey, out var uomId))
            {
                return Error.Validation(
                    "ItemUpload.InvalidUoM",
                    $"Unit of Measure '{uomText}' not found at row {row}."
                );
            }

            if (string.IsNullOrWhiteSpace(categoryName))
            {
                return Error.Validation("ItemUpload.MissingCategory", $"Item category is required at row {row}.");
            }
            
            if (!categoryLookup.TryGetValue(categoryName, out var categoryId))
            {
                return Error.Validation("Category.NotFound", $"Item category '{categoryName}' not found at row {row}.");
            }

            if (!int.TryParse(minText, out var minLevel) ||
                !int.TryParse(reorderText, out var reorderLevel) ||
                !int.TryParse(maxText, out var maxLevel))
            {
                return Error.Validation(
                    "ItemUpload.InvalidLevels",
                    $"Invalid inventory levels at row {row}. Levels must be integers."
                );
            }

            if (minLevel < 0 || reorderLevel < 0 || maxLevel < 0)
            {
                return Error.Validation(
                    "ItemUpload.InvalidLevels",
                    $"Inventory levels cannot be negative at row {row}."
                );
            }

            itemsToUpload.Add(new Item
            {
                Name = itemName,
                Store = store,
                Code = itemCode,
                Classification = classification,
                ItemCategoryId = categoryId,
                MinimumLevel = minLevel,
                ReorderLevel = reorderLevel,
                MaximumLevel = maxLevel,
                UnitOfMeasureId = uomId
            });

            existingItemCodes.Add(itemCode);
        }

        if (itemsToUpload.Count == 0)
        {
            return Error.Validation(
                "ItemsUpload.NoneAdded",
                "No new items were uploaded."
            );
        }

        await context.Items.AddRangeAsync(itemsToUpload);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    private static bool TryParseStrictEnum<TEnum>(string value, out TEnum result)
        where TEnum : struct, Enum
    {
        return Enum.TryParse(value, true, out result)
               && Enum.IsDefined(result);
    }

    public async Task<Result<Paginateable<IEnumerable<ItemDto>>>> GetItems(
        int page,
        int pageSize,
        string searchQuery,
        Store? store)
    {
        var query = context.Items
            .AsNoTracking()
            .Include(i => i.UnitOfMeasure)
            .Include(i => i.ItemCategory)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(
                searchQuery,
                i => i.Name,
                i => i.Code
            );
        }

        if (store.HasValue)
        {
            query = query.Where(i => i.Store == store.Value);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            entity => mapper.Map<ItemDto>(
                entity,
                opts => opts.Items[AppConstants.ModelType] = nameof(Item)
            )
        );
    }

    public async Task<Result<ItemDto>> GetItem(Guid id)
    {
        var item = await context.Items
            .Include(i => i.UnitOfMeasure)
            .Include(i => i.ItemCategory)
            .FirstOrDefaultAsync(i => i.Id == id);
        return item is null ?
            Error.NotFound("Item.NotFound", "Item not found") :
            mapper.Map<ItemDto>(item,
                opts => opts.Items[AppConstants.ModelType] = nameof(Item));
    }


    public async Task<Result> UpdateItem(Guid id, CreateItemsRequest request)
    {
        var item = await context.Items.FirstOrDefaultAsync(i => i.Id == id);
        if (item == null) return Error.NotFound("Item.NotFound", "Item not found");

        mapper.Map(request, item);
        context.Items.Update(item);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteItem(Guid id, Guid userId)
    {
        var item = await context.Items.FirstOrDefaultAsync(i => i.Id == id);
        if (item == null) return Error.NotFound("Item.NotFound", "Item not found");

        item.DeletedAt = DateTime.UtcNow;
        item.LastDeletedById = userId;
        item.IsActive = false;

        context.Items.Update(item);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<List<ItemTransactionLogDto>>> GetItemTransactions(
        string itemCode,
        string transactionType)

    {
        var query = context.ItemTransactionLogs
            .Where(i => i.ItemCode == itemCode);

        if (Enum.TryParse<TransactionType>(transactionType, out var transaction))
        {
            query = query.Where(i => i.TransactionType == transaction);
        }
        var transactions = await query
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        var result = mapper.Map<List<ItemTransactionLogDto>>(transactions);

        return Result.Success(result);
    }

    // ************* Item Shipment Invoice *************

    public async Task<Result<Guid>> CreateItemShipmentInvoice(CreateItemShipmentInvoice request, Guid userId)
    {
        if (await context.ItemShipmentInvoices.AnyAsync(i => i.Code == request.Code))
        {
            return Error.Validation("Code", "Item shipment invoice code already exists.");
        }

        if (request.CurrencyId.HasValue)
        {
            var exists = await context.Currencies.AnyAsync(i => i.Id == request.CurrencyId.Value);
            if (!exists) return Error.Validation("Currency.NotFound", "Currency not found.");
        }

        if (request.VendorId.HasValue)
        {
            var exists = await context.Vendors.AnyAsync(i => i.Id == request.VendorId.Value);
            if (!exists) return Error.Validation("Vendor.NotFound", "Supplier not found.");
        }

        foreach (var item in request.Items)
        {
            var purchaseOrderExists = await context.PurchaseOrders.AnyAsync(i => i.Id == item.PurchaseOrderId);
            if (!purchaseOrderExists) return Error.Validation("PurchaseOrder.NotFound", "PurchaseOrder not found.");
        }

        var invoice = mapper.Map<ItemShipmentInvoice>(request);
        invoice.CreatedById = userId;
        await context.ItemShipmentInvoices.AddAsync(invoice);
        await context.SaveChangesAsync();
        return invoice.Id;
    }

    public async Task<Result<ItemShipmentInvoiceDto>> GetItemShipmentInvoice(Guid invoiceId)
    {
        var invoice = await context.ItemShipmentInvoices
            .AsSplitQuery()
            .Include(si => si.Items)
                .ThenInclude(item => item.Item)
            .Include(si => si.Items)
                .ThenInclude(item => item.UoM)
            .Include(si => si.Items)
                .ThenInclude(item => item.Currency)
            .Include(si => si.Vendor)
            .Include(si => si.Currency)
            .FirstOrDefaultAsync(si => si.Id == invoiceId);

        return invoice is null
            ? Error.NotFound("ItemShipmentInvoice.NotFound", "Item shipment invoice not found")
            : mapper.Map<ItemShipmentInvoiceDto>(invoice);
    }

    public async Task<Result<Paginateable<IEnumerable<ItemShipmentInvoiceDto>>>> GetItemShipmentInvoices(int page, int pageSize, string searchQuery)
    {
        var query = context.ItemShipmentInvoices
            .AsSplitQuery()
            .Include(si => si.Items)
                .ThenInclude(item => item.Item)
            .Include(si => si.Items)
                .ThenInclude(item => item.UoM)
            .Include(si => si.Vendor)
            .Include(si => si.Currency)
            .OrderByDescending(s => s.CreatedAt)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, bs => bs.Code);
        }

        var paginatedResult = await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize);
        var invoices = await paginatedResult.Data.ToListAsync();

        return new Paginateable<IEnumerable<ItemShipmentInvoiceDto>>
        {
            Data = mapper.Map<IEnumerable<ItemShipmentInvoiceDto>>(invoices),
            PageIndex = page,
            PageCount = paginatedResult.PageCount,
            TotalRecordCount = paginatedResult.TotalRecordCount,
            StartPageIndex = paginatedResult.StartPageIndex,
            StopPageIndex = paginatedResult.StopPageIndex
        };
    }

    public async Task<Result<IEnumerable<ItemShipmentInvoiceDto>>> GetUnattachedItemShipmentInvoices()
    {
        var unattached = await context.ItemShipmentInvoices
            .AsSplitQuery()
            .Where(si => !context.ItemShipmentDocuments.Any(sd => sd.ItemShipmentInvoiceId == si.Id))
            .Include(si => si.Items)
                .ThenInclude(item => item.Item)
            .Include(si => si.Items)
                .ThenInclude(item => item.UoM)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        return mapper.Map<List<ItemShipmentInvoiceDto>>(unattached);
    }

    public async Task<Result> UpdateItemShipmentInvoice(CreateItemShipmentInvoice request, Guid invoiceId, Guid userId)
    {
        var existing = await context.ItemShipmentInvoices
            .Include(si => si.Items)
            .FirstOrDefaultAsync(si => si.Id == invoiceId);
        if (existing is null)
        {
            return Error.NotFound("ItemShipmentInvoice.NotFound", "Item shipment invoice not found");
        }
        
        if (request.CurrencyId.HasValue)
        {
            var exists = await context.Currencies.AnyAsync(i => i.Id == request.CurrencyId.Value);
            if (!exists) return Error.Validation("Currency.NotFound", "Currency not found.");
        }

        if (request.VendorId.HasValue)
        {
            var exists = await context.Suppliers.AnyAsync(i => i.Id == request.VendorId.Value);
            if (!exists) return Error.Validation("SupplierId.NotFound", "Supplier not found.");
        }
        
        foreach (var item in request.Items)
        {
            var purchaseOrderExists = await context.PurchaseOrders.AnyAsync(i => i.Id == item.PurchaseOrderId);
            if (!purchaseOrderExists) return Error.Validation("PurchaseOrder.NotFound", "PurchaseOrder not found.");
        }

        mapper.Map(request, existing);
        existing.LastUpdatedById = userId;

        context.ItemShipmentInvoices.Update(existing);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> MarkItemShipmentInvoiceAsPaid(Guid invoiceId, DateTime? paidAt, Guid userId)
    {
        var existing = await context.ItemShipmentInvoices
            .Include(si => si.Items)
            .FirstOrDefaultAsync(si => si.Id == invoiceId);
        if (existing is null)
        {
            return Error.NotFound("ItemShipmentInvoice.NotFound", "Item shipment invoice not found");
        }

        var existingBillingSheet = await context.ItemBillingSheets
            .FirstOrDefaultAsync(bs => bs.InvoiceId == invoiceId);
        if (existingBillingSheet is not null)
        {
            existingBillingSheet.Status = BillingSheetStatus.Paid;
            context.ItemBillingSheets.Update(existingBillingSheet);
        }

        existing.PaidAt = paidAt ?? DateTime.UtcNow;
        existing.LastUpdatedById = userId;
        context.ItemShipmentInvoices.Update(existing);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteItemShipmentInvoice(Guid invoiceId, Guid userId)
    {
        var invoice = await context.ItemShipmentInvoices.FirstOrDefaultAsync(si => si.Id == invoiceId);
        if (invoice is null)
        {
            return Error.NotFound("ItemShipmentInvoice.NotFound", "Item shipment invoice not found");
        }

        invoice.DeletedAt = DateTime.UtcNow;
        invoice.LastDeletedById = userId;

        context.ItemShipmentInvoices.Update(invoice);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    // ************* Item Shipment Document *************

    public async Task<Result<Guid>> CreateItemShipmentDocument(CreateItemShipmentDocumentRequest request, Guid userId)
    {
        var shipmentDocument = mapper.Map<ItemShipmentDocument>(request);

        if (request.ItemShipmentInvoiceId.HasValue)
        {
            var exists = await context.ItemShipmentInvoices.AnyAsync(i => i.Id == request.ItemShipmentInvoiceId.Value);
            if (!exists) return Error.Validation("ItemShipmentInvoice.NotFound", "Item shipment invoice not found");
        }
        
        shipmentDocument.Type = DocType.Shipment;
        shipmentDocument.Status = ShipmentStatus.New;
        shipmentDocument.CreatedById = userId;
        await context.ItemShipmentDocuments.AddAsync(shipmentDocument);
        await context.SaveChangesAsync();
        return shipmentDocument.Id;
    }

    public async Task<Result<ItemShipmentDocumentDto>> GetItemShipmentDocument(Guid shipmentDocumentId)
    {
        var shipmentDocument = await context.ItemShipmentDocuments
            .AsSplitQuery()
            .Include(s => s.ItemShipmentInvoice)
                .ThenInclude(s => s.Items)
            .FirstOrDefaultAsync(bs => bs.Id == shipmentDocumentId);

        return shipmentDocument is null
            ? Error.NotFound("ItemShipmentDocument.NotFound", "Item shipment document not found")
            : mapper.Map<ItemShipmentDocumentDto>(shipmentDocument);
    }

    public async Task<Result<Paginateable<IEnumerable<ItemShipmentDocumentDto>>>> GetItemShipmentDocuments(int page, int pageSize, string searchQuery, bool? onlyApproved)
    {
        var query = context.ItemShipmentDocuments
            .AsSplitQuery()
            .Include(s => s.ItemShipmentInvoice)
            .Where(s => s.Type == DocType.Shipment)
            .OrderByDescending(s => s.CreatedAt)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, bs => bs.Code);
        }

        if (onlyApproved.HasValue)
        {
            if (onlyApproved.Value)
            {
                query = query.Where(q => q.Approved);
            }
        }

        var paginatedResult = await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize);
        var shipmentDocuments = await paginatedResult.Data.ToListAsync();

        return new Paginateable<IEnumerable<ItemShipmentDocumentDto>>
        {
            Data = mapper.Map<IEnumerable<ItemShipmentDocumentDto>>(shipmentDocuments),
            PageIndex = page,
            PageCount = paginatedResult.PageCount,
            TotalRecordCount = paginatedResult.TotalRecordCount,
            StartPageIndex = paginatedResult.StartPageIndex,
            StopPageIndex = paginatedResult.StopPageIndex
        };
    }

    public async Task<Result> UpdateItemShipmentDocument(CreateItemShipmentDocumentRequest request, Guid shipmentDocumentId, Guid userId)
    {
        var existing = await context.ItemShipmentDocuments.FirstOrDefaultAsync(bs => bs.Id == shipmentDocumentId && bs.Type == DocType.Shipment);
        if (existing is null)
        {
            return Error.NotFound("ItemShipmentDocument.NotFound", "Item shipment document not found");
        }
        
        if (request.ItemShipmentInvoiceId.HasValue)
        {
            var exists = await context.ItemShipmentInvoices.AnyAsync(i => i.Id == request.ItemShipmentInvoiceId.Value);
            if (!exists) return Error.Validation("ItemShipmentInvoice.NotFound", "Item shipment invoice not found");
        }

        mapper.Map(request, existing);
        existing.LastUpdatedById = userId;

        context.ItemShipmentDocuments.Update(existing);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteItemShipmentDocument(Guid shipmentDocumentId, Guid userId)
    {
        var shipmentDocument = await context.ItemShipmentDocuments.FirstOrDefaultAsync(bs => bs.Id == shipmentDocumentId && bs.Type == DocType.Shipment);
        if (shipmentDocument is null)
        {
            return Error.NotFound("ItemShipmentDocument.NotFound", "Item shipment document not found");
        }

        shipmentDocument.DeletedAt = DateTime.UtcNow;
        shipmentDocument.LastDeletedById = userId;

        context.ItemShipmentDocuments.Update(shipmentDocument);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> MarkItemShipmentAsArrived(Guid shipmentDocumentId, Guid userId)
    {
        var shipmentDocument = await context.ItemShipmentDocuments
            .FirstOrDefaultAsync(sd => sd.Id == shipmentDocumentId);
        if (shipmentDocument is null)
        {
            return Error.NotFound("ItemShipmentDocument.NotFound", "Item shipment document not found");
        }

        shipmentDocument.ArrivedAt = DateTime.UtcNow;
        shipmentDocument.Status = ShipmentStatus.Arrived;
        shipmentDocument.LastUpdatedById = userId;

        context.ItemShipmentDocuments.Update(shipmentDocument);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    // ************* Item Waybill *************

    public async Task<Result<Guid>> CreateItemWaybill(CreateItemShipmentDocumentRequest request, Guid userId)
    {
        var waybill = mapper.Map<ItemShipmentDocument>(request);
        if (request.ItemShipmentInvoiceId.HasValue)
        {
            var exists = await context.ItemShipmentInvoices.AnyAsync(i => i.Id == request.ItemShipmentInvoiceId.Value);
            if (!exists) return Error.Validation("ItemShipmentInvoice.NotFound", "Item shipment invoice not found");
        }
        
        waybill.Type = DocType.Waybill;
        waybill.CreatedById = userId;
        await context.ItemShipmentDocuments.AddAsync(waybill);
        await context.SaveChangesAsync();
        return waybill.Id;
    }

    public async Task<Result<ItemShipmentDocumentDto>> GetItemWaybill(Guid waybillId)
    {
        var shipmentDocument = await context.ItemShipmentDocuments
            .AsSplitQuery()
            .Include(s => s.ItemShipmentInvoice)
                .ThenInclude(s => s.Items)
            .FirstOrDefaultAsync(bs => bs.Id == waybillId && bs.Type == DocType.Waybill);

        return shipmentDocument is null
            ? Error.NotFound("ItemShipmentDocument.NotFound", "Item waybill not found")
            : mapper.Map<ItemShipmentDocumentDto>(shipmentDocument);
    }

    public async Task<Result<Paginateable<IEnumerable<ItemShipmentDocumentDto>>>> GetItemWaybills(int page, int pageSize, string searchQuery, ShipmentStatus? status)
    {
        var query = context.ItemShipmentDocuments
            .AsSplitQuery()
            .Include(s => s.ItemShipmentInvoice)
            .Where(s => s.Type == DocType.Waybill)
            .OrderByDescending(s => s.CreatedAt)
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

        return new Paginateable<IEnumerable<ItemShipmentDocumentDto>>
        {
            Data = mapper.Map<IEnumerable<ItemShipmentDocumentDto>>(shipmentDocuments),
            PageIndex = page,
            PageCount = paginatedResult.PageCount,
            TotalRecordCount = paginatedResult.TotalRecordCount,
            StartPageIndex = paginatedResult.StartPageIndex,
            StopPageIndex = paginatedResult.StopPageIndex
        };
    }

    public async Task<Result> UpdateItemWaybill(CreateItemShipmentDocumentRequest request, Guid waybillId, Guid userId)
    {
        var existing = await context.ItemShipmentDocuments.FirstOrDefaultAsync(bs => bs.Id == waybillId && bs.Type == DocType.Waybill);
        if (existing is null)
        {
            return Error.NotFound("ItemShipmentDocument.NotFound", "Item waybill not found");
        }
        
        if (request.ItemShipmentInvoiceId.HasValue)
        {
            var exists = await context.ItemShipmentInvoices.AnyAsync(i => i.Id == request.ItemShipmentInvoiceId.Value);
            if (!exists) return Error.Validation("ItemShipmentInvoice.NotFound", "Item shipment invoice not found");
        }

        mapper.Map(request, existing);
        existing.LastUpdatedById = userId;

        context.ItemShipmentDocuments.Update(existing);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteItemWaybill(Guid waybillId, Guid userId)
    {
        var shipmentDocument = await context.ItemShipmentDocuments.FirstOrDefaultAsync(bs => bs.Id == waybillId && bs.Type == DocType.Waybill);
        if (shipmentDocument is null)
        {
            return Error.NotFound("ItemShipmentDocument.NotFound", "Item waybill not found");
        }

        shipmentDocument.DeletedAt = DateTime.UtcNow;
        shipmentDocument.LastDeletedById = userId;

        context.ItemShipmentDocuments.Update(shipmentDocument);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    // ************* Item Billing Sheet *************

    public async Task<Result<Guid>> CreateItemBillingSheet(CreateItemBillingSheetRequest request, Guid userId)
    {
        if (await context.ItemBillingSheets.AnyAsync(s => s.InvoiceId == request.InvoiceId))
        {
            return Error.Validation("ItemBillingSheet.Duplicate", "A billing sheet for this invoice already exists.");
        }

        if (request.Charges.Count == 0)
        {
            return Error.Validation("ItemBillingSheet.Charges", "No charges for this invoice.");
        }

        var billingSheet = mapper.Map<ItemBillingSheet>(request);
        billingSheet.CreatedById = userId;
        await context.ItemBillingSheets.AddAsync(billingSheet);
        await context.SaveChangesAsync();
        return billingSheet.Id;
    }

    public async Task<Result<ItemBillingSheetDto>> GetItemBillingSheet(Guid billingSheetId)
    {
        var billingSheet = await context.ItemBillingSheets
            .AsSplitQuery()
            .Include(bs => bs.Vendor)
                .ThenInclude(s => s.Currency)
            .Include(bs => bs.Vendor)
                .ThenInclude(s => s.Country)
            .Include(bs => bs.Invoice)
                .ThenInclude(i => i.Items)
                .ThenInclude(ii => ii.Item)
            .Include(bs => bs.Charges)
                .ThenInclude(c => c.Charge)
            .Include(bs => bs.Charges)
                .ThenInclude(c => c.Currency)
            .Include(bs => bs.ContainerPackageStyle)
            .FirstOrDefaultAsync(bs => bs.Id == billingSheetId);

        return billingSheet is null
            ? Error.NotFound("ItemBillingSheet.NotFound", "Item billing sheet not found")
            : mapper.Map<ItemBillingSheetDto>(billingSheet);
    }

    public async Task<Result<ItemBillingSheetDto>> GetItemBillingSheetByInvoice(Guid invoiceId)
    {
        var billingSheet = await context.ItemBillingSheets
            .AsSplitQuery()
            .Include(bs => bs.Vendor)
                .ThenInclude(s => s.Currency)
            .Include(bs => bs.Vendor)
                .ThenInclude(s => s.Country)
            .Include(bs => bs.Invoice)
                .ThenInclude(i => i.Items)
                .ThenInclude(ii => ii.Item)
            .Include(bs => bs.Charges)
                .ThenInclude(c => c.Charge)
            .Include(bs => bs.Charges)
                .ThenInclude(c => c.Currency)
            .FirstOrDefaultAsync(bs => bs.InvoiceId == invoiceId);

        return billingSheet is null
            ? Error.NotFound("ItemBillingSheet.NotFound", "Item billing sheet not found")
            : mapper.Map<ItemBillingSheetDto>(billingSheet);
    }

    public async Task<Result<Paginateable<IEnumerable<ItemBillingSheetDto>>>> GetItemBillingSheets(int page, int pageSize, string searchQuery, BillingSheetStatus? status)
    {
        var query = context.ItemBillingSheets
            .Include(bs => bs.Vendor)
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
            mapper.Map<ItemBillingSheetDto>
        );
    }

    public async Task<Result> UpdateItemBillingSheet(UpdateItemBillingSheetRequest request, Guid billingSheetId, Guid userId)
    {
        var existing = await context.ItemBillingSheets.FirstOrDefaultAsync(bs => bs.Id == billingSheetId);
        if (existing is null)
        {
            return Error.NotFound("ItemBillingSheet.NotFound", "Item billing sheet not found");
        }
        
        if (request.VendorId.HasValue)
        {
            var exists = await context.Vendors.AnyAsync(i => i.Id == request.VendorId.Value);
            if (!exists) return Error.Validation("Supplier.NotFound", "Supplier not found");
        }

        if (request.ContainerPackageStyleId.HasValue)
        {
            var exists = await context.PackageStyles.AnyAsync(i => i.Id == request.ContainerPackageStyleId.Value);
            if (!exists) return Error.Validation("Container.PackageStyle.NotFound", "Container package style not found");
        }
        
        var invoiceExists = await context.ItemShipmentInvoices.AnyAsync(i => i.Id == request.InvoiceId);
        if  (!invoiceExists) return Error.Validation("ItemShipmentInvoices.NotFound", "Item invoice not found");

        mapper.Map(request, existing);
        existing.LastUpdatedById = userId;

        context.ItemBillingSheets.Update(existing);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> AddChargesToItemBillingSheet(List<CreateBillingSheetCharge> request, Guid billingSheetId, Guid userId)
    {
        var existing = await context.ItemBillingSheets
            .AsSplitQuery()
            .Include(bs => bs.Charges)
            .FirstOrDefaultAsync(bs => bs.Id == billingSheetId);

        if (existing is null)
        {
            return Error.NotFound("ItemBillingSheet.NotFound", "Item billing sheet not found");
        }
        
        if (request.Count == 0) return Error.Validation("Charges.NotFound", "No billing sheet charges found");

        existing.Charges.AddRange(mapper.Map<List<ItemBillingSheetCharge>>(request));
        existing.LastUpdatedById = userId;
        context.ItemBillingSheets.Update(existing);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> MarkItemBillingSheetChargeAsPaid(MarkBillingSheetCharge request, Guid userId)
    {
        var existingCharges = await context.ItemBillingSheetCharges
            .Where(bs => request.BillingSheetChargeIds.Contains(bs.Id))
            .ToListAsync();

        if (existingCharges.Count == 0)
        {
            return Error.NotFound("Charge.NotFound", "Item billing sheet charge not found");
        }

        await context.ItemBillingSheetCharges
            .Where(bs => request.BillingSheetChargeIds.Contains(bs.Id))
            .ExecuteUpdateAsync(setters =>
                setters
                    .SetProperty(e => e.Paid, true)
                    .SetProperty(p => p.LastUpdatedById, userId)
                    .SetProperty(p => p.LastUpdatedOn, DateTime.UtcNow));

        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteItemBillingSheet(Guid billingSheetId, Guid userId)
    {
        var billingSheet = await context.ItemBillingSheets.FirstOrDefaultAsync(bs => bs.Id == billingSheetId);
        if (billingSheet is null)
        {
            return Error.NotFound("ItemBillingSheet.NotFound", "Item billing sheet not found");
        }

        billingSheet.DeletedAt = DateTime.UtcNow;
        billingSheet.LastDeletedById = userId;

        context.ItemBillingSheets.Update(billingSheet);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}