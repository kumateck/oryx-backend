using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Items;
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
}