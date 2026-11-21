using APP.Extensions;
using APP.Services.Production;
using APP.Utils;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.ProductionOrders;
using DOMAIN.Entities.ProformaInvoices;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.FinishedGoods;

public sealed partial class FinishedGoodsWorkflow(ApplicationDbContext db, APP.IRepository.IApprovalRepository approvals = null)
{
    private Task<Result> Write(string key, Func<Task<Result>> action) => FinishedGoodsStock.Write(db, key, null, action);
    private async Task<Result> Save(string key)
    {
        var denied = await FinishedGoodsStock.Require(db, key);
        if (denied is not null) return denied;
        await db.SaveChangesAsync();
        return Result.Success();
    }
    private Task<AllocateProductionOrder> Allocation(Guid id) => db.AllocateProductionOrders.IgnoreAutoIncludes().AsSplitQuery()
        .Include(a => a.ProductionOrder).ThenInclude(o => o.Customer)
        .Include(a => a.Products).ThenInclude(p => p.FulfilledQuantities).ThenInclude(q => q.FinishedGoodsTransferNote)
        .ThenInclude(n => n.BatchManufacturingRecord).ThenInclude(b => b.ProductionScheduleProduct)
        .FirstOrDefaultAsync(a => a.Id == id);
    private static Error Ready(AllocateProductionOrder allocation)
    {
        if (allocation is null) return Error.NotFound("FinishedGoods.Allocation", "Packing list not found.");
        if (allocation.ProductionOrder?.Customer is null) return Error.Validation("FinishedGoods.Customer", "The customer record must be active before this consignment progresses.");
        if (allocation.CancelledAt.HasValue) return Error.Validation("FinishedGoods.Cancelled", "This packing list has been cancelled.");
        var gate = allocation.EnsureApprovedForProgression("Packing list");
        if (gate.IsFailure) return gate.Error;
        if (allocation.Products.Count == 0 || allocation.Products.Any(p => p.FulfilledQuantities.Count == 0))
            return Error.Validation("FinishedGoods.EmptyAllocation", "A packing list must identify its product, packing style and source batch quantities.");
        foreach (var note in allocation.Products.SelectMany(p => p.FulfilledQuantities).Select(q => q.FinishedGoodsTransferNote).Distinct())
        {
            var problem = FinishedGoodsStock.Eligible(note);
            if (problem is not null) return problem;
        }
        return null;
    }

    public Task<Result> VerifyPo(Guid id, VerifyFinishedGoodsCustomerPoRequest request) => Write(PermissionKeys.CanVerifyFinishedGoodsCustomerPo, async () =>
    {
        var proforma = await db.ProformaInvoices.Include(p => p.Products).FirstOrDefaultAsync(p => p.Id == id);
        if (proforma is null) return Error.NotFound("FinishedGoods.Proforma", "Proforma not found.");
        if ((proforma.Status != ProformaInvoiceStatus.SentToCustomer && !(proforma.Status == ProformaInvoiceStatus.Invoice && !proforma.IssuedInvoiceId.HasValue)) || proforma.CustomerPoVerifiedAt.HasValue
            || await db.Invoices.AnyAsync(i => i.ProformaInvoiceId == id))
            return Error.Validation("FinishedGoods.PoState", "Verify the PO once against a sent, uninvoiced proforma.");
        if (string.IsNullOrWhiteSpace(request.CustomerPoNumber) || request.CustomerPoNumber.Length > 100
            || request.CustomerPoDate == default || request.CustomerPoDate.Date > DateTime.UtcNow.Date)
            return Error.Validation("FinishedGoods.CustomerPo", "Enter the customer's PO number and date.");
        if (!await db.AllocateProductionOrders.AnyAsync(a => a.Id == proforma.AllocateProductionOrderId && a.Approved && !a.CancelledAt.HasValue))
            return Error.Validation("FinishedGoods.Allocation", "The linked packing list must still be approved and active.");
        if (!await db.Attachments.AnyAsync(a => a.Id == request.AttachmentId && a.ModelId == id && a.ModelType == nameof(ProformaInvoice)))
            return Error.Validation("FinishedGoods.PoEvidence", "Attach this customer's purchase order to this proforma first.");
        if (request.Lines is null || request.Lines.Count != proforma.Products.Count || request.Lines.Select(l => l.ProformaInvoiceProductId).Distinct().Count() != request.Lines.Count
            || proforma.Products.Any(p => !request.Lines.Any(l => l.ProformaInvoiceProductId == p.Id && l.Quantity == p.Quantity
                && l.UnitPrice == p.UnitPrice && l.CurrencyId == p.PriceCurrencyId)))
            return Error.Validation("FinishedGoods.PoMatch", "PO product/pack quantities, prices and currencies must match the proforma. Resolve differences before verification.");
        proforma.CustomerPoNumber = request.CustomerPoNumber.Trim();
        proforma.CustomerPoDate = DateTime.SpecifyKind(request.CustomerPoDate.Date, DateTimeKind.Utc);
        proforma.CustomerPoAttachmentId = request.AttachmentId;
        proforma.CustomerPoVerifiedAt = DateTime.UtcNow; proforma.CustomerPoVerifiedById = db.CurrentActorId;
        var saved = await Save(PermissionKeys.CanVerifyFinishedGoodsCustomerPo);
        if (saved.IsFailure) return saved;
        var denied = await FinishedGoodsStock.Require(db, PermissionKeys.CanVerifyFinishedGoodsCustomerPo);
        if (denied is not null) return denied;
        if (approvals is not null) await approvals.CreateInitialApprovalsAsync(nameof(ProformaInvoice), proforma.Id, db.CurrentActorId);
        return Result.Success();
    });

    public Task<Result> Load(Guid id, LoadFinishedGoodsRequest request) => Write(PermissionKeys.CanLoadFinishedGoods, async () =>
    {
        var allocation = await Allocation(id); var problem = Ready(allocation); if (problem is not null) return problem;
        if (allocation.LoadedAt.HasValue || allocation.DispatchedAt.HasValue || allocation.DeliveredAt.HasValue)
            return Error.Conflict("FinishedGoods.Loaded", "These goods have already been loaded or dispatched.");
        if (!request.ConditionsConfirmed || new[] { request.VehicleRegistration, request.DriverName, request.DriverPhone, request.LoadingLocation, request.InspectionReference }.Any(string.IsNullOrWhiteSpace))
            return Error.Validation("FinishedGoods.LoadEvidence", "Record the vehicle, driver/contact, loading location, inspection and suitable loading/storage conditions.");
        var existingWaybill = await db.ProductionOrderWaybills.FirstOrDefaultAsync(w => w.AllocateProductionOrderId == id && !w.Voided);
        if (existingWaybill is not null && existingWaybill.VehicleRegistration != request.VehicleRegistration.Trim())
            return Error.Validation("FinishedGoods.Vehicle", "The loading vehicle must match the waybill vehicle.");
        allocation.LoadedVehicle = request.VehicleRegistration.Trim(); allocation.LoadedDriver = request.DriverName.Trim();
        allocation.LoadedDriverPhone = request.DriverPhone.Trim(); allocation.LoadingLocation = request.LoadingLocation.Trim();
        allocation.LoadingInspectionReference = request.InspectionReference.Trim();
        allocation.LoadedAt = DateTime.UtcNow; allocation.Status = AllocateProductionOrderStatus.Loaded;
        return await Save(PermissionKeys.CanLoadFinishedGoods);
    });

    public Task<Result> Waybill(Guid id, CreateProductionOrderWaybill request) => Write(PermissionKeys.CanCreateWaybillForFgw, async () =>
    {
        var allocation = await Allocation(id); var problem = Ready(allocation); if (problem is not null) return problem;
        if (allocation.DispatchedAt.HasValue || allocation.DeliveredAt.HasValue || await db.ProductionOrderWaybills.AnyAsync(w => w.AllocateProductionOrderId == id && !w.Voided))
            return Error.Conflict("FinishedGoods.Waybill", "A waybill exists or these goods have already departed.");
        var invoice = await db.Invoices.Include(i => i.ProformaInvoice).FirstOrDefaultAsync(i => i.Id == request.InvoiceId && !i.Voided);
        if (invoice is null || invoice.ProformaInvoice.AllocateProductionOrderId != id || !invoice.ProformaInvoice.CustomerPoVerifiedAt.HasValue)
            return Error.Validation("FinishedGoods.Invoice", "Choose the issued invoice against this allocation's verified customer PO.");
        if (new[] { request.Code, request.VehicleRegistration, request.DriverName, request.DriverPhone, request.Transporter,
            request.DeliveryAddress, request.StorageConditions }.Any(string.IsNullOrWhiteSpace))
            return Error.Validation("FinishedGoods.Transport", "Enter the waybill number, transporter, vehicle, driver/contact, delivery address and transport conditions.");
        if (request.Code.Length > 100) return Error.Validation("FinishedGoods.WaybillCode", "Use a waybill number of at most 100 characters.");
        if (await db.ProductionOrderWaybills.AnyAsync(w => w.Code == request.Code.Trim()))
            return Error.Conflict("FinishedGoods.WaybillCode", "This waybill number is already used.");
        if (allocation.LoadedAt.HasValue && allocation.LoadedVehicle != request.VehicleRegistration.Trim())
            return Error.Validation("FinishedGoods.Vehicle", "The waybill vehicle must match the vehicle holding the loaded goods.");
        db.ProductionOrderWaybills.Add(new ProductionOrderWaybill {
            AllocateProductionOrderId = id, InvoiceId = invoice.Id, Code = request.Code.Trim(), Comment = request.Comment,
            VehicleRegistration = request.VehicleRegistration.Trim(), DriverName = request.DriverName.Trim(), DriverPhone = request.DriverPhone.Trim(),
            Transporter = request.Transporter.Trim(), DeliveryAddress = request.DeliveryAddress.Trim(), StorageConditions = request.StorageConditions.Trim(), SealNumber = request.SealNumber,
        });
        return await Save(PermissionKeys.CanCreateWaybillForFgw);
    });

    public Task<Result> Clear(Guid id, ClearFinishedGoodsDispatchRequest request) => Write(PermissionKeys.CanClearFinishedGoodsDispatch, async () =>
    {
        var allocation = await Allocation(id); var problem = Ready(allocation); if (problem is not null) return problem;
        if (allocation.DispatchedAt.HasValue || allocation.CommercialClearedAt.HasValue)
            return Error.Conflict("FinishedGoods.Clearance", "This allocation is already cleared or dispatched.");
        if (!request.PaymentOrCreditConfirmed || string.IsNullOrWhiteSpace(request.Reason)
            || string.IsNullOrWhiteSpace(request.CustomerAuthorisationReference) || request.MinimumRemainingShelfLifeDays < 0 || request.MinimumRemainingShelfLifeDays > 3650)
            return Error.Validation("FinishedGoods.Clearance", "Confirm the agreed payment or credit terms and record the clearance basis.");
        if (!await db.Invoices.AnyAsync(i => i.ProformaInvoice.AllocateProductionOrderId == id && !i.Voided && i.ProformaInvoice.CustomerPoVerifiedAt.HasValue))
            return Error.Validation("FinishedGoods.Invoice", "Issue the invoice against the verified customer PO first.");
        allocation.CommercialClearedAt = DateTime.UtcNow; allocation.CommercialClearedById = db.CurrentActorId;
        allocation.CommercialClearanceReason = request.Reason.Trim();
        allocation.CustomerAuthorisationReference = request.CustomerAuthorisationReference.Trim();
        allocation.MinimumRemainingShelfLifeDays = request.MinimumRemainingShelfLifeDays;
        return await Save(PermissionKeys.CanClearFinishedGoodsDispatch);
    });

    public Task<Result> Dispatch(Guid id, DispatchFinishedGoodsRequest request) => Write(PermissionKeys.CanDispatchFinishedGoods, async () =>
    {
        var allocation = await Allocation(id); var problem = Ready(allocation); if (problem is not null) return problem;
        if (!allocation.LoadedAt.HasValue || !allocation.CommercialClearedAt.HasValue || !allocation.CostingConfirmedAt.HasValue)
            return Error.Validation("FinishedGoods.DispatchGate", "Load the goods and obtain costing confirmation and commercial clearance before departure.");
        if (allocation.DispatchedAt.HasValue || allocation.DeliveredAt.HasValue)
            return Error.Conflict("FinishedGoods.Dispatched", "These goods have already departed; stock cannot be issued twice.");
        if (!request.VehicleInspected || !request.ConditionsConfirmed || string.IsNullOrWhiteSpace(request.ExitReference) || request.ExitReference.Length > 200)
            return Error.Validation("FinishedGoods.Exit", "Record the gate/exit reference and confirm vehicle suitability and transport conditions.");
        if (allocation.Products.SelectMany(p => p.FulfilledQuantities).Any(q => q.FinishedGoodsTransferNote.BatchManufacturingRecord.ExpiryDate.Value.Date <= DateTime.UtcNow.Date.AddDays(allocation.MinimumRemainingShelfLifeDays)))
            return Error.Validation("FinishedGoods.ShelfLife", "This batch no longer meets the customer's remaining shelf-life requirement.");
        var waybill = await db.ProductionOrderWaybills.SingleOrDefaultAsync(w => w.AllocateProductionOrderId == id && !w.Voided);
        if (waybill is null || waybill.Voided || !await db.Invoices.AnyAsync(i => i.Id == waybill.InvoiceId && !i.Voided && i.ProformaInvoice.CustomerPoVerifiedAt.HasValue))
            return Error.Validation("FinishedGoods.Waybill", "A waybill linked to the issued invoice and verified PO is required.");
        foreach (var group in allocation.Products.SelectMany(p => p.FulfilledQuantities).GroupBy(q => q.FinishedGoodsTransferNoteId))
        {
            var note = group.First().FinishedGoodsTransferNote; var quantity = group.Sum(q => q.YieldQuantity);
            if (quantity <= 0 || quantity > note.ReservedQuantity || quantity > note.PhysicalQuantity)
                return Error.Validation("FinishedGoods.Stock", "The reserved batch quantity cannot be reconciled. Reconcile the packing list before departure.");
            note.DispatchedQuantity += quantity;
            await FinishedGoodsStock.Post(db, note, -quantity, "FgDispatch", id.ToString(), db.CurrentActorId.Value,
                $"Dispatch {waybill.Code}; exit {request.ExitReference.Trim()}", waybill.Code);
        }
        allocation.DispatchedAt = DateTime.UtcNow; allocation.DispatchedById = db.CurrentActorId;
        allocation.ExitReference = request.ExitReference.Trim(); allocation.Status = AllocateProductionOrderStatus.Dispatched;
        return await Save(PermissionKeys.CanDispatchFinishedGoods);
    });

    public Task<Result> Deliver(Guid id, DeliverFinishedGoodsRequest request) => Write(PermissionKeys.CanDeliverFinishedGoods, async () =>
    {
        var allocation = await Allocation(id);
        if (allocation is null) return Error.NotFound("FinishedGoods.Allocation", "Packing list not found.");
        if (!allocation.DispatchedAt.HasValue || allocation.DeliveredAt.HasValue)
            return Error.Validation("FinishedGoods.Delivery", "Delivery requires a dispatched consignment and can be recorded once.");
        if (string.IsNullOrWhiteSpace(request.ReceiverName) || string.IsNullOrWhiteSpace(request.EvidenceReference) || string.IsNullOrWhiteSpace(request.ConditionNotes))
            return Error.Validation("FinishedGoods.Proof", "Record the receiver, proof of delivery reference and product/transport condition.");
        var quantities = allocation.Products.SelectMany(p => p.FulfilledQuantities).GroupBy(q => q.FinishedGoodsTransferNoteId).ToDictionary(g => g.Key, g => g.Sum(q => q.YieldQuantity));
        if (request.Lines is null || request.Lines.Count != quantities.Count || request.Lines.Select(l => l.FinishedGoodsTransferNoteId).Distinct().Count() != request.Lines.Count
            || request.Lines.Any(l => !quantities.TryGetValue(l.FinishedGoodsTransferNoteId, out var expected) || l.AcceptedYield < 0 || l.DamagedYield < 0
                || l.ShortYield < 0 || l.AcceptedYield + l.DamagedYield + l.ShortYield != expected))
            return Error.Validation("FinishedGoods.Reconciliation", "For every dispatched batch, accepted, damaged and short quantities must reconcile exactly to the dispatched quantity.");
        allocation.DeliveryReceiver = request.ReceiverName.Trim(); allocation.DeliveryEvidence = request.EvidenceReference.Trim();
        allocation.DeliveryCondition = request.ConditionNotes.Trim(); allocation.DeliveryLines = request.Lines;
        allocation.DeliveredAt = DateTime.UtcNow; allocation.Status = AllocateProductionOrderStatus.Delivered;
        return await Save(PermissionKeys.CanDeliverFinishedGoods);
    });
}
