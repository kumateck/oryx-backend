using APP.Repository;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.Items.Requisitions;
using DOMAIN.Entities.Memos;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.VendorQuotations;
using DOMAIN.Entities.Vendors;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public class StoreProcurementIntegrityTests
{
    private static readonly Guid Actor = Guid.NewGuid();
    private static ApplicationDbContext Context() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new CurrentUser());
    private static InventoryProcurementRepository Repository(ApplicationDbContext c) => new(c, null!, null!, null!);

    private sealed record Seeded(Item paper, Item toner, UnitOfMeasure unit, Vendor first, Vendor second);

    private static async Task<Seeded> Seed(ApplicationDbContext c)
    {
        var unit = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Box", Symbol = "bx" };
        var paper = new Item { Id = Guid.NewGuid(), Name = "Paper", Code = "PPR", Store = Store.GeneralStore, IsActive = true, UnitOfMeasureId = unit.Id };
        var toner = new Item { Id = Guid.NewGuid(), Name = "Toner", Code = "TNR", Store = Store.GeneralStore, IsActive = true, UnitOfMeasureId = unit.Id };
        var first = new Vendor { Id = Guid.NewGuid(), Name = "Alpha Supplies" };
        var second = new Vendor { Id = Guid.NewGuid(), Name = "Beta Office" };
        c.AddRange(unit, paper, toner, first, second);
        await c.SaveChangesAsync();
        return new Seeded(paper, toner, unit, first, second);
    }

    private static CreateInventoryPurchaseRequisition Requisition(Seeded s) => new()
    {
        Code = "PR-1", Items = [new() { ItemId = s.paper.Id, UoMId = s.unit.Id, Quantity = 10 }, new() { ItemId = s.toner.Id, UoMId = s.unit.Id, Quantity = 2 }]
    };

    private static CreateSourceInventoryRequisition Source(Guid requisition, Seeded s, Item item, decimal quantity, params Vendor[] vendors) => new()
    {
        InventoryPurchaseRequisitionId = requisition,
        Items = [new() { ItemId = item.Id, UoMId = s.unit.Id, Quantity = quantity, Vendors = vendors.Select(v => new CreateSourceInventoryRequisitionItemVendor { VendorId = v.Id }).ToList() }]
    };

    [Fact]
    public async Task EachItemIsSourcedOnceAndTheRequisitionCompletesWhenAllAre()
    {
        await using var c = Context(); var s = await Seed(c); var repository = Repository(c);
        var id = (await repository.CreateInventoryPurchaseRequisition(Requisition(s), Actor)).Value;
        Assert.True((await repository.CreateSourceRequisition(Source(id, s, s.paper, 10, s.first, s.second), Actor)).IsSuccess);
        Assert.Equal(InventoryPurchaseRequisitionStatus.Pending, (await c.InventoryPurchaseRequisitions.SingleAsync()).Status);
        Assert.Equal("Sourcing.Sourced", (await repository.CreateSourceRequisition(Source(id, s, s.paper, 10, s.first), Actor)).Error.Code);
        Assert.Equal("Sourcing.Quantity", (await repository.CreateSourceRequisition(Source(id, s, s.toner, 5, s.first), Actor)).Error.Code);
        Assert.Equal("Requisition.Sourced", (await repository.UpdateInventoryPurchaseRequisition(id, Requisition(s), Actor)).Error.Code);

        var toner = await c.InventoryPurchaseRequisitionItems.SingleAsync(i => i.ItemId == s.toner.Id);
        Assert.True((await repository.CreateMarketRequisition([new() { InventoryPurchaseRequisitionItemId = toner.Id, ItemId = s.toner.Id, UoMId = s.unit.Id, Quantity = 2 }], Actor)).IsSuccess);
        Assert.Equal(InventoryPurchaseRequisitionStatus.Complete, (await c.InventoryPurchaseRequisitions.SingleAsync()).Status);
        Assert.Equal(2, await c.VendorQuotationItems.CountAsync());
    }

    [Fact]
    public async Task ARequisitionDrawsOnOneStore()
    {
        await using var c = Context(); var s = await Seed(c);
        s.toner.Store = Store.ItStore; await c.SaveChangesAsync();
        Assert.Equal("Requisition.Stores", (await Repository(c).CreateInventoryPurchaseRequisition(Requisition(s), Actor)).Error.Code);
    }

    private static async Task<(InventoryProcurementRepository repository, List<VendorQuotationItem> quotes)> Quoted(ApplicationDbContext c, Seeded s)
    {
        var repository = Repository(c);
        var id = (await repository.CreateInventoryPurchaseRequisition(Requisition(s), Actor)).Value;
        await repository.CreateSourceRequisition(Source(id, s, s.paper, 10, s.first, s.second), Actor);
        var quotes = await c.VendorQuotationItems.Include(q => q.VendorQuotation).ToListAsync();
        foreach (var quote in quotes)
            Assert.True((await repository.ReceiveQuotationFromVendor([new VendorQuotationResponseDto { Id = quote.Id, Price = quote.VendorQuotation.VendorId == s.first.Id ? 5 : 6, PriceUoM = "box" }], quote.VendorQuotationId, Actor)).IsSuccess);
        return (repository, quotes);
    }

    [Fact]
    public async Task AQuotedPriceIsUsedOnceAndCompetingQuotesAreSetAside()
    {
        await using var c = Context(); var s = await Seed(c); var (repository, quotes) = await Quoted(c, s);
        var chosen = quotes.Single(q => q.VendorQuotation.VendorId == s.first.Id);
        var memo = new CreateMemoItem { VendorQuotationItemId = chosen.Id, ItemId = s.paper.Id, UoMId = s.unit.Id, Quantity = 10 };
        Assert.True((await repository.ProcessTrustedVendorMemo([memo], Actor)).IsSuccess);
        Assert.Equal("Memo.Quote", (await repository.ProcessTrustedVendorMemo([memo], Actor)).Error.Code);
        c.ChangeTracker.Clear();
        Assert.Equal(VendorQuotationItemStatus.NotUsed, (await c.VendorQuotationItems.SingleAsync(q => q.Id != chosen.Id)).Status);
        var stored = await c.Memos.Include(m => m.Items).SingleAsync();
        Assert.Equal(Actor, stored.CreatedById); Assert.Single(stored.Items); Assert.Equal(5m, stored.Items[0].PricePerUnit);
        Assert.Equal("Vendor.Quotation.Used", (await repository.ReceiveQuotationFromVendor([new VendorQuotationResponseDto { Id = chosen.Id, Price = 1 }], chosen.VendorQuotationId, Actor)).Error.Code);
    }

    [Fact]
    public async Task QuotedPricesMustBePositiveAndComplete()
    {
        await using var c = Context(); var s = await Seed(c); var repository = Repository(c);
        var id = (await repository.CreateInventoryPurchaseRequisition(Requisition(s), Actor)).Value;
        await repository.CreateSourceRequisition(Source(id, s, s.paper, 10, s.first), Actor);
        var quote = await c.VendorQuotationItems.SingleAsync();
        Assert.Equal("Vendor.Quotation.Price", (await repository.ReceiveQuotationFromVendor([new VendorQuotationResponseDto { Id = quote.Id, Price = 0 }], quote.VendorQuotationId, Actor)).Error.Code);
        Assert.Equal("Vendor.Quotation.Item", (await repository.ReceiveQuotationFromVendor([new VendorQuotationResponseDto { Id = quote.Id, Price = 3 }, new VendorQuotationResponseDto { Id = Guid.NewGuid(), Price = 3 }], quote.VendorQuotationId, Actor)).Error.Code);
        Assert.False((await c.VendorQuotations.SingleAsync()).ReceivedQuotation);
    }

    [Fact]
    public async Task AnOpenMarketMemoHasOneLinePerChosenPriceAndClosesTheItem()
    {
        await using var c = Context(); var s = await Seed(c); var repository = Repository(c);
        var id = (await repository.CreateInventoryPurchaseRequisition(Requisition(s), Actor)).Value;
        var line = await c.InventoryPurchaseRequisitionItems.SingleAsync(i => i.ItemId == s.toner.Id);
        await repository.CreateMarketRequisition([new() { InventoryPurchaseRequisitionItemId = line.Id, ItemId = s.toner.Id, UoMId = s.unit.Id, Quantity = 2 }], Actor);
        var market = await c.MarketRequisitions.SingleAsync();
        var terms = Guid.NewGuid();
        c.MarketRequisitionVendors.AddRange(
            new MarketRequisitionVendor { Id = Guid.NewGuid(), MarketRequisitionId = market.Id, VendorName = "Kiosk A", PricePerUnit = 40, TermsOfPaymentId = terms },
            new MarketRequisitionVendor { Id = Guid.NewGuid(), MarketRequisitionId = market.Id, VendorName = "Kiosk B", PricePerUnit = 45, TermsOfPaymentId = terms });
        await c.SaveChangesAsync();
        var chosen = await c.MarketRequisitionVendors.SingleAsync(v => v.VendorName == "Kiosk A");
        var request = new CreateMemoItem { MarketRequisitionVendorId = chosen.Id, ItemId = s.toner.Id, UoMId = s.unit.Id, Quantity = 2 };
        Assert.True((await repository.ProcessOpenMarketMemo([request], Actor)).IsSuccess);
        var memo = await c.Memos.Include(m => m.Items).SingleAsync();
        // The earlier code added every open-market item twice.
        Assert.Single(memo.Items); Assert.Equal(40m, memo.Items[0].PricePerUnit);
        Assert.All(await c.MarketRequisitionVendors.ToListAsync(), v => Assert.True(v.Complete));
        Assert.Equal("Memo.Offer", (await repository.ProcessOpenMarketMemo([request], Actor)).Error.Code);
    }

    [Fact]
    public async Task PayingEachMemoItemReceivesItsQuantityUnderThePayer()
    {
        await using var c = Context(); var s = await Seed(c);
        var memo = new Memo { Id = Guid.NewGuid(), Code = "MEMO-000001", Items =
        [
            new MemoItem { Id = Guid.NewGuid(), ItemId = s.paper.Id, UoMId = s.unit.Id, Quantity = 4, PricePerUnit = 5 },
            new MemoItem { Id = Guid.NewGuid(), ItemId = s.paper.Id, UoMId = s.unit.Id, Quantity = 6, PricePerUnit = 5 }
        ] };
        c.Memos.Add(memo); await c.SaveChangesAsync();
        var repository = Repository(c);
        Assert.True((await repository.MarkMemoItemAsPaid(memo.Items[0].Id, Actor)).IsSuccess);
        Assert.False((await c.Memos.SingleAsync()).Paid);
        Assert.True((await repository.MarkMemoItemAsPaid(memo.Items[1].Id, Actor)).IsSuccess);
        Assert.False((await repository.MarkMemoItemAsPaid(memo.Items[1].Id, Actor)).IsSuccess);
        var entries = await c.StockEntries.ToListAsync();
        // Before the repair the second line of the same item was never received, and receipts had no creator to check against.
        Assert.Equal([4m, 6m], entries.Select(e => e.Quantity).Order());
        Assert.All(entries, e => Assert.Equal(Actor, e.CreatedById));
        Assert.True((await c.Memos.SingleAsync()).Paid);
    }

    private sealed class CurrentUser : ICurrentUserService
    {
        public Guid? UserId => Actor; public Guid? DepartmentId => null; public string DepartmentType => string.Empty;
    }
}
