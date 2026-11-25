using DOMAIN.Entities.BinCards;
using DOMAIN.Entities.Materials.Batch;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

/// <summary>Prepares bin-card entries before the caller's final permission check and atomic stock write.</summary>
internal static class StockTransferLedger
{
    internal sealed record Posting(MaterialBatch Batch, Guid WarehouseId, decimal Received, decimal Issued,
        string Description, Guid ActorId, DateTime At, decimal? Density);

    internal static async Task<Result<List<BinCardInformation>>> Prepare(ApplicationDbContext context, IEnumerable<Posting> postings)
    {
        var cards = new List<BinCardInformation>();
        foreach (var posting in postings)
        {
            var batch = posting.Batch;
            var history = await context.BinCardInformation.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
                .Where(card => card.DeletedAt == null && card.WarehouseId == posting.WarehouseId
                    && card.MaterialBatch.MaterialId == batch.MaterialId)
                .Select(card => new { card.QuantityReceived, card.QuantityIssued,
                    Unit = card.UoMId == null ? card.MaterialBatch.UoM.Symbol : card.UoM.Symbol })
                .ToListAsync();
            var quantities = history.Select(card => MaterialDensity.Convert(card.QuantityReceived - card.QuantityIssued,
                card.Unit, batch.UoM.Symbol, posting.Density)).ToList();
            // A lot can be split across several units. Never add raw kg, mg or litres together.
            quantities.AddRange(cards.Where(card => card.WarehouseId == posting.WarehouseId && card.MaterialBatch.MaterialId == batch.MaterialId)
                .Select(card => MaterialDensity.Convert(card.QuantityReceived - card.QuantityIssued, card.UoM.Symbol, batch.UoM.Symbol, posting.Density)));
            if (quantities.Any(quantity => quantity is null))
                return Error.Conflict("StockTransfer.LedgerUnit", "The store's bin-card units cannot be reconciled. Review its ledger before transferring stock.");
            var metadata = await context.BinCardInformation.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
                .Where(card => card.MaterialBatchId == batch.Id && card.DeletedAt == null)
                .OrderBy(card => card.CreatedAt).ThenBy(card => card.Id)
                .Select(card => new { card.ArNumber, card.Supplier, card.Manufacturer }).FirstOrDefaultAsync();
            var arNumber = await context.MaterialSamplings.IgnoreAutoIncludes().AsNoTracking()
                .Where(sample => sample.MaterialBatchId == batch.Id).OrderByDescending(sample => sample.CreatedAt)
                .Select(sample => sample.ArNumber).FirstOrDefaultAsync();
            cards.Add(new BinCardInformation
            {
                MaterialBatchId = batch.Id, MaterialBatch = batch, WarehouseId = posting.WarehouseId,
                UoMId = batch.UoMId, UoM = batch.UoM, QuantityReceived = posting.Received, QuantityIssued = posting.Issued,
                BalanceQuantity = quantities.Sum(quantity => quantity.GetValueOrDefault()) + posting.Received - posting.Issued,
                Description = posting.Description, ArNumber = arNumber ?? metadata?.ArNumber,
                Supplier = metadata?.Supplier, Manufacturer = metadata?.Manufacturer,
                CreatedAt = posting.At, CreatedById = posting.ActorId,
            });
        }
        return cards;
    }
}
