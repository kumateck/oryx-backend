using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.DamagedStocks;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.ItemTransactionLogs;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class DamagedStocksRepository(ApplicationDbContext context, IMapper mapper) : IDamagedStocksRepository
{
    public async Task<Result<Guid>> CreateDamagedStocks(CreateDamagedStockRequest request, Guid userId)
    {
        var item = await context.Items.FirstOrDefaultAsync(i => i.Id == request.ItemId);
        if (item == null) return Error.NotFound("Item.Invalid", "Invalid item.");
        if (!Enum.IsDefined(request.DamageStatus) || request.QuantityDamaged <= 0 || request.QuantityDamaged > item.AvailableQuantity)
            return Error.Validation("Quantity.Invalid", "Choose damage or missing, with a positive quantity no greater than current available stock.");
        var ledger = await APP.Services.Inventory.InventoryLedger.Balance(context, item);
        if (ledger.IsFailure) return ledger.Error;
        var report = mapper.Map<DamagedStock>(request);
        report.Id = Guid.NewGuid();
        report.CreatedById = userId;
        item.AvailableQuantity -= request.QuantityDamaged;
        context.DamagedStocks.Add(report);
        context.ItemTransactionLogs.Add(new ItemTransactionLog {
            Id = Guid.NewGuid(), ItemCode = item.Code,
            TransactionType = request.DamageStatus == DamageStatus.Damage ? TransactionType.Damaged : TransactionType.Missing,
            Credit = 0, Debit = request.QuantityDamaged, TotalBalance = item.AvailableQuantity, CreatedById = userId
        });
        // The stock, report and ledger are one atomic SaveChanges transaction.
        var commit = await APP.Services.Inventory.InventoryCommit.Save(context);
        return commit.IsFailure ? commit.Error : report.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<DamagedStockDto>>>> GetDamagedStocks(int page, int pageSize, string searchQuery,
        IReadOnlyCollection<Store> readableStores, Store? store, DamageStatus? damageStatus)
    {
        var query = context.DamagedStocks
            .AsSplitQuery()
            .Include(d => d.Item).ThenInclude(i => i.UnitOfMeasure)
            .Include(d => d.CreatedBy)
            .Where(d => readableStores.Contains(d.Item.Store));
        if (store.HasValue) query = query.Where(d => d.Item.Store == store);
        if (damageStatus.HasValue) query = query.Where(d => d.DamageStatus == damageStatus);
        if (!string.IsNullOrWhiteSpace(searchQuery)) query = query.WhereSearch(searchQuery, q => q.Item.Name, q => q.Item.Code);
        return await PaginationHelper.GetPaginatedResultAsync(query.OrderByDescending(d => d.CreatedAt), page, pageSize, entity => mapper.Map<DamagedStockDto>(entity, opts =>
            opts.Items[AppConstants.ModelType] = nameof(DamagedStock)));
    }

    public async Task<Result<DamagedStockDto>> GetDamagedStock(Guid id)
    {
        var stocks = await context.DamagedStocks
            .AsSplitQuery()
            .Include(d => d.Item)
            .FirstOrDefaultAsync(ds => ds.Id == id);
        return stocks is null ?
            Error.NotFound("DamagedStock.NotFound", "Damaged stock not found") :
            mapper.Map<DamagedStockDto>(stocks,
                opts => { opts.Items[AppConstants.ModelType] = nameof(DamagedStock); });

    }

    /// <summary>
    /// Corrects a damage or missing record. The item cannot change; a larger quantity takes the difference from stock
    /// and a smaller one gives it back, posted to the ledger as a debit or credit in the same atomic save.
    /// </summary>
    public async Task<Result> UpdateDamagedStocks(Guid id, CreateDamagedStockRequest request, Guid userId)
    {
        var report = await context.DamagedStocks.Include(d => d.Item).FirstOrDefaultAsync(ds => ds.Id == id);
        if (report == null) return Error.NotFound("DamagedStock.NotFound", "Damaged stock not found");
        if (request.ItemId != report.ItemId)
            return Error.Validation("DamagedStock.Item", "The item of a damage or missing record cannot be changed. Delete it and record the right item.");
        if (!Enum.IsDefined(request.DamageStatus) || request.QuantityDamaged <= 0)
            return Error.Validation("Quantity.Invalid", "Choose damage or missing, with a positive quantity.");
        var difference = request.QuantityDamaged - report.QuantityDamaged;
        if (difference > report.Item.AvailableQuantity)
            return Error.Validation("DamagedStock.Invalid", "Not enough stock to increase the damaged or missing quantity.");
        if (difference != 0)
        {
            var ledger = await APP.Services.Inventory.InventoryLedger.Balance(context, report.Item);
            if (ledger.IsFailure) return ledger.Error;
            report.Item.AvailableQuantity -= difference;
            context.ItemTransactionLogs.Add(new ItemTransactionLog
            {
                Id = Guid.NewGuid(), ItemCode = report.Item.Code,
                TransactionType = request.DamageStatus == DamageStatus.Damage ? TransactionType.Damaged : TransactionType.Missing,
                Debit = difference > 0 ? difference : 0, Credit = difference < 0 ? -difference : 0,
                TotalBalance = report.Item.AvailableQuantity, CreatedById = userId
            });
        }
        report.DamageStatus = request.DamageStatus;
        report.QuantityDamaged = request.QuantityDamaged;
        report.Remarks = request.Remarks;
        report.LastUpdatedById = userId;
        return await APP.Services.Inventory.InventoryCommit.Save(context);
    }

    /// <summary>Deletes a damage or missing record and gives its quantity back to stock in one atomic posting.</summary>
    public async Task<Result> DeleteDamagedStocks(Guid id, Guid userId)
    {
        var report = await context.DamagedStocks.Include(ds => ds.Item).FirstOrDefaultAsync(ds => ds.Id == id);
        if (report == null) return Error.NotFound("DamagedStock.NotFound", "Damaged stock not found");
        var ledger = await APP.Services.Inventory.InventoryLedger.Balance(context, report.Item);
        if (ledger.IsFailure) return ledger.Error;
        report.Item.AvailableQuantity += report.QuantityDamaged;
        report.LastDeletedById = userId;
        report.DeletedAt = DateTime.UtcNow;
        context.ItemTransactionLogs.Add(new ItemTransactionLog
        {
            Id = Guid.NewGuid(), ItemCode = report.Item.Code,
            TransactionType = report.DamageStatus == DamageStatus.Damage ? TransactionType.Damaged : TransactionType.Missing,
            Credit = report.QuantityDamaged, Debit = 0, TotalBalance = report.Item.AvailableQuantity, CreatedById = userId
        });
        return await APP.Services.Inventory.InventoryCommit.Save(context);
    }
}
