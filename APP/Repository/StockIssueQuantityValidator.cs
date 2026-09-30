using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Requisitions;
using SHARED;

namespace APP.Repository;

internal static class StockIssueQuantityValidator
{
    public static Result Validate(
        Requisition requisition,
        IReadOnlyCollection<MaterialBatchReservedQuantity> reservations
    )
    {
        if (requisition.Items.GroupBy(item => item.MaterialId).Any(group => group.Count() > 1))
            return Error.Validation(
                "Stock.DuplicateMaterial",
                "A stock requisition cannot issue the same material more than once."
            );

        var quantitiesByMaterial = reservations
            .GroupBy(reservation => reservation.MaterialBatch.MaterialId)
            .ToDictionary(group => group.Key, group => group.Sum(reservation => reservation.Quantity));

        foreach (var item in requisition.Items)
        {
            if (!quantitiesByMaterial.TryGetValue(item.MaterialId, out var reservedQuantity))
                return Error.Validation(
                    "Stock.Requisition",
                    $"No reserved quantities to issue for {item.Material?.Name ?? "item"}"
                );

            if (item.Quantity <= 0 || reservedQuantity != item.Quantity)
                return Error.Validation(
                    "Stock.ReservationMismatch",
                    $"Reserved quantity does not match the approved requisition quantity for {item.Material?.Name ?? "item"}."
                );
        }

        if (quantitiesByMaterial.Keys.Except(requisition.Items.Select(item => item.MaterialId)).Any())
            return Error.Validation(
                "Stock.UnrequestedReservation",
                "The production reservation contains a material not requested by this requisition."
            );

        return Result.Success();
    }
}
