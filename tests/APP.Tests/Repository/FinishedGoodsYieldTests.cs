using System.Reflection;
using APP.IRepository;
using APP.Repository;
using APP.Utils;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Customers;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.ProductionOrders;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

/// <summary>
/// Finished goods are stored as yield (base units produced); orders, packing lists and invoices count
/// packs. Before this, a packing list subtracted packs straight from a transfer note's yield, so taking
/// 50 boxes of tablets removed 50 tablets from stock. Packing styles also carry fill rows ("Tube = 25"
/// meaning 25 g) and no reliable row order, which a naive reading turns into a wrong pack size.
/// </summary>
public class FinishedGoodsYieldTests
{
    // ---------------------------------------------------------------- units per pack

    [Fact]
    public void Units_per_pack_multiplies_the_rows_below_the_pack()
    {
        // 80 boxes per shipper; a box holds 20 strips of 10 tablets.
        Assert.Equal(200m, PackConversion.UnitsPerPack(Style(80, "Tablet", ("Shipper", "s", 80, 1), ("Box", "box", 20, 2), ("Strip", "strip", 10, 3)).Packing));
    }

    [Fact]
    public void A_fill_row_on_the_base_unit_is_not_a_count()
    {
        // "18x20x25g": 18 cartons of 20 tubes; "Tube = 25" is the 25 g in a tube.
        var style = Style(360, "Tube", ("Shipper", "s", 18, 0), ("Outer Carton", "Out ctn", 20, 0), ("Tube", "Tu", 25, 0));
        Assert.Equal(1m, PackConversion.UnitsPerPack(style.Packing));
    }

    [Fact]
    public void Rows_are_read_shipper_first_whatever_their_stored_order()
    {
        // As stored: "Bottle = 100" (the 100 ml in a bottle) before the shipper row, every Order 0.
        var style = Style(60, "Bottle", ("Bottle", "bottle", 100, 0), ("Shipper", "s", 60, 0));
        Assert.Equal(1m, PackConversion.UnitsPerPack(style.Packing));
    }

    [Fact]
    public void Mass_and_volume_rows_describe_contents_not_counts()
    {
        var style = Style(30, "Bottle", ("Milligram", "mg", 200, 0));
        Assert.Equal(1m, PackConversion.UnitsPerPack(style.Packing));
    }

    [Fact]
    public void A_style_whose_rows_never_reach_packs_per_shipper_cannot_convert()
    {
        // 36 x 25 = 900 cartons, but the style says 650 per shipper.
        var style = Style(650, "Tablet", ("Shipper", "s", 36, 0), ("Outer Carton", "Out ctn", 25, 0), ("Blister", "BL", 10, 0));
        Assert.Null(PackConversion.UnitsPerPack(style.Packing));
        Assert.Contains("650 packs per shipper", PackConversion.CannotConvert(style.Packing));
    }

    [Fact]
    public void The_pack_can_sit_below_the_carton()
    {
        // 1,000 blisters per shipper: the blister of 10 tablets is the pack.
        var style = Style(1000, "Tablet", ("Shipper", "s", 100, 0), ("Outer Carton", "Out ctn", 10, 0), ("Blister", "BL", 10, 0));
        Assert.Equal(10m, PackConversion.UnitsPerPack(style.Packing));
        Assert.True(PackConversion.IsWholePacks(1_000_000m, 10m));
        Assert.False(PackConversion.IsWholePacks(1_000_005m, 10m));
    }

    // ---------------------------------------------------------------- packing lists

    [Fact]
    public async Task A_packing_list_takes_packs_from_the_order_and_their_yield_from_stock()
    {
        await using var context = CreateContext();
        var seed = await SeedAsync(context, orderedPacks: 1000, noteYield: 1_600_000m);

        var result = await CreateOrders(context).CreateProductOrderAllocation(Request(seed, (seed.NoteId, 50m)));

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Description : null);
        context.ChangeTracker.Clear();
        var note = await context.FinishedGoodsTransferNotes.SingleAsync(n => n.Id == seed.NoteId);
        Assert.Equal(10_000m, note.AllocatedQuantity);
        Assert.Equal(1_590_000m, note.RemainingQuantity);

        var order = await context.ProductionOrders.Include(o => o.Products).ThenInclude(p => p.FulfilledQuantities).SingleAsync();
        var taken = Assert.Single(order.Products[0].FulfilledQuantities);
        Assert.Equal((50m, 10_000m, 200m), (taken.Quantity, taken.YieldQuantity, taken.UnitsPerPack));
        Assert.Equal(950m, order.Products[0].RemainingQuantity);

        var list = await context.AllocateProductionOrders.Include(a => a.Products).ThenInclude(p => p.FulfilledQuantities).SingleAsync();
        var line = Assert.Single(Assert.Single(list.Products).FulfilledQuantities);
        Assert.Equal((50m, 10_000m, 200m), (line.Quantity, line.YieldQuantity, line.UnitsPerPack));
    }

    [Fact]
    public async Task A_packing_list_cannot_take_more_packs_than_the_batch_holds()
    {
        await using var context = CreateContext();
        // 1,000 packs in stock; the order wants 2,000.
        var seed = await SeedAsync(context, orderedPacks: 2000, noteYield: 200_000m);

        var result = await CreateOrders(context).CreateProductOrderAllocation(Request(seed, (seed.NoteId, 1001m)));

        Assert.True(result.IsFailure);
        Assert.Equal("FinishedGoodsTransferNote.OverAllocate", result.Error.Code);
        Assert.Contains("has 1000 packs left", result.Error.Description);
        await AssertNothingSaved(context, seed);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(2.5)]
    public async Task Packs_must_be_whole_and_above_zero(double packs)
    {
        await using var context = CreateContext();
        var seed = await SeedAsync(context, orderedPacks: 1000, noteYield: 1_600_000m);

        var result = await CreateOrders(context).CreateProductOrderAllocation(Request(seed, (seed.NoteId, (decimal)packs)));

        Assert.True(result.IsFailure);
        Assert.Equal("FinishedGoodsTransferNote.InvalidQuantity", result.Error.Code);
        await AssertNothingSaved(context, seed);
    }

    [Fact]
    public async Task An_order_line_cannot_get_more_packs_than_it_still_needs()
    {
        await using var context = CreateContext();
        var seed = await SeedAsync(context, orderedPacks: 40, noteYield: 1_600_000m);

        var result = await CreateOrders(context).CreateProductOrderAllocation(Request(seed, (seed.NoteId, 41m)));

        Assert.True(result.IsFailure);
        Assert.Equal("ProductionOrder.ProductOverAllocate", result.Error.Code);
        await AssertNothingSaved(context, seed);
    }

    [Fact]
    public async Task Stock_from_another_packing_style_cannot_fill_the_order()
    {
        await using var context = CreateContext();
        var seed = await SeedAsync(context, orderedPacks: 1000, noteYield: 1_600_000m);
        var other = Style(1000, "Tablet", ("Shipper", "s", 100, 0), ("Outer Carton", "Out ctn", 10, 0), ("Blister", "BL", 10, 0));
        other.Packing.ProductId = seed.ProductId;
        context.AddRange(other.Units);
        context.Add(other.Packing);
        var note = await context.FinishedGoodsTransferNotes.SingleAsync();
        note.ProductPackingId = other.Packing.Id;
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var result = await CreateOrders(context).CreateProductOrderAllocation(Request(seed, (seed.NoteId, 10m)));

        Assert.True(result.IsFailure);
        Assert.Equal("FinishedGoodsTransferNote.WrongPacking", result.Error.Code);
    }

    [Fact]
    public async Task Stock_whose_packing_style_cannot_convert_is_refused()
    {
        await using var context = CreateContext();
        var seed = await SeedAsync(context, orderedPacks: 1000, noteYield: 1_600_000m, unitsPerPackOnNote: 0m,
            style: Style(650, "Tablet", ("Shipper", "s", 36, 0), ("Outer Carton", "Out ctn", 25, 0), ("Blister", "BL", 10, 0)));

        var result = await CreateOrders(context).CreateProductOrderAllocation(Request(seed, (seed.NoteId, 10m)));

        Assert.True(result.IsFailure);
        Assert.Equal("FinishedGoodsTransferNote.Packing", result.Error.Code);
        await AssertNothingSaved(context, seed);
    }

    [Fact]
    public async Task Stock_not_yet_received_by_the_store_cannot_be_allocated()
    {
        await using var context = CreateContext();
        var seed = await SeedAsync(context, orderedPacks: 1000, noteYield: 1_600_000m, received: false);

        var result = await CreateOrders(context).CreateProductOrderAllocation(Request(seed, (seed.NoteId, 10m)));

        Assert.True(result.IsFailure);
        Assert.Equal("FinishedGoodsTransferNote.NotReceived", result.Error.Code);
    }

    [Fact]
    public async Task Stock_is_what_the_store_received_not_what_was_sent()
    {
        await using var context = CreateContext();
        // Sent 1,000 packs (200,000 tablets); the store counted in 900.
        var seed = await SeedAsync(context, orderedPacks: 2000, noteYield: 200_000m, receivedYield: 180_000m);

        var result = await CreateOrders(context).CreateProductOrderAllocation(Request(seed, (seed.NoteId, 901m)));

        Assert.True(result.IsFailure);
        Assert.Contains("has 900 packs left", result.Error.Description);
    }

    // ---------------------------------------------------------------- editing a packing list

    [Fact]
    public async Task Editing_a_packing_list_gives_back_its_old_stock_and_takes_the_new()
    {
        await using var context = CreateContext();
        var seed = await SeedAsync(context, orderedPacks: 1000, noteYield: 1_600_000m);
        var created = await CreateOrders(context).CreateProductOrderAllocation(Request(seed, (seed.NoteId, 50m)));
        Assert.True(created.IsSuccess, created.IsFailure ? created.Error.Description : null);
        context.ChangeTracker.Clear();

        var result = await CreateOrders(context).UpdateProductOrderAllocation(created.Value, Request(seed, (seed.NoteId, 30m)));

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Description : null);
        context.ChangeTracker.Clear();
        Assert.Equal(6_000m, (await context.FinishedGoodsTransferNotes.SingleAsync()).AllocatedQuantity);
        var order = await context.ProductionOrders.Include(o => o.Products).ThenInclude(p => p.FulfilledQuantities).SingleAsync();
        var taken = Assert.Single(order.Products[0].FulfilledQuantities);
        Assert.Equal((30m, 6_000m), (taken.Quantity, taken.YieldQuantity));
        var list = await context.AllocateProductionOrders.Include(a => a.Products).ThenInclude(p => p.FulfilledQuantities).SingleAsync();
        var line = Assert.Single(Assert.Single(list.Products).FulfilledQuantities);
        Assert.Equal((30m, 6_000m), (line.Quantity, line.YieldQuantity));
    }

    [Fact]
    public async Task An_edit_can_use_the_stock_its_own_old_lines_held()
    {
        await using var context = CreateContext();
        // 1,000 packs in stock, all on the packing list, and the order wants exactly that.
        var seed = await SeedAsync(context, orderedPacks: 1000, noteYield: 200_000m);
        var created = await CreateOrders(context).CreateProductOrderAllocation(Request(seed, (seed.NoteId, 1000m)));
        Assert.True(created.IsSuccess, created.IsFailure ? created.Error.Description : null);
        context.ChangeTracker.Clear();

        var same = await CreateOrders(context).UpdateProductOrderAllocation(created.Value, Request(seed, (seed.NoteId, 1000m)));
        Assert.True(same.IsSuccess, same.IsFailure ? same.Error.Description : null);
        context.ChangeTracker.Clear();

        var fewer = await CreateOrders(context).UpdateProductOrderAllocation(created.Value, Request(seed, (seed.NoteId, 400m)));
        Assert.True(fewer.IsSuccess, fewer.IsFailure ? fewer.Error.Description : null);
        context.ChangeTracker.Clear();
        var order = await context.ProductionOrders.Include(o => o.Products).ThenInclude(p => p.FulfilledQuantities).SingleAsync();
        Assert.False(order.Products[0].Fulfilled);
        Assert.Equal(600m, order.Products[0].RemainingQuantity);
        Assert.Equal(ProductionOrderStatus.PartialPackingReady, order.Status);
        Assert.Equal(80_000m, (await context.FinishedGoodsTransferNotes.SingleAsync()).AllocatedQuantity);
    }

    [Fact]
    public async Task A_refused_edit_changes_nothing()
    {
        await using var context = CreateContext();
        var seed = await SeedAsync(context, orderedPacks: 2000, noteYield: 200_000m);
        var created = await CreateOrders(context).CreateProductOrderAllocation(Request(seed, (seed.NoteId, 50m)));
        Assert.True(created.IsSuccess, created.IsFailure ? created.Error.Description : null);
        context.ChangeTracker.Clear();

        var result = await CreateOrders(context).UpdateProductOrderAllocation(created.Value, Request(seed, (seed.NoteId, 1001m)));

        Assert.True(result.IsFailure);
        Assert.Equal("FinishedGoodsTransferNote.OverAllocate", result.Error.Code);
        context.ChangeTracker.Clear();
        Assert.Equal(10_000m, (await context.FinishedGoodsTransferNotes.SingleAsync()).AllocatedQuantity);
        var order = await context.ProductionOrders.Include(o => o.Products).ThenInclude(p => p.FulfilledQuantities).SingleAsync();
        Assert.Equal(50m, Assert.Single(order.Products[0].FulfilledQuantities).Quantity);
        var list = await context.AllocateProductionOrders.Include(a => a.Products).ThenInclude(p => p.FulfilledQuantities).SingleAsync();
        Assert.Equal(50m, Assert.Single(Assert.Single(list.Products).FulfilledQuantities).Quantity);
    }

    [Fact]
    public async Task An_approved_packing_list_cannot_be_edited()
    {
        await using var context = CreateContext();
        var seed = await SeedAsync(context, orderedPacks: 1000, noteYield: 1_600_000m);
        var created = await CreateOrders(context).CreateProductOrderAllocation(Request(seed, (seed.NoteId, 50m)));
        Assert.True(created.IsSuccess, created.IsFailure ? created.Error.Description : null);
        var list = await context.AllocateProductionOrders.SingleAsync();
        list.Approved = true;
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var result = await CreateOrders(context).UpdateProductOrderAllocation(created.Value, Request(seed, (seed.NoteId, 30m)));

        Assert.True(result.IsFailure);
        Assert.Equal("AllocateProductionOrder.Locked", result.Error.Code);
        context.ChangeTracker.Clear();
        Assert.Equal(10_000m, (await context.FinishedGoodsTransferNotes.SingleAsync()).AllocatedQuantity);
    }

    // ---------------------------------------------------------------- receiving a transfer

    [Fact]
    public async Task The_store_receives_whole_packs_or_everything_sent()
    {
        await using var context = CreateContext();
        // 2 shippers, 3 boxes and 7 tablets short of a box.
        var seed = await SeedAsync(context, orderedPacks: 1000, noteYield: 32_607m, received: false);
        var schedules = CreateSchedules(context);

        var part = await schedules.ApproveTransferNote(seed.NoteId, new ApproveTransferNoteRequest { QuantityReceived = 32_601m });
        Assert.True(part.IsFailure);
        Assert.Equal("TransferNote.Received", part.Error.Code);

        var tooMuch = await schedules.ApproveTransferNote(seed.NoteId, new ApproveTransferNoteRequest { QuantityReceived = 32_800m });
        Assert.True(tooMuch.IsFailure);

        var everything = await schedules.ApproveTransferNote(seed.NoteId, new ApproveTransferNoteRequest { QuantityReceived = 32_607m });
        Assert.True(everything.IsSuccess, everything.IsFailure ? everything.Error.Description : null);

        var again = await schedules.ApproveTransferNote(seed.NoteId, new ApproveTransferNoteRequest { QuantityReceived = 32_607m });
        Assert.True(again.IsFailure);
        Assert.Equal("TransferNote.Approved", again.Error.Code);
    }

    [Fact]
    public async Task The_store_can_receive_fewer_whole_packs_than_were_sent()
    {
        await using var context = CreateContext();
        var seed = await SeedAsync(context, orderedPacks: 1000, noteYield: 32_607m, received: false);

        var result = await CreateSchedules(context).ApproveTransferNote(seed.NoteId, new ApproveTransferNoteRequest { QuantityReceived = 32_000m });

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Description : null);
        context.ChangeTracker.Clear();
        var note = await context.FinishedGoodsTransferNotes.SingleAsync();
        Assert.Equal(32_000m, note.StockQuantity);
    }

    // ---------------------------------------------------------------- helpers

    private sealed record StyleSeed(ProductPacking Packing, List<UnitOfMeasure> Units);

    private sealed record Seeded(Guid ProductId, Guid PackingId, Guid NoteId, Guid OrderId);

    private static StyleSeed Style(int packsPerShipper, string baseUnit, params (string Name, string Symbol, decimal Quantity, int Order)[] rows)
    {
        var units = new Dictionary<string, UnitOfMeasure>(StringComparer.OrdinalIgnoreCase);
        UnitOfMeasure Unit(string name, string symbol) =>
            units.TryGetValue(name, out var unit) ? unit : units[name] = new UnitOfMeasure { Id = Guid.NewGuid(), Name = name, Symbol = symbol };

        var basis = Unit(baseUnit, baseUnit.ToLowerInvariant());
        var packing = new ProductPacking
        {
            Id = Guid.NewGuid(), Name = $"{packsPerShipper} per shipper", PackPerShipper = packsPerShipper,
            BasePackingUomId = basis.Id, BasePackingUoM = basis,
            PackingLists = rows.Select(row => { var unit = Unit(row.Name, row.Symbol); return new ProductPackingList { UomId = unit.Id, Uom = unit, Quantity = row.Quantity, Order = row.Order }; }).ToList(),
        };
        return new StyleSeed(packing, units.Values.ToList());
    }

    private static async Task<Seeded> SeedAsync(ApplicationDbContext context, int orderedPacks, decimal noteYield, decimal unitsPerPackOnNote = 200m,
        StyleSeed style = null, bool received = true, decimal? receivedYield = null)
    {
        style ??= Style(80, "Tablet", ("Shipper", "s", 80, 0), ("Box", "box", 20, 0), ("Strip", "strip", 10, 0));
        var product = new Product { Id = Guid.NewGuid(), Name = "Dicnac 100 Tablets", Code = "NT-081" };
        style.Packing.ProductId = product.Id;
        var schedule = new ProductionSchedule { Id = Guid.NewGuid() };
        var scheduled = new ProductionScheduleProduct { Id = Guid.NewGuid(), ProductionScheduleId = schedule.Id, ProductId = product.Id };
        var batch = new BatchManufacturingRecord { Id = Guid.NewGuid(), ProductionScheduleProductId = scheduled.Id, BatchNumber = "DC-1" };
        var note = new FinishedGoodsTransferNote
        {
            Id = Guid.NewGuid(), TransferNoteNumber = "FGTN-1", BatchManufacturingRecordId = batch.Id, ProductPackingId = style.Packing.Id,
            TotalQuantity = noteYield, QuantityPerPack = unitsPerPackOnNote, Approved = true,
            IsApproved = received, QuantityReceived = received ? receivedYield ?? noteYield : 0,
        };
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Hospital" };
        var order = new ProductionOrder
        {
            Id = Guid.NewGuid(), Code = "PO-1", CustomerId = customer.Id, Approved = true,
            Products = [new ProductionOrderProducts { ProductId = product.Id, ProductPackingId = style.Packing.Id, TotalOrderQuantity = orderedPacks }],
        };
        context.AddRange(style.Units);
        context.AddRange(product, style.Packing, schedule, scheduled, batch, note, customer, order);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return new Seeded(product.Id, style.Packing.Id, note.Id, order.Id);
    }

    private static AllocateProductionOrderRequest Request(Seeded seed, params (Guid NoteId, decimal Packs)[] lines) => new()
    {
        ProductionOrderId = seed.OrderId,
        Products =
        [
            new AllocateProductionOrderProductRequest
            {
                ProductId = seed.ProductId, ProductPackingId = seed.PackingId,
                FulfilledQuantities = lines.Select(line => new AllocateProductQuantityRequest { FinishedGoodsTransferNoteId = line.NoteId, Quantity = line.Packs }).ToList(),
            },
        ],
    };

    private static async Task AssertNothingSaved(ApplicationDbContext context, Seeded seed)
    {
        context.ChangeTracker.Clear();
        Assert.Empty(await context.AllocateProductionOrders.ToListAsync());
        Assert.Equal(0m, (await context.FinishedGoodsTransferNotes.SingleAsync(n => n.Id == seed.NoteId)).AllocatedQuantity);
        var order = await context.ProductionOrders.Include(o => o.Products).ThenInclude(p => p.FulfilledQuantities).SingleAsync();
        Assert.Empty(order.Products[0].FulfilledQuantities);
    }

    private static ProductionOrderRepository CreateOrders(ApplicationDbContext context) =>
        new(context, null!, NoApprovals.Create());

    private static ProductionScheduleRepository CreateSchedules(ApplicationDbContext context) =>
        new(context, null!, null!, null!, null!, NoApprovals.Create(), null!);

    private static ApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new NoUser());

    private sealed class NoUser : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }

    /// <summary>An approval repository that starts no workflows: these tests are about stock, not approvals.</summary>
    public class NoApprovals : DispatchProxy
    {
        public static IApprovalRepository Create() => Create<IApprovalRepository, NoApprovals>();

        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            var type = targetMethod!.ReturnType;
            if (type == typeof(Task)) return Task.CompletedTask;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
                return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(type.GetGenericArguments()[0])
                    .Invoke(null, [type.GetGenericArguments()[0].IsValueType ? Activator.CreateInstance(type.GetGenericArguments()[0]) : null]);
            return type.IsValueType && type != typeof(void) ? Activator.CreateInstance(type) : null;
        }
    }
}
