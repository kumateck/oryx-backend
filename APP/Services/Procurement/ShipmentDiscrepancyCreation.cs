using APP.IRepository;
using DOMAIN.Entities.Shipments;
using DOMAIN.Entities.Shipments.Request;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;
using System.Data;

namespace APP.Services.Procurement;

public static class ShipmentDiscrepancyCreation
{
    public static async Task<Result<Guid>> Create(ApplicationDbContext context,
        IApprovalRepository approvals, CreateShipmentDiscrepancy request, Guid actorId)
    {
        if (request.Items is not { Count: > 0 })
            return Error.Validation("Items.Required", "At least one discrepancy line is required.");
        if (request.Items.Any(item => item.ReceivedQuantity < 0 ||
            string.IsNullOrWhiteSpace(item.Reason)))
            return Error.Validation("Items.Invalid", "Physical count must be nonnegative and reason is required.");
        if (!await context.Approvals.AnyAsync(item =>
            item.ItemType == nameof(ShipmentDiscrepancy) &&
            item.ApprovalStages.Any(stage => stage.Required)))
            return Error.Validation("Approval.Required",
                "Configure a required ShipmentDiscrepancy approval workflow first.");

        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;

        var document = await context.ShipmentDocuments
            .Include(item => item.ShipmentInvoice).ThenInclude(invoice => invoice.Items)
            .SingleOrDefaultAsync(item => item.Id == request.ShipmentDocumentId);
        if (document?.ShipmentInvoice is null)
            return Error.NotFound("ShipmentDocument.NotFound", "Shipment invoice document not found.");

        var lines = new List<ShipmentDiscrepancyItem>();
        foreach (var item in request.Items)
        {
            var matches = document.ShipmentInvoice.Items.Where(invoiceItem =>
                invoiceItem.MaterialId == item.MaterialId && invoiceItem.UoMId == item.UoMId &&
                (!item.ShipmentInvoiceItemId.HasValue ||
                    invoiceItem.Id == item.ShipmentInvoiceItemId.Value)).ToList();
            if (matches.Count != 1)
                return Error.Validation("ShipmentInvoiceItem.Invalid",
                    "Select one exact invoice line for each discrepancy.");
            var source = matches[0];
            if (item.ReceivedQuantity == source.ReceivedQuantity)
                return Error.Validation("Items.NoVariance", "A discrepancy line must differ from the invoice quantity.");
            if (lines.Any(line => line.ShipmentInvoiceItemId == source.Id))
                return Error.Validation("Items.Duplicate", "An invoice line can occur only once in a case.");
            if (await context.ShipmentDiscrepancies.AnyAsync(existing =>
                !existing.Rejected && existing.Items.Any(line =>
                    line.ShipmentInvoiceItemId == source.Id)))
                return Error.Validation("Items.AlreadyReported",
                    "An active discrepancy already exists for this invoice line.");
            lines.Add(new ShipmentDiscrepancyItem
            {
                MaterialId = source.MaterialId,
                UoMId = source.UoMId,
                ShipmentInvoiceItemId = source.Id,
                DeclaredQuantitySnapshot = source.ReceivedQuantity,
                ReceivedQuantity = item.ReceivedQuantity,
                TypeId = item.TypeId,
                Reason = item.Reason.Trim(),
                CreatedById = actorId,
                CreatedAt = DateTime.UtcNow,
            });
        }

        var discrepancy = new ShipmentDiscrepancy
        {
            ShipmentDocumentId = document.Id,
            CreatedById = actorId,
            CreatedAt = DateTime.UtcNow,
            Items = lines,
        };
        context.ShipmentDiscrepancies.Add(discrepancy);
        await context.SaveChangesAsync();
        await approvals.CreateInitialApprovalsAsync(nameof(ShipmentDiscrepancy),
            discrepancy.Id, actorId);
        if (transaction is not null) await transaction.CommitAsync();
        return discrepancy.Id;
    }
}
