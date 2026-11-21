using System.Reflection;
using API.Controllers;
using APP.Repository;
using APP.Services.FinishedGoods;
using APP.Tests.Repository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Customers;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.FinishedGoods;
using DOMAIN.Entities.Invoices;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.ProductionOrders;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.ProductionSchedules.Packing;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.ProformaInvoices;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SHARED;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.FinishedGoods;

public sealed class FinishedGoodsWorkflowTests
{
    private sealed class Actor : ICurrentUserService {
        public Guid? UserId { get; set; } = Guid.NewGuid(); public Guid? DepartmentId { get; set; }
        public string DepartmentType => string.Empty;
    }
    private readonly Actor actor = new();
    private readonly ApplicationDbContext db;
    private readonly IMapper mapper = new MapperConfiguration(c => {
        c.CreateMap<CreateInvoice, Invoice>(); c.CreateMap<CreateInvoiceAmount, InvoiceAmount>();
        c.CreateMap<CreateFinishedGoodsTransferNoteRequest, FinishedGoodsTransferNote>();
    }).CreateMapper();
    private FinishedGoodsWorkflow Flow => new(db);
    private ProductionOrderRepository Orders => new(db, mapper, FinishedGoodsYieldTests.NoApprovals.Create());
    private ProductionScheduleRepository Schedules => new(db, mapper, null!, null!, null!, FinishedGoodsYieldTests.NoApprovals.Create(), new NoOpProductionActivityStepEventPublisher());
    public FinishedGoodsWorkflowTests() => db = new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, actor);

    private async Task<(FinishedGoodsTransferNote Note, AllocateProductionOrder Allocation, ProformaInvoice Proforma, Guid Currency, Guid Attachment)> Seed(params string[] keys)
    {
        var role = new Role { Id = Guid.NewGuid(), Name = "FG officer" };
        var dept = new Department { Id = Guid.NewGuid(), Name = "Production", Type = DepartmentType.Production, Division = (Division)1 };
        actor.DepartmentId = dept.Id;
        db.AddRange(dept, role, new User { Id = actor.UserId!.Value, UserName = "operator", DepartmentId = dept.Id });
        db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = actor.UserId.Value, RoleId = role.Id });
        var count = 0;
        foreach (var key in keys) { var claim = new IdentityRoleClaim<Guid> { Id = ++count, RoleId = role.Id, ClaimType = AppConstants.Permission, ClaimValue = key }; db.RoleClaims.Add(claim);
            db.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), RoleClaimId = claim.Id, Key = key, Type = "Edit" }); }
        var unit = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Tablet", Symbol = "tablet" };
        var product = new Product { Id = Guid.NewGuid(), Name = "Product", Code = "P-1", DepartmentId = dept.Id, Division = dept.Division, BaseUomId = unit.Id };
        var packing = new ProductPacking { Id = Guid.NewGuid(), ProductId = product.Id, Name = "100 tablets", PackPerShipper = 80, BasePackingUomId = unit.Id };
        var scheduled = new ProductionScheduleProduct { Id = Guid.NewGuid(), ProductId = product.Id };
        var bmr = new BatchManufacturingRecord { Id = Guid.NewGuid(), ProductionScheduleProductId = scheduled.Id, BatchNumber = "B-1", Status = BatchManufacturingStatus.Approved, ExpiryDate = DateTime.UtcNow.AddYears(1) };
        var source = new Warehouse { Id = Guid.NewGuid(), Name = "Production", DepartmentId = dept.Id, Type = WarehouseType.Production, Division = dept.Division };
        var store = new Warehouse { Id = Guid.NewGuid(), Name = "FG", Type = WarehouseType.FinishedGoodsStorage, Division = dept.Division };
        var note = new FinishedGoodsTransferNote { Id = Guid.NewGuid(), BatchManufacturingRecordId = bmr.Id, TotalQuantity = 10000, QuantityReceived = 9800,
            QuantityPerPack = 100, ProductPackingId = packing.Id, UoMId = unit.Id, IsApproved = true, Approved = true,
            AllocatedQuantity = 6000, TransferNoteNumber = "FGTN-1", FromWarehouseId = source.Id, ToWarehouseId = store.Id, PackingStyleSnapshot = packing.Name };
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Hospital", Email = "customer@example.test" };
        var order = new ProductionOrder { Id = Guid.NewGuid(), Approved = true, CustomerId = customer.Id,
            Products = [new() { ProductId = product.Id, ProductPackingId = packing.Id, TotalOrderQuantity = 100 }] };
        var allocation = new AllocateProductionOrder { Id = Guid.NewGuid(), ProductionOrderId = order.Id, Approved = true,
            Products = [new() { ProductId = product.Id, ProductPackingId = packing.Id, FulfilledQuantities = [new() { FinishedGoodsTransferNoteId = note.Id, Quantity = 60, UnitsPerPack = 100, YieldQuantity = 6000 }] }] };
        var currency = new Currency { Id = Guid.NewGuid(), Name = "Cedi", Symbol = "GHS", IsBaseCurrency = true };
        var proforma = new ProformaInvoice { Id = Guid.NewGuid(), Code = "PF-1", AllocateProductionOrderId = allocation.Id, Approved = true, Status = ProformaInvoiceStatus.SentToCustomer,
            Products = [new() { Id = Guid.NewGuid(), ProductId = product.Id, ProductPackingId = packing.Id, Quantity = 60, UnitPrice = 12, PriceCurrencyId = currency.Id }] };
        var attachment = new Attachment { Id = Guid.NewGuid(), ModelId = proforma.Id, ModelType = nameof(ProformaInvoice), Name = "Customer PO" };
        db.AddRange(unit, product, packing, scheduled, bmr, source, store, note, customer, order, allocation, currency, proforma, attachment);
        await db.SaveChangesAsync();
        return (note, allocation, proforma, currency.Id, attachment.Id);
    }
    private static string[] All => [PermissionKeys.CanApproveFgtn, PermissionKeys.CanCreateFgtn, PermissionKeys.CanVerifyFinishedGoodsCustomerPo,
        PermissionKeys.CanCreateInvoice, PermissionKeys.CanLoadFinishedGoods, PermissionKeys.CanCreateWaybillForFgw, PermissionKeys.CanClearFinishedGoodsDispatch,
        PermissionKeys.CanConfirmFinishedGoodsCosting, PermissionKeys.CanDispatchFinishedGoods, PermissionKeys.CanDeliverFinishedGoods, PermissionKeys.CanCreateProductRecall,
        PermissionKeys.CanNotifyProductRecall, PermissionKeys.CanRecordProductRecallRecovery, PermissionKeys.CanDisposeProductRecallRecovery, PermissionKeys.CanCloseProductRecall];

    [Fact]
    public void Every_endpoint_requires_one_specific_registered_permission()
    {
        var catalog = PermissionUtils.GeneratePermissions().Select(p => p.Key).ToList();
        foreach (var action in typeof(FinishedGoodsController).GetMethods().Where(m => m.DeclaringType == typeof(FinishedGoodsController))) {
            Assert.Empty(action.GetCustomAttributes<AllowAnonymousAttribute>());
            var key = Assert.Single(action.GetCustomAttributes<AuthorizeAttribute>()).Policy;
            Assert.NotNull(key); Assert.Single(catalog, key);
        }
    }
    [Fact]
    public async Task Missing_or_revoked_permission_cannot_change_loading_or_stock()
    {
        var f = await Seed();
        Assert.Equal(ErrorType.Forbidden, (await Flow.Load(f.Allocation.Id, Loading())).Error.Type);
        Assert.Null(f.Allocation.LoadedAt); Assert.Equal(9800, f.Note.PhysicalQuantity);
    }
    [Fact]
    public async Task Loading_is_allowed_before_the_waybill_but_departure_is_gated()
    {
        var f = await Seed(All);
        Assert.True((await Flow.Load(f.Allocation.Id, Loading())).IsSuccess);
        Assert.Equal(9800, f.Note.PhysicalQuantity); Assert.Equal(6000, f.Note.ReservedQuantity); Assert.Equal(3800, f.Note.AvailableQuantity);
        Assert.True((await Flow.Dispatch(f.Allocation.Id, new() { ExitReference = "GATE-1", VehicleInspected = true, ConditionsConfirmed = true })).IsFailure);
        Assert.Equal(0, f.Note.DispatchedQuantity);
    }
    [Fact]
    public async Task Verified_po_invoice_clearance_waybill_dispatch_and_delivery_reconcile_once()
    {
        var f = await Seed(All);
        Assert.True((await Flow.VerifyPo(f.Proforma.Id, Po(f.Proforma, f.Currency, f.Attachment))).IsSuccess);
        var issued = await Orders.CreateInvoice(new() { Code = "INV-1", ProformaInvoiceId = f.Proforma.Id, CustomerId = f.Allocation.ProductionOrder.CustomerId });
        Assert.True(issued.IsSuccess, issued.IsFailure ? issued.Error.Description : null);
        var invoice = await db.Invoices.Include(i => i.Amounts).SingleAsync(); Assert.Equal(720, invoice.Amounts.Single().Amount);
        Assert.True((await Flow.Load(f.Allocation.Id, Loading())).IsSuccess);
        Assert.True((await Flow.Clear(f.Allocation.Id, new() { Reason = "Approved credit terms", PaymentOrCreditConfirmed = true, CustomerAuthorisationReference = "LIC-1", MinimumRemainingShelfLifeDays = 30 })).IsSuccess);
        Assert.True((await Flow.Waybill(f.Allocation.Id, new() { InvoiceId = issued.Value, Code = "WB-1", VehicleRegistration = "GR-1", DriverName = "Driver", DriverPhone = "123", Transporter = "Carrier", DeliveryAddress = "Hospital receiving", StorageConditions = "Below 30 C" })).IsSuccess);
        Assert.True((await Flow.ConfirmCosting(f.Allocation.Id, new() { ApprovedBasisConfirmed = true, Lines = [new() {
            ProductId = f.Allocation.Products[0].ProductId, ProductPackingId = f.Allocation.Products[0].ProductPackingId.Value,
            ManufacturingCostPerPack = 8, CurrencyId = f.Currency, ApprovedCostBasisReference = "Approved batch cost BC-1" }] })).IsSuccess);
        var departure = new DispatchFinishedGoodsRequest { ExitReference = "EXIT-1", VehicleInspected = true, ConditionsConfirmed = true };
        var dispatch = await Flow.Dispatch(f.Allocation.Id, departure); Assert.True(dispatch.IsSuccess, dispatch.IsFailure ? dispatch.Error.Description : null);
        Assert.Equal(3800, f.Note.PhysicalQuantity); Assert.Equal(0, f.Note.ReservedQuantity); Assert.Equal(3800, f.Note.AvailableQuantity);
        Assert.Equal(6000, f.Note.DispatchedQuantity);
        Assert.Single(await db.InventoryLedgers.Where(l => l.TransactionType == "FgDispatch").ToListAsync());
        Assert.True((await Flow.Dispatch(f.Allocation.Id, departure)).IsFailure);
        Assert.Single(await db.InventoryLedgers.Where(l => l.TransactionType == "FgDispatch").ToListAsync());
        var delivery = await Flow.Deliver(f.Allocation.Id, new() { ReceiverName = "Receiver", EvidenceReference = "Signed POD-1", ConditionNotes = "100 tablets damaged", Lines = [new() { FinishedGoodsTransferNoteId = f.Note.Id, AcceptedYield = 5900, DamagedYield = 100 }] });
        Assert.True(delivery.IsSuccess, delivery.IsFailure ? delivery.Error.Description : null);
    }
    [Fact]
    public async Task Receipt_posts_actual_count_and_preserves_sent_count()
    {
        var f = await Seed(All); f.Note.IsApproved = false; f.Note.QuantityReceived = 0; f.Note.AllocatedQuantity = 0; await db.SaveChangesAsync();
        var result = await Schedules.ApproveTransferNote(f.Note.Id, new() { QuantityReceived = 9000, Notes = "Ten packs short" });
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Description : null);
        Assert.Equal(10000, f.Note.TotalQuantity); Assert.Equal(9000, f.Note.PhysicalQuantity);
        Assert.Equal(9000, (await db.ProductBinCardInformation.SingleAsync()).QuantityReceived);
        Assert.Equal(9000, (await db.FinishedProductBatchMovements.SingleAsync()).Quantity);
        Assert.Equal(actor.UserId, f.Note.AcceptedById);
    }
    [Fact]
    public async Task Recall_blocks_loading_and_dispatch_and_returns_stay_quarantined()
    {
        var f = await Seed(All);
        f.Note.DispatchedQuantity = 6000; f.Allocation.DispatchedAt = DateTime.UtcNow; await db.SaveChangesAsync();
        var opened = await Flow.Recall(new() { BatchManufacturingRecordId = f.Note.BatchManufacturingRecordId, Code = "RC-1", Reason = "Quality defect", RegulatoryClassification = "Pending authority classification", AuthorityReference = "FDA notification reference" });
        Assert.True(opened.IsSuccess); Assert.True(f.Note.IsOnHold); Assert.Equal(0, f.Note.AvailableQuantity);
        Assert.True((await Flow.Load(f.Allocation.Id, Loading())).IsFailure);
        var recall = await db.FinishedGoodsRecalls.SingleAsync();
        Assert.True((await Flow.Recover(recall.Id, new() { AllocationId = f.Allocation.Id, FinishedGoodsTransferNoteId = f.Note.Id, Quantity = 200, Condition = "Sealed packs", Evidence = "Return receipt RR-1" })).IsSuccess);
        var recoveredNote = await db.FinishedGoodsTransferNotes.Include(n => n.BatchManufacturingRecord).SingleAsync();
        Assert.Equal(4000, recoveredNote.PhysicalQuantity); Assert.Equal(200, recoveredNote.QuarantinedReturnQuantity); Assert.Equal(0, recoveredNote.AvailableQuantity);
        Assert.True((await Flow.CloseRecall(recall.Id, new() { Evidence = "Report" })).IsFailure);
    }
    [Theory]
    [InlineData(BatchManufacturingStatus.Testing, false)]
    [InlineData(BatchManufacturingStatus.Quarantine, false)]
    [InlineData(BatchManufacturingStatus.Approved, true)]
    [InlineData(BatchManufacturingStatus.Available, true)]
    public async Task Only_quality_released_unexpired_batches_can_load(BatchManufacturingStatus status, bool allowed)
    {
        var f = await Seed(All); f.Note.BatchManufacturingRecord.Status = status; await db.SaveChangesAsync();
        Assert.Equal(allowed, (await Flow.Load(f.Allocation.Id, Loading())).IsSuccess);
    }
    private static VerifyFinishedGoodsCustomerPoRequest Po(ProformaInvoice proforma, Guid currency, Guid attachment) => new() {
        CustomerPoNumber = "CPO-1", CustomerPoDate = DateTime.UtcNow.Date, AttachmentId = attachment,
        Lines = proforma.Products.Select(p => new VerifyFinishedGoodsCustomerPoLine { ProformaInvoiceProductId = p.Id, Quantity = p.Quantity, UnitPrice = p.UnitPrice!.Value, CurrencyId = currency }).ToList() };

    [Fact]
    public async Task Every_changed_endpoint_policy_allows_its_current_grant_and_refuses_missing_or_revoked_grants()
    {
        await Seed();
        var methods = typeof(FinishedGoodsController).GetMethods().Where(m => m.DeclaringType == typeof(FinishedGoodsController)).ToList();
        foreach (var name in new[] { "GetApprovedProducts", "GetApprovedProduct", "GetApprovedProductDetails", "CreateFinishedGoodsTransferNote", "ApproveTransferNote", "CreateFinishedGoodsTransferNoteQuantity" })
            methods.Add(typeof(ProductionScheduleController).GetMethod(name)!);
        foreach (var name in new[] { "UpdateProformaInvoice", "DeleteProformaInvoice", "SendWaybillToCustomer", "GetInvoice", "GetInvoices" }) methods.Add(typeof(ProductionOrderController).GetMethod(name)!);
        var keys = methods.Select(m => Assert.Single(m.GetCustomAttributes<AuthorizeAttribute>()).Policy!).Distinct().ToList();
        var role = await db.UserRoles.Select(r => r.RoleId).SingleAsync(); var index = 100;
        async Task<bool> Granted(string key) {
            var policy = new AuthorizationHandlerContext([new APP.Claims.PermissionRequirement(key)],
                new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity([
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, actor.UserId.ToString()!)], "test")), null);
            await new APP.Claims.PermissionAuthorizationHandler(db).HandleAsync(policy); return policy.HasSucceeded;
        }
        foreach (var key in keys) {
            Assert.False(await Granted(key));
            var claim = new IdentityRoleClaim<Guid> { Id = ++index, RoleId = role, ClaimType = AppConstants.Permission, ClaimValue = key };
            var access = new PermissionType { Id = Guid.NewGuid(), RoleClaimId = claim.Id, Key = key, Type = "Edit" };
            db.AddRange(claim, access); await db.SaveChangesAsync(); Assert.True(await Granted(key));
            db.RemoveRange(claim, access); await db.SaveChangesAsync(); Assert.False(await Granted(key));
        }
    }

    [Fact]
    public async Task Generation_posts_no_receipt_and_uses_cumulative_final_packing_capacity()
    {
        var f = await Seed(All);
        var bmr = f.Note.BatchManufacturingRecord; bmr.BatchQuantity = 50000;
        var shipper = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Shipper", Symbol = "shipper" };
        var box = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Box", Symbol = "box" };
        f.Note.ProductPacking.PackingLists = [new() { UomId = shipper.Id, Quantity = 80, Order = 1 }, new() { UomId = box.Id, Quantity = 100, Order = 2 }];
        var activity = new ProductionActivity { Id = Guid.NewGuid(), ProductionScheduleProductId = bmr.ProductionScheduleProductId };
        var operation = new Operation { Id = Guid.NewGuid(), Name = "FG transfer" };
        var step = new ProductionActivityStep { Id = Guid.NewGuid(), ProductionActivityId = activity.Id, OperationId = operation.Id };
        db.AddRange(shipper, box, operation, activity, step, new FinalPacking { Id = Guid.NewGuid(), ProductionScheduleProductId = bmr.ProductionScheduleProductId,
            ProductPackingId = f.Note.ProductPackingId, TotalQuantityPacked = 15000 });
        await db.SaveChangesAsync();
        CreateFinishedGoodsTransferNoteRequest Request(decimal quantity) => new() { TransferNoteNumber = "FGTN-2", BatchManufacturingRecordId = bmr.Id,
            ProductPackingId = f.Note.ProductPackingId, ProductionActivityStepId = step.Id, IsPartial = true, TotalQuantity = quantity };
        Assert.True((await Schedules.CreateFinishedGoodsTransferNote(Request(6000), actor.UserId!.Value)).IsFailure);
        var result = await Schedules.CreateFinishedGoodsTransferNote(Request(5000), actor.UserId!.Value);
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Description : null);
        var created = await db.FinishedGoodsTransferNotes.SingleAsync(n => n.TransferNoteNumber == "FGTN-2");
        Assert.Equal(100, created.QuantityPerPack); Assert.Equal(80, created.PacksPerShipperSnapshot);
        Assert.Equal(0, created.StockQuantity); Assert.Equal(0, created.PhysicalQuantity);
        Assert.Empty(db.ProductBinCardInformation); Assert.Empty(db.FinishedProductBatchMovements); Assert.Empty(db.DistributedFinishedProducts);
    }

    [Fact]
    public async Task A_zero_receipt_requires_discrepancy_evidence_and_never_uses_the_sent_count_as_stock()
    {
        var f = await Seed(All); f.Note.IsApproved = false; f.Note.QuantityReceived = 0; f.Note.AllocatedQuantity = 0; await db.SaveChangesAsync();
        Assert.True((await Schedules.ApproveTransferNote(f.Note.Id, new() { QuantityReceived = 0 })).IsFailure);
        var result = await Schedules.ApproveTransferNote(f.Note.Id, new() { QuantityReceived = 0, Notes = "No goods received. Discrepancy report DR-1 raised for QA investigation." });
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Description : null);
        var note = await db.FinishedGoodsTransferNotes.SingleAsync();
        Assert.Equal(10000, note.TotalQuantity); Assert.Equal(0, note.StockQuantity); Assert.Equal(0, note.RemainingQuantity);
    }

    [Fact]
    public async Task A_QA_hold_is_batch_wide_and_never_released_by_a_finance_permission()
    {
        var f = await Seed(PermissionKeys.CanHoldFinishedGoods, PermissionKeys.CanReleaseFinishedGoodsHold);
        Assert.True((await Flow.Hold(f.Note.Id, new() { Hold = true, Reason = "QA investigation Q-1" })).IsSuccess);
        Assert.Equal(0, f.Note.AvailableQuantity);
        Assert.True((await Flow.Hold(f.Note.Id, new() { Hold = false, Reason = "QA release Q-1" })).IsSuccess);
        Assert.Equal(3800, f.Note.AvailableQuantity);
        db.RoleClaims.RemoveRange(db.RoleClaims); await db.SaveChangesAsync();
        Assert.Equal(ErrorType.Forbidden, (await Flow.Hold(f.Note.Id, new() { Hold = true, Reason = "Denied" })).Error.Type);
    }

    [Fact]
    public async Task Cancelling_an_undeparted_list_releases_reservations_without_issuing_stock()
    {
        var f = await Seed(PermissionKeys.CanCancelFinishedGoodsAllocation);
        var result = await Flow.Cancel(f.Allocation.Id, new() { Evidence = "Truck unloaded and all 60 packs counted back into batch B-1, bay A." });
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Description : null);
        Assert.Equal(9800, f.Note.PhysicalQuantity); Assert.Equal(0, f.Note.AllocatedQuantity);
        Assert.Equal(9800, f.Note.AvailableQuantity); Assert.Equal(AllocateProductionOrderStatus.Cancelled, f.Allocation.Status);
        Assert.Equal(100, f.Allocation.ProductionOrder.Products.Single().RemainingQuantity);
        Assert.Empty(db.FinishedProductBatchMovements);
    }

    [Fact]
    public async Task Reconciliation_pins_the_verified_pack_and_preserves_the_original_sent_quantity()
    {
        var f = await Seed(PermissionKeys.CanReconcileFinishedGoodsStock);
        f.Note.RequiresStockReconciliation = true; await db.SaveChangesAsync();
        var result = await Flow.Reconcile(f.Note.Id, new() { CountedPhysicalYield = 3800, VerifiedUnitsPerPack = 100, VerifiedPacksPerShipper = 80,
            VerifiedPackingStyleName = "100 tablets", Evidence = "Signed physical count SC-1 and gate exit EX-1 reconciled.",
            Departures = [new() { AllocationId = f.Allocation.Id, DepartedAt = DateTime.UtcNow.AddHours(-1), Evidence = "Gate exit EX-1" }] });
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Description : null);
        Assert.Equal(10000, f.Note.TotalQuantity); Assert.Equal(3800, f.Note.PhysicalQuantity); Assert.Equal(3800, f.Note.AvailableQuantity);
        Assert.Equal(6000, f.Note.DispatchedQuantity); Assert.False(f.Note.RequiresStockReconciliation);
        Assert.Equal(60, f.Allocation.ProductionOrder.Products.Single().FulfilledQuantities.Single().Quantity);
        Assert.Equal(3800, (await db.ProductBinCardInformation.SingleAsync()).BalanceQuantity);
    }

    [Fact]
    public async Task The_complete_recall_cycle_retains_closure_history_and_keeps_stock_blocked()
    {
        var f = await Seed([..All, PermissionKeys.CanReopenProductRecall, PermissionKeys.CanDisposeRecalledFinishedGoodsStock]);
        f.Note.DispatchedQuantity = 6000; f.Allocation.DispatchedAt = DateTime.UtcNow; await db.SaveChangesAsync();
        Assert.True((await Flow.Recall(new() { BatchManufacturingRecordId = f.Note.BatchManufacturingRecordId, Code = "RC-FULL", Reason = "Confirmed defect",
            RegulatoryClassification = "Class II (authority assigned)", AuthorityReference = "FDA reference R-1" })).IsSuccess);
        var recall = await db.FinishedGoodsRecalls.SingleAsync();
        Assert.True((await Flow.Contact(recall.Id, new() { AllocationId = f.Allocation.Id, Acknowledged = true, Evidence = "Customer acknowledged notice N-1" })).IsSuccess);
        Assert.True((await Flow.Recover(recall.Id, new() { AllocationId = f.Allocation.Id, FinishedGoodsTransferNoteId = f.Note.Id, Quantity = 6000, Evidence = "Return receipt RR-1", Condition = "Sealed and quarantined in Q1" })).IsSuccess);
        var recovery = await db.FinishedGoodsRecallRecoveries.SingleAsync();
        Assert.True((await Flow.DisposeRecovery(recall.Id, recovery.Id, new() { Evidence = "QA-authorised destruction certificate DC-1" })).IsSuccess);
        Assert.True((await Flow.DisposeOnSite(recall.Id, new() { TransferNoteId = f.Note.Id, Quantity = 3800, Evidence = "On-site destruction certificate DC-2" })).IsSuccess);
        Assert.True((await Flow.CloseRecall(recall.Id, new() { Evidence = "Final reconciliation and effectiveness report FR-1" })).IsSuccess);
        Assert.True(recall.Closed); Assert.Equal(0, f.Note.PhysicalQuantity); Assert.Equal(0, f.Note.AvailableQuantity); Assert.True(f.Note.IsOnHold);
        Assert.True((await Flow.ReopenRecall(recall.Id, new() { Evidence = "Late investigation evidence QA-2" })).IsSuccess);
        Assert.True((await Flow.CloseRecall(recall.Id, new() { Evidence = "Supplementary final report FR-2" })).IsSuccess);
        var history = (await Flow.RecallDetail(recall.Id)).Value.History;
        Assert.Equal(3, history.Count); Assert.Contains(history, row => row.Evidence.Contains("FR-1")); Assert.Contains(history, row => row.Evidence.Contains("FR-2"));
    }

    [Fact]
    public async Task An_expired_batch_cannot_load_even_with_all_task_permissions()
    {
        var f = await Seed(All); f.Note.BatchManufacturingRecord.ExpiryDate = DateTime.UtcNow.Date; await db.SaveChangesAsync();
        var result = await Flow.Load(f.Allocation.Id, Loading()); Assert.Equal("FinishedGoods.Expiry", result.Error.Code);
    }


    [Fact]
    public async Task Voiding_preserves_history_invalidates_waybill_and_refuses_payment_until_reissue()
    {
        var f = await Seed([..All, PermissionKeys.CanVoidFinishedGoodsInvoice]);
        Assert.True((await Flow.VerifyPo(f.Proforma.Id, Po(f.Proforma, f.Currency, f.Attachment))).IsSuccess);
        var first = await Orders.CreateInvoice(new() { Code = "INV-OLD", ProformaInvoiceId = f.Proforma.Id, CustomerId = f.Allocation.ProductionOrder.CustomerId });
        Assert.True(first.IsSuccess);
        Assert.True((await Flow.Waybill(f.Allocation.Id, new() { InvoiceId = first.Value, Code = "WB-OLD", VehicleRegistration = "GR-1", DriverName = "Driver", DriverPhone = "123", Transporter = "Carrier", DeliveryAddress = "Hospital", StorageConditions = "Below 30 C" })).IsSuccess);
        Assert.True((await Flow.VoidInvoice(first.Value, new() { Evidence = "Finance cancellation approval FC-1" })).IsSuccess);
        Assert.True((await db.Invoices.SingleAsync(i => i.Id == first.Value)).Voided);
        Assert.True((await db.ProductionOrderWaybills.SingleAsync()).Voided);
        var payment = await new PaymentRepository(db, mapper).RecordPayment(new() { PayableType = DOMAIN.Entities.Payments.PayableType.CustomerInvoice,
            PayableId = first.Value, CurrencyId = f.Currency, Amount = 1, Reference = "PAY-DENIED" }, actor.UserId!.Value);
        Assert.True(payment.IsFailure); Assert.Empty(db.Payments);
        var second = await Orders.CreateInvoice(new() { Code = "INV-NEW", ProformaInvoiceId = f.Proforma.Id, CustomerId = f.Allocation.ProductionOrder.CustomerId });
        Assert.True(second.IsSuccess);
        Assert.True((await Flow.Waybill(f.Allocation.Id, new() { InvoiceId = second.Value, Code = "WB-NEW", VehicleRegistration = "GR-1", DriverName = "Driver", DriverPhone = "123", Transporter = "Carrier", DeliveryAddress = "Hospital", StorageConditions = "Below 30 C" })).IsSuccess);
        var documents = (await Flow.Documents(f.Allocation.Id)).Value;
        Assert.Equal("INV-NEW", documents.InvoiceCode); Assert.Equal("WB-NEW", documents.WaybillCode);
        Assert.Equal(2, documents.InvoiceHistory.Count); Assert.Single(documents.InvoiceHistory.Where(i => i.Voided));
        Assert.Equal(9800, (await db.FinishedGoodsTransferNotes.SingleAsync()).PhysicalQuantity);
    }


    [Fact]
    public async Task Approved_stock_counts_post_a_delta_once_and_preserve_transfer_and_dispatch_evidence()
    {
        var f = await Seed(PermissionKeys.CanApplyFinishedGoodsStockAdjustment);
        var adjustment = new DOMAIN.Entities.StockAdjustments.StockAdjustment { Id = Guid.NewGuid(), AdjustmentNumber = "SC-1", Approved = true,
            TargetType = DOMAIN.Entities.StockAdjustments.StockAdjustmentTarget.Product, Lines = [new() { FinishedGoodsTransferNoteId = f.Note.Id,
                SystemQuantitySnapshot = 9800, PhysicalCount = 9700, Variance = -100, ReasonCode = "PhysicalCount", Notes = "Signed count SC-1" }] };
        db.Add(adjustment); await db.SaveChangesAsync();
        var repository = new StockAdjustmentRepository(db, FinishedGoodsYieldTests.NoApprovals.Create(), mapper);
        Assert.True((await repository.ApplyStockAdjustment(adjustment.Id, actor.UserId!.Value)).IsSuccess);
        Assert.Equal(10000, f.Note.TotalQuantity); Assert.Equal(9800, f.Note.QuantityReceived); Assert.Equal(-100, f.Note.AdjustmentQuantity);
        Assert.Equal(9700, f.Note.PhysicalQuantity); Assert.Equal(3700, f.Note.AvailableQuantity);
        Assert.True((await repository.ApplyStockAdjustment(adjustment.Id, actor.UserId.Value)).IsFailure);
        Assert.Single(db.InventoryLedgers); Assert.Single(db.ProductBinCardInformation);
    }

    [Fact]
    public async Task Stock_adjustments_cannot_consume_reserved_customer_stock()
    {
        var f = await Seed(PermissionKeys.CanApplyFinishedGoodsStockAdjustment);
        var adjustment = new DOMAIN.Entities.StockAdjustments.StockAdjustment { Id = Guid.NewGuid(), AdjustmentNumber = "SC-2", Approved = true,
            TargetType = DOMAIN.Entities.StockAdjustments.StockAdjustmentTarget.Product, Lines = [new() { FinishedGoodsTransferNoteId = f.Note.Id,
                SystemQuantitySnapshot = 9800, PhysicalCount = 5800, Variance = -4000, ReasonCode = "PhysicalCount" }] };
        db.Add(adjustment); await db.SaveChangesAsync();
        var result = await new StockAdjustmentRepository(db, FinishedGoodsYieldTests.NoApprovals.Create(), mapper).ApplyStockAdjustment(adjustment.Id, actor.UserId!.Value);
        Assert.Equal("StockAdjustment.Reserved", result.Error.Code); Assert.Empty(db.InventoryLedgers);
        Assert.Equal(9800, (await db.FinishedGoodsTransferNotes.SingleAsync()).PhysicalQuantity);
    }

    [Fact]
    public async Task Invoiced_product_reports_include_only_actual_live_invoices_with_the_issued_number_and_date()
    {
        var f = await Seed([..All, PermissionKeys.CanVoidFinishedGoodsInvoice]);
        var reports = new ReportRepository(db, mapper, null!, Microsoft.Extensions.Logging.Abstractions.NullLogger<ReportRepository>.Instance);
        Assert.Empty((await reports.GetInvoicedProductsSummary(new())).Value);
        Assert.True((await Flow.VerifyPo(f.Proforma.Id, Po(f.Proforma, f.Currency, f.Attachment))).IsSuccess);
        var issued = await Orders.CreateInvoice(new() { Code = "INV-REPORT", ProformaInvoiceId = f.Proforma.Id, CustomerId = f.Allocation.ProductionOrder.CustomerId });
        Assert.True(issued.IsSuccess);
        var row = Assert.Single((await reports.GetInvoicedProductsSummary(new())).Value);
        Assert.Equal("INV-REPORT", row.InvoiceNo); Assert.Equal(12, row.UnitPrice);
        Assert.True((await Flow.VoidInvoice(issued.Value, new() { Evidence = "Finance cancellation approval FC-2" })).IsSuccess);
        Assert.Empty((await reports.GetInvoicedProductsSummary(new())).Value);
    }


    [Fact]
    public async Task Authority_decisions_update_an_open_recall_with_retained_evidence_and_their_own_permission()
    {
        var f = await Seed(PermissionKeys.CanCreateProductRecall, PermissionKeys.CanUpdateProductRecallDecision);
        Assert.True((await Flow.Recall(new() { Code = "RC-AUTH", BatchManufacturingRecordId = f.Note.BatchManufacturingRecordId, Reason = "Defect",
            RegulatoryClassification = "Pending authority classification", AuthorityReference = "Notification pending" })).IsSuccess);
        var recall = await db.FinishedGoodsRecalls.SingleAsync();
        Assert.True((await Flow.UpdateRecallDecision(recall.Id, new() { RegulatoryClassification = "Class II (authority assigned)", AuthorityReference = "FDA ACK-1", Evidence = "Signed authority notification and QA decision QA-1" })).IsSuccess);
        var detail = (await Flow.RecallDetail(recall.Id)).Value;
        Assert.Equal("FDA ACK-1", detail.AuthorityReference); Assert.Contains(detail.History, row => row.Event == "FgRecallDecision" && row.Evidence.Contains("Notification pending"));
        Assert.True(f.Note.IsOnHold); Assert.Equal(0, f.Note.AvailableQuantity);
    }

    private static LoadFinishedGoodsRequest Loading() => new() { VehicleRegistration = "GR-1", DriverName = "Driver", DriverPhone = "123", LoadingLocation = "Bay 1", InspectionReference = "Inspection 1", ConditionsConfirmed = true };
}
