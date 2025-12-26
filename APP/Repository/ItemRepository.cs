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

        var itemsToUpload = new List<Item>();

        var lastRow = worksheet.Dimension.End.Row;
        while (lastRow >= 2 && string.IsNullOrWhiteSpace(worksheet.Cells[lastRow, 1].Text))
        {
            lastRow--;
        }

        for (var row = 2; row <= lastRow; row++)
        {
            var storeType = worksheet.Cells[row, 1].Text?.Replace(" ", "").Trim();
            var itemName = worksheet.Cells[row, 2].Text?.Trim();
            var itemCode = worksheet.Cells[row, 3].Text?.Trim();
            var categoryName = worksheet.Cells[row, 4].Text?.Replace(" ", "").Trim();
            var uomName = worksheet.Cells[row, 5].Text?.Trim();
            var classificationText = worksheet.Cells[row, 6].Text?.Replace(" ", "").Trim();
            var minimumLevel = worksheet.Cells[row, 7].Text?.Trim();
            var reorderLevelText = worksheet.Cells[row, 8].Text?.Trim();
            var maximumLevelText = worksheet.Cells[row, 9].Text?.Trim();

            if (string.IsNullOrWhiteSpace(storeType) ||
                string.IsNullOrWhiteSpace(itemCode) ||
                string.IsNullOrWhiteSpace(uomName) ||
                string.IsNullOrWhiteSpace(classificationText))
            {
                return Error.Validation(
                    "ItemUpload.MissingFields",
                    $"Missing required fields at row {row}."
                );
            }

            if (!Enum.TryParse<Store>(storeType, true, out var inventoryStore))
            {
                return Error.Validation(
                    "ItemUpload.InvalidStore",
                    $"Invalid store '{storeType}' at row {row}."
                );
            }

            if (!Enum.TryParse<InventoryClassification>(classificationText, true, out var inventoryClassification))
            {
                return Error.Validation(
                    "ItemUpload.InvalidClassification",
                    $"Invalid classification '{classificationText}' at row {row}."
                );
            }

            var name = uomName[..uomName.IndexOf('(')].Trim();
            var symbol = uomName[(uomName.IndexOf('(') + 1)..^1].Trim();

            var uom = await context.UnitOfMeasures
                .FirstOrDefaultAsync(u =>
                    u.Name == name &&
                    u.Symbol == symbol);

            var itemCategory = await context.ItemCategories.FirstOrDefaultAsync(ic => ic.Name == categoryName);

            if (uom == null)
            {
                return Error.Validation(
                    "ItemUpload.InvalidUoM",
                    $"Unit of Measure '{uomName}' not found at row {row}."
                );
            }

            var itemExists = await context.Items.AnyAsync(i => i.Code == itemCode);
            if (itemExists) continue;

            if (!int.TryParse(minimumLevel, out var minLevel) ||
                !int.TryParse(reorderLevelText, out var reorderLevel) ||
                !int.TryParse(maximumLevelText, out var maxLevel))
            {
                return Error.Validation(
                    "ItemUpload.InvalidLevels",
                    $"Invalid inventory levels at row {row}. Levels must be integers."
                );
            }

            itemsToUpload.Add(new Item
            {
                Name = itemName,
                Store = inventoryStore,
                Code = itemCode,
                Classification = inventoryClassification,
                ItemCategoryId = itemCategory?.Id,
                MinimumLevel = minLevel,
                ReorderLevel = reorderLevel,
                MaximumLevel = maxLevel,
                UnitOfMeasureId = uom.Id
            });
        }

        if (itemsToUpload.Count == 0)
            return Error.Validation("ItemsUpload.NoneAdded", "No new items were uploaded.");

        await context.Items.AddRangeAsync(itemsToUpload);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<Paginateable<IEnumerable<ItemDto>>>> GetItems(int page, int pageSize,
        string searchQuery, Store? store)
    {
        var query = context.Items
            .Include(i => i.UnitOfMeasure)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, i => i.Name,
                i => i.Code);
        }

        if (store.HasValue)
        {
            query = query.Where(i => i.Store == store.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            if (Enum.TryParse<Store>(searchQuery, true, out var itemStore))
            {
                query = query.Where(q => q.Store == itemStore);
            }
        }

        return await PaginationHelper.GetPaginatedResultAsync(query,
            page,
            pageSize,
            entity => mapper.Map<ItemDto>(entity, opts =>
            opts.Items[AppConstants.ModelType] = nameof(Item)));
    }

    public async Task<Result<ItemDto>> GetItem(Guid id)
    {
        var item = await context.Items
            .Include(i => i.UnitOfMeasure)
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