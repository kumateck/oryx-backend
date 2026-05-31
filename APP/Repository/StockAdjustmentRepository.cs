using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.BinCards;
using DOMAIN.Entities.InventoryLedgers;
using DOMAIN.Entities.ItemTransactionLogs;
using DOMAIN.Entities.StockAdjustments;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class StockAdjustmentRepository(
    ApplicationDbContext context,
    IApprovalRepository approvalRepository,
    IMapper mapper
) : IStockAdjustmentRepository
{
    public async Task<Result<StockAdjustmentSummaryDto>> CreateStockAdjustment(
        CreateStockAdjustmentRequest request,
        Guid userId
    )
    {
        foreach (var line in request.Lines)
        {
            if (line.PhysicalCount < 0)
                return Error.Validation(
                    "PhysicalCount.Invalid",
                    $"Physical count cannot be negative for ModelId: {line.ModelId}"
                );
        }

        var adjustment = new StockAdjustment
        {
            AdjustmentNumber = request.AdjustmentNumber,
            AdjustmentDate = request.AdjustmentDate,
            TargetType = request.TargetType,
            CreatedById = userId,
            CreatedAt = DateTime.UtcNow,
            Approved = false,
        };

        await context.StockAdjustments.AddAsync(adjustment);

        foreach (var lineRequest in request.Lines)
        {
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

                adjustmentLine.ItemId = item.Id;
                adjustmentLine.SystemQuantitySnapshot = item.AvailableQuantity;
                adjustmentLine.Variance = lineRequest.PhysicalCount - item.AvailableQuantity;
            }
            else if (request.TargetType == StockAdjustmentTarget.Material)
            {
                var shelfBatch = await context.ShelfMaterialBatches.FirstOrDefaultAsync(s =>
                    s.Id == lineRequest.ModelId
                );
                if (shelfBatch == null)
                    return Error.NotFound(
                        "ShelfMaterialBatch.NotFound",
                        $"Shelf material batch not found: {lineRequest.ModelId}"
                    );

                adjustmentLine.ShelfMaterialBatchId = shelfBatch.Id;
                adjustmentLine.SystemQuantitySnapshot = shelfBatch.Quantity;
                adjustmentLine.Variance = lineRequest.PhysicalCount - shelfBatch.Quantity;
            }
            else // Product
            {
                var transferNote = await context.FinishedGoodsTransferNotes.FirstOrDefaultAsync(t =>
                    t.Id == lineRequest.ModelId
                );
                if (transferNote == null)
                    return Error.NotFound(
                        "FinishedGoodsTransferNote.NotFound",
                        $"Finished goods transfer note not found: {lineRequest.ModelId}"
                    );

                adjustmentLine.FinishedGoodsTransferNoteId = transferNote.Id;
                adjustmentLine.SystemQuantitySnapshot = transferNote.TotalQuantity;
                adjustmentLine.Variance = lineRequest.PhysicalCount - transferNote.TotalQuantity;
            }

            await context.StockAdjustmentLines.AddAsync(adjustmentLine);
        }

        await context.SaveChangesAsync();

        await approvalRepository.CreateInitialApprovalsAsync(
            nameof(StockAdjustment),
            adjustment.Id
        );

        // Check if it was auto-approved (no stages)
        var updatedAdjustment = await context
            .StockAdjustments.AsSplitQuery()
            .Include(a => a.Lines)
            .FirstOrDefaultAsync(a => a.Id == adjustment.Id);

        if (updatedAdjustment is { Approved: true })
        {
            var applyResult = await ApplyStockAdjustment(updatedAdjustment.Id, userId);
            if (!applyResult.IsSuccess)
                return applyResult.Error;
        }

        return new StockAdjustmentSummaryDto
        {
            Id = adjustment.Id,
            AdjustmentNumber = adjustment.AdjustmentNumber,
            AdjustmentDate = adjustment.AdjustmentDate,
            LinesProcessed = adjustment.Lines.Count,
            TotalVariance = adjustment.Lines.Sum(l => Math.Abs(l.Variance)),
            Approved = updatedAdjustment?.Approved ?? false,
            TargetType = adjustment.TargetType,
        };
    }

    public async Task<
        Result<Paginateable<IEnumerable<StockAdjustmentSummaryDto>>>
    > GetStockAdjustments(
        int page,
        int pageSize,
        string searchQuery,
        bool? approved = null,
        StockAdjustmentTarget? targetType = null
    )
    {
        var query = context
            .StockAdjustments.AsSplitQuery()
            .Include(a => a.Lines)
                .ThenInclude(l => l.Item)
                    .ThenInclude(i => i.UnitOfMeasure)
            .Include(a => a.Lines)
                .ThenInclude(l => l.ShelfMaterialBatch)
                    .ThenInclude(s => s.MaterialBatch)
                        .ThenInclude(mb => mb.Material)
            .Include(a => a.Lines)
                .ThenInclude(l => l.ShelfMaterialBatch)
                    .ThenInclude(s => s.UoM)
            .Include(a => a.Lines)
                .ThenInclude(l => l.FinishedGoodsTransferNote)
                    .ThenInclude(t => t.UoM)
            .Include(a => a.Lines)
                .ThenInclude(l => l.FinishedGoodsTransferNote)
                    .ThenInclude(t => t.BatchManufacturingRecord)
                        .ThenInclude(b => b.ProductionScheduleProduct)
                            .ThenInclude(p => p.Product)
            .OrderByDescending(a => a.AdjustmentDate)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, a => a.AdjustmentNumber);
        }

        if (approved.HasValue)
        {
            query = query.Where(a => a.Approved == approved.Value);
        }

        if (targetType.HasValue)
        {
            query = query.Where(a => a.TargetType == targetType.Value);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            a => new StockAdjustmentSummaryDto
            {
                Id = a.Id,
                AdjustmentNumber = a.AdjustmentNumber,
                AdjustmentDate = a.AdjustmentDate,
                LinesProcessed = a.Lines.Count,
                TotalVariance = a.Lines.Sum(l => Math.Abs(l.Variance)),
                Approved = a.Approved,
                TargetType = a.TargetType,
                Lines = a.Lines.Select(l => new StockAdjustmentLineSummaryDto
                {
                    Id = l.Id,
                    Name = a.TargetType switch
                    {
                        StockAdjustmentTarget.Item => l.Item?.Name,
                        StockAdjustmentTarget.Material => l.ShelfMaterialBatch?.MaterialBatch?.Material?.Name,
                        StockAdjustmentTarget.Product => l.FinishedGoodsTransferNote?.BatchManufacturingRecord?.ProductionScheduleProduct?.Product?.Name,
                        _ => null
                    },
                    CodeOrBatch = a.TargetType switch
                    {
                        StockAdjustmentTarget.Item => l.Item?.Code,
                        StockAdjustmentTarget.Material => l.ShelfMaterialBatch?.MaterialBatch?.BatchNumber,
                        StockAdjustmentTarget.Product => l.FinishedGoodsTransferNote?.TransferNoteNumber,
                        _ => null
                    },
                    PhysicalCount = l.PhysicalCount,
                    SystemQuantitySnapshot = l.SystemQuantitySnapshot,
                    Variance = l.Variance,
                    UomSymbol = a.TargetType switch
                    {
                        StockAdjustmentTarget.Item => l.Item?.UnitOfMeasure?.Symbol,
                        StockAdjustmentTarget.Material => l.ShelfMaterialBatch?.UoM?.Symbol,
                        StockAdjustmentTarget.Product => l.FinishedGoodsTransferNote?.UoM?.Symbol,
                        _ => null
                    }
                }).ToList()
            }
        );
    }

    public async Task<Result<StockAdjustmentDetailDto>> GetStockAdjustment(Guid id)
    {
        var adjustment = await context
            .StockAdjustments.AsSplitQuery()
            .Include(a => a.Lines)
                .ThenInclude(l => l.Item)
                    .ThenInclude(i => i.UnitOfMeasure)
            .Include(a => a.Lines)
                .ThenInclude(l => l.ShelfMaterialBatch)
                    .ThenInclude(s => s.MaterialBatch)
                        .ThenInclude(mb => mb.Material)
            .Include(a => a.Lines)
                .ThenInclude(l => l.ShelfMaterialBatch)
                    .ThenInclude(s => s.WarehouseLocationShelf)
            .Include(a => a.Lines)
                .ThenInclude(l => l.ShelfMaterialBatch)
                    .ThenInclude(s => s.UoM)
            .Include(a => a.Lines)
                .ThenInclude(l => l.FinishedGoodsTransferNote)
                    .ThenInclude(t => t.UoM)
            .Include(a => a.Lines)
                .ThenInclude(l => l.FinishedGoodsTransferNote)
                    .ThenInclude(t => t.BatchManufacturingRecord)
                        .ThenInclude(b => b.ProductionScheduleProduct)
                            .ThenInclude(p => p.Product)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (adjustment == null)
            return Error.NotFound("StockAdjustment.NotFound", "Stock adjustment not found.");

        return new StockAdjustmentDetailDto
        {
            Id = adjustment.Id,
            AdjustmentNumber = adjustment.AdjustmentNumber,
            AdjustmentDate = adjustment.AdjustmentDate,
            Approved = adjustment.Approved,
            TargetType = adjustment.TargetType,
            CreatedAt = adjustment.CreatedAt,
            Lines = adjustment
                .Lines.Select(l => new StockAdjustmentLineDetailDto
                {
                    Id = l.Id,
                    ItemId = l.ItemId,
                    ItemName = l.Item?.Name,
                    ItemCode = l.Item?.Code,
                    ShelfMaterialBatchId = l.ShelfMaterialBatchId,
                    BatchNumber = l.ShelfMaterialBatch?.MaterialBatch?.BatchNumber,
                    MaterialName = l.ShelfMaterialBatch?.MaterialBatch?.Material?.Name,
                    ShelfName = l.ShelfMaterialBatch?.WarehouseLocationShelf?.Name,
                    FinishedGoodsTransferNoteId = l.FinishedGoodsTransferNoteId,
                    TransferNoteNumber = l.FinishedGoodsTransferNote?.TransferNoteNumber,
                    ProductName = l.FinishedGoodsTransferNote
                        ?.BatchManufacturingRecord
                        ?.ProductionScheduleProduct
                        ?.Product
                        ?.Name,
                    PhysicalCount = l.PhysicalCount,
                    SystemQuantitySnapshot = l.SystemQuantitySnapshot,
                    Variance = l.Variance,
                    Uom = adjustment.TargetType switch
                    {
                        StockAdjustmentTarget.Item => mapper.Map<UnitOfMeasureDto>(
                            l.Item?.UnitOfMeasure
                        ),
                        StockAdjustmentTarget.Material => mapper.Map<UnitOfMeasureDto>(
                            l.ShelfMaterialBatch?.UoM
                        ),
                        StockAdjustmentTarget.Product => mapper.Map<UnitOfMeasureDto>(
                            l.FinishedGoodsTransferNote?.UoM
                        ),
                        _ => null,
                    },
                    ReasonCode = l.ReasonCode,
                    Notes = l.Notes,
                })
                .ToList(),
        };
    }

    public async Task<Result> ApplyStockAdjustment(Guid adjustmentId, Guid userId)
    {
        var adjustment = await context
            .StockAdjustments.Include(a => a.Lines)
            .FirstOrDefaultAsync(a => a.Id == adjustmentId);

        if (adjustment == null)
            return Error.NotFound("StockAdjustment.NotFound", "Stock adjustment not found.");

        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            foreach (var line in adjustment.Lines)
            {
                if (line.Variance == 0)
                    continue;

                decimal postBalance;

                if (adjustment.TargetType == StockAdjustmentTarget.Item)
                {
                    var item = await context.Items.FirstOrDefaultAsync(i => i.Id == line.ItemId);
                    if (item == null)
                        continue;

                    item.AvailableQuantity += (int)line.Variance;
                    postBalance = item.AvailableQuantity;
                    context.Items.Update(item);

                    // Backward compatibility: ItemTransactionLog
                    var itemTransactionLog = new ItemTransactionLog
                    {
                        ItemCode = item.Code,
                        TransactionType = TransactionType.Adjustment,
                        Credit = line.Variance > 0 ? line.Variance : 0,
                        Debit = line.Variance < 0 ? Math.Abs(line.Variance) : 0,
                        TotalBalance = postBalance,
                        CreatedAt = DateTime.UtcNow,
                    };
                    await context.ItemTransactionLogs.AddAsync(itemTransactionLog);
                }
                else if (adjustment.TargetType == StockAdjustmentTarget.Material)
                {
                    var shelfBatch = await context
                        .ShelfMaterialBatches.Include(s => s.MaterialBatch)
                        .Include(s => s.WarehouseLocationShelf)
                            .ThenInclude(w => w.WarehouseLocationRack)
                                .ThenInclude(r => r.WarehouseLocation)
                        .FirstOrDefaultAsync(s => s.Id == line.ShelfMaterialBatchId);

                    if (shelfBatch == null)
                        continue;

                    shelfBatch.Quantity += line.Variance;
                    postBalance = shelfBatch.Quantity;
                    shelfBatch.MaterialBatch.TotalQuantity += line.Variance;

                    context.ShelfMaterialBatches.Update(shelfBatch);
                    context.MaterialBatches.Update(shelfBatch.MaterialBatch);

                    // Backward compatibility/Consistency: Log to BinCardInformation
                    var binCard = new BinCardInformation
                    {
                        MaterialBatchId = shelfBatch.MaterialBatchId,
                        WarehouseId = shelfBatch
                            .WarehouseLocationShelf
                            .WarehouseLocationRack
                            .WarehouseLocation
                            .WarehouseId,
                        QuantityReceived = line.Variance > 0 ? line.Variance : 0,
                        QuantityIssued = line.Variance < 0 ? Math.Abs(line.Variance) : 0,
                        BalanceQuantity = postBalance,
                        Description = $"Stock Adjustment: {adjustment.AdjustmentNumber}",
                        CreatedAt = DateTime.UtcNow,
                        UoMId = shelfBatch.UoMId,
                    };
                    await context.BinCardInformation.AddAsync(binCard);
                }
                else // Product
                {
                    var transferNote = await context
                        .FinishedGoodsTransferNotes.Include(t => t.BatchManufacturingRecord)
                        .FirstOrDefaultAsync(t => t.Id == line.FinishedGoodsTransferNoteId);

                    if (transferNote == null)
                        continue;

                    transferNote.TotalQuantity += line.Variance;
                    postBalance = transferNote.TotalQuantity;

                    context.FinishedGoodsTransferNotes.Update(transferNote);

                    // Log to ProductBinCardInformation
                    var binCardEvent = new ProductBinCardInformation
                    {
                        BatchId = transferNote.BatchManufacturingRecordId,
                        QuantityReceived = line.Variance > 0 ? line.Variance : 0,
                        QuantityIssued = line.Variance < 0 ? Math.Abs(line.Variance) : 0,
                        BalanceQuantity = postBalance,
                        Description = $"Stock Adjustment: {adjustment.AdjustmentNumber}",
                        CreatedAt = DateTime.UtcNow,
                        UoMId = transferNote.UoMId,
                        CreatedById = userId,
                    };
                    await context.ProductBinCardInformation.AddAsync(binCardEvent);
                }

                // Immutable Audit Ledger
                var ledgerEntry = new InventoryLedger
                {
                    TransactionType = "Adjustment",
                    ReferenceId = adjustment.AdjustmentNumber,
                    ItemId = line.ItemId,
                    ShelfMaterialBatchId = line.ShelfMaterialBatchId,
                    FinishedGoodsTransferNoteId = line.FinishedGoodsTransferNoteId,
                    ChangeAmount = line.Variance,
                    PostTransactionBalance = postBalance,
                    Notes = line.Notes,
                    CreatedById = userId,
                    CreatedAt = DateTime.UtcNow,
                };
                await context.InventoryLedgers.AddAsync(ledgerEntry);
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return Result.Success();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return Error.Failure("StockAdjustment.ApplyFailed", ex.Message);
        }
    }
}
