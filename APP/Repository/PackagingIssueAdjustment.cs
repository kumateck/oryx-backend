using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Requisitions;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

/// <summary>
/// Packaging is issued in whole packs, so a line may leave the store over or under its requirement. The difference is
/// taken from, or left on, the last batch in picking order: the one whose pack is opened. Production returns any
/// leftover through Final Packing's partial returns.
/// </summary>
internal static class PackagingIssueAdjustment
{
    internal const int MinimumReasonLength = 5;

    /// <summary>Picking order, as the allocator reserves: returned stock first, then soonest effective date.</summary>
    internal static List<MaterialBatchReservedQuantity> PickingOrder(IEnumerable<MaterialBatchReservedQuantity> reservations) =>
        reservations.OrderBy(item => item.MaterialBatch?.ReturnDate == null).ThenBy(item => item.MaterialBatch?.ReturnDate)
            .ThenBy(item => item.MaterialBatch is null ? null : MaterialValidity.EffectiveDate(item.MaterialBatch))
            .ThenBy(item => item.Id).ToList();

    /// <summary>Why a line's issued quantity cannot stand, or null when it can.</summary>
    internal static string Problem(RequisitionItem item, decimal issued, string reason)
    {
        if (item.Material?.Kind != MaterialKind.Package) return "Only packaging lines can be issued over or under their requirement.";
        if (issued <= 0) return "The quantity issued must be more than zero.";
        if (issued == item.Quantity) return null;
        var text = reason?.Trim() ?? "";
        if (text.Length < MinimumReasonLength) return "Say why more or less than the requirement is issued.";
        return text.Length > 500 ? "Keep the reason to 500 characters." : null;
    }

    /// <summary>Stock on the reservation's batch and shelf that no other batch holds and this batch has not already set aside.</summary>
    internal static async Task<decimal> FreeOnShelf(ApplicationDbContext context, MaterialBatchReservedQuantity reservation,
        IEnumerable<MaterialBatchReservedQuantity> ownReservations)
    {
        var onShelf = await context.ShelfMaterialBatches.IgnoreQueryFilters()
            .Where(shelf => shelf.MaterialBatchId == reservation.MaterialBatchId && shelf.WarehouseLocationShelfId == reservation.WarehouseLocationShelfId
                && shelf.DeletedAt == null)
            .SumAsync(shelf => shelf.Quantity);
        var heldByOthers = await ProductionMaterialAllocator.UnissuedReservations(context)
            .Where(other => other.MaterialBatchId == reservation.MaterialBatchId && other.WarehouseLocationShelfId == reservation.WarehouseLocationShelfId
                && other.ProductionScheduleProductId != reservation.ProductionScheduleProductId)
            .SumAsync(other => other.Quantity);
        var heldHere = ownReservations.Where(own => own.MaterialBatchId == reservation.MaterialBatchId
            && own.WarehouseLocationShelfId == reservation.WarehouseLocationShelfId).Sum(own => own.Quantity);
        return onShelf - heldByOthers - heldHere;
    }

    /// <summary>
    /// Checks the line's issued quantity against the batches set aside for it and, unless <paramref name="checkOnly"/>, changes
    /// their quantities to it: an over is added to the last batch from its free shelf stock; an under is taken off from the
    /// last batch backwards. Reservation quantities are in the batch's unit; the line's are converted to it.
    /// </summary>
    internal static async Task<Result> Apply(ApplicationDbContext context, RequisitionItem item, decimal issued, string reason,
        IReadOnlyCollection<MaterialBatchReservedQuantity> reservations, bool checkOnly = false)
    {
        var problem = Problem(item, issued, reason);
        if (problem is not null) return Error.Validation("Stock.IssuedQuantity", $"{item.Material?.Name ?? "Packaging"}: {problem}");
        if (issued == item.Quantity) return Result.Success();
        var ordered = PickingOrder(reservations);
        if (ordered.Count == 0) return Error.Validation("Stock.IssuedQuantity", $"{item.Material?.Name}: no batch is set aside to issue from.");
        decimal? Convert(decimal value, MaterialBatchReservedQuantity row) =>
            row.UoMId == item.UoMId ? value : UomConverter.Convert(value, item.UoM?.Symbol, row.UoM?.Symbol);

        var difference = issued - item.Quantity;
        if (difference > 0)
        {
            var last = ordered[^1];
            var extra = Convert(difference, last);
            if (!extra.HasValue) return Error.Validation("Stock.Units", $"{item.Material?.Name}: the line's and the batch's units cannot be reconciled.");
            var free = await FreeOnShelf(context, last, ordered);
            if (free < extra.Value)
                return Error.Conflict("Stock.IssuedQuantity",
                    $"{item.Material?.Name}: batch {last.MaterialBatch?.BatchNumber} has {free} free on its shelf, so {extra.Value} more cannot be issued from it.");
            if (!checkOnly) last.Quantity += extra.Value;
            return Result.Success();
        }

        var remaining = -difference;
        var changes = new List<(MaterialBatchReservedQuantity Row, decimal Quantity)>();
        foreach (var row in Enumerable.Reverse(ordered))
        {
            if (remaining <= 0) break;
            var inLineUnit = row.UoMId == item.UoMId ? row.Quantity : UomConverter.Convert(row.Quantity, row.UoM?.Symbol, item.UoM?.Symbol);
            if (!inLineUnit.HasValue) return Error.Validation("Stock.Units", $"{item.Material?.Name}: the line's and the batch's units cannot be reconciled.");
            var taken = Math.Min(inLineUnit.Value, remaining);
            var inRowUnit = Convert(taken, row);
            if (!inRowUnit.HasValue) return Error.Validation("Stock.Units", $"{item.Material?.Name}: the line's and the batch's units cannot be reconciled.");
            changes.Add((row, Math.Max(0, row.Quantity - inRowUnit.Value)));
            remaining -= taken;
        }
        if (remaining > 0) return Error.Validation("Stock.IssuedQuantity", $"{item.Material?.Name}: less is set aside than would be left out.");
        if (!checkOnly) foreach (var (row, quantity) in changes) row.Quantity = quantity;
        return Result.Success();
    }
}
