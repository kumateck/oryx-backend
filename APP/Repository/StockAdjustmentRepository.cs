using APP.IRepository;
using DOMAIN.Entities.BinCards;
using DOMAIN.Entities.InventoryLedgers;
using DOMAIN.Entities.ItemTransactionLogs;
using DOMAIN.Entities.StockAdjustments;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class StockAdjustmentRepository(ApplicationDbContext context) : IStockAdjustmentRepository
{
    public async Task<Result<StockAdjustmentSummaryDto>> CreateStockAdjustment(
        CreateStockAdjustmentRequest request,
        Guid userId
    )
    {
        var warehouse = await context.Warehouses.AnyAsync(w => w.Id == request.WarehouseId);
        if (!warehouse)
            return Error.NotFound("Warehouse.NotFound", "Warehouse not found");

        foreach (var line in request.Lines)
        {
            if (line.PhysicalCount < 0)
                return Error.Validation(
                    "PhysicalCount.Invalid",
                    $"Physical count cannot be negative for ModelId: {line.ModelId}"
                );
        }

        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var adjustment = new StockAdjustment
            {
                AdjustmentNumber = request.AdjustmentNumber,
                WarehouseId = request.WarehouseId,
                AdjustmentDate = request.AdjustmentDate,
                TargetType = request.TargetType,
                CreatedById = userId,
                CreatedAt = DateTime.UtcNow,
            };

            await context.StockAdjustments.AddAsync(adjustment);

            decimal totalVariance = 0;
            int linesProcessed = 0;

            foreach (var lineRequest in request.Lines)
            {
                decimal systemQuantity;
                decimal variance;
                decimal postBalance;

                var adjustmentLine = new StockAdjustmentLine
                {
                    StockAdjustment = adjustment,
                    PhysicalCount = lineRequest.PhysicalCount,
                    ReasonCode = lineRequest.ReasonCode.ToString(),
                    Notes = lineRequest.Notes,
                    CreatedById = userId,
                    CreatedAt = DateTime.UtcNow,
                };

                if (request.TargetType == StockAdjustmentTarget.Item)
                {
                    var item = await context.Items.FirstOrDefaultAsync(i =>
                        i.Id == lineRequest.ModelId
                    );
                    if (item == null)
                        return Error.NotFound(
                            "Item.NotFound",
                            $"Item not found: {lineRequest.ModelId}"
                        );

                    systemQuantity = item.AvailableQuantity;
                    variance = lineRequest.PhysicalCount - systemQuantity;

                    if (
                        variance != 0
                        && string.IsNullOrWhiteSpace(lineRequest.ReasonCode.ToString())
                    )
                        return Error.Validation(
                            "ReasonCode.Required",
                            $"Reason code is required for non-zero variance on item: {item.Name}"
                        );

                    if (variance == 0)
                        continue; // Skip if verified

                    item.AvailableQuantity += (int)variance;
                    postBalance = item.AvailableQuantity;

                    adjustmentLine.ItemId = item.Id;
                    adjustmentLine.SystemQuantitySnapshot = systemQuantity;
                    adjustmentLine.Variance = variance;

                    context.Items.Update(item);

                    // Backward compatibility: ItemTransactionLog
                    var itemTransactionLog = new ItemTransactionLog
                    {
                        ItemCode = item.Code,
                        TransactionType = TransactionType.Adjustment,
                        Credit = variance > 0 ? variance : 0,
                        Debit = variance < 0 ? Math.Abs(variance) : 0,
                        TotalBalance = postBalance,
                        CreatedAt = DateTime.UtcNow,
                    };
                    await context.ItemTransactionLogs.AddAsync(itemTransactionLog);
                }
                else // Material
                {
                    var shelfBatch = await context
                        .ShelfMaterialBatches.AsSplitQuery()
                        .Include(s => s.MaterialBatch)
                        .FirstOrDefaultAsync(s => s.Id == lineRequest.ModelId);
                    if (shelfBatch == null)
                        return Error.NotFound(
                            "ShelfMaterialBatch.NotFound",
                            $"Shelf material batch not found: {lineRequest.ModelId}"
                        );

                    systemQuantity = shelfBatch.Quantity;
                    variance = lineRequest.PhysicalCount - systemQuantity;

                    if (
                        variance != 0
                        && string.IsNullOrWhiteSpace(lineRequest.ReasonCode.ToString())
                    )
                        return Error.Validation(
                            "ReasonCode.Required",
                            $"Reason code is required for non-zero variance on batch: {shelfBatch.MaterialBatch.BatchNumber}"
                        );

                    if (variance == 0)
                        continue;

                    // Update Shelf Quantity
                    shelfBatch.Quantity += variance;
                    postBalance = shelfBatch.Quantity;

                    // Update Global Batch Total to maintain consistency
                    shelfBatch.MaterialBatch.TotalQuantity += variance;

                    adjustmentLine.ShelfMaterialBatchId = shelfBatch.Id;
                    adjustmentLine.SystemQuantitySnapshot = systemQuantity;
                    adjustmentLine.Variance = variance;

                    context.ShelfMaterialBatches.Update(shelfBatch);
                    context.MaterialBatches.Update(shelfBatch.MaterialBatch);

                    // Backward compatibility/Consistency: Log to BinCardInformation
                    var binCard = new BinCardInformation
                    {
                        MaterialBatchId = shelfBatch.MaterialBatchId,
                        WarehouseId = request.WarehouseId,
                        QuantityReceived = variance > 0 ? variance : 0,
                        QuantityIssued = variance < 0 ? Math.Abs(variance) : 0,
                        BalanceQuantity = postBalance,
                        Description = $"Stock Adjustment: {request.AdjustmentNumber}",
                        CreatedAt = DateTime.UtcNow,
                        UoMId = shelfBatch.UoMId,
                    };
                    await context.BinCardInformation.AddAsync(binCard);
                }

                await context.StockAdjustmentLines.AddAsync(adjustmentLine);

                // Immutable Audit Ledger
                var ledgerEntry = new InventoryLedger
                {
                    TransactionType = "Adjustment",
                    ReferenceId = request.AdjustmentNumber,
                    ItemId =
                        request.TargetType == StockAdjustmentTarget.Item
                            ? lineRequest.ModelId
                            : null,
                    ShelfMaterialBatchId =
                        request.TargetType == StockAdjustmentTarget.Material
                            ? lineRequest.ModelId
                            : null,
                    ChangeAmount = variance,
                    PostTransactionBalance = postBalance,
                    Notes = lineRequest.Notes,
                    CreatedById = userId,
                    CreatedAt = DateTime.UtcNow,
                };
                await context.InventoryLedgers.AddAsync(ledgerEntry);

                totalVariance += Math.Abs(variance);
                linesProcessed++;
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new StockAdjustmentSummaryDto
            {
                Id = adjustment.Id,
                AdjustmentNumber = adjustment.AdjustmentNumber,
                LinesProcessed = linesProcessed,
                TotalVariance = totalVariance,
            };
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return Error.Conflict(
                "Stock.ConcurrencyConflict",
                "The system quantity was changed by another process. Please refresh and try again."
            );
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return Error.Failure("StockAdjustment.Failed", ex.Message);
        }
    }
}
