using System.Text.Json;
using DOMAIN.Entities.Procurement.Distribution;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.Repository;

public class MaterialDistributionPageReadTests
{
    [Fact]
    public async Task Batched_read_preserves_proportions_fifo_duplicate_link_handling_and_both_clients_fields()
    {
        await using var fixture = new DistributionPageFixture();
        await fixture.Seed();
        var result = await fixture.Repository.GetMaterialDistribution(fixture.Shipment.Id);
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Description : null);
        var section = Assert.Single(result.Value.Sections);
        Assert.Equal(60m, section.TotalQuantity);
        Assert.Equal("Description", section.Material.Description);
        Assert.Equal("mg", section.UoM.Symbol);
        Assert.Equal(2, section.Items.Count);
        foreach (var share in section.Items)
        {
            Assert.Equal(share.QuantityRequested * 0.6m, share.QuantityAllocated);
            Assert.Equal(share.QuantityRequested * 0.4m, share.QuantityRemaining);
            Assert.Equal(share.QuantityAllocated, share.Distributions.Sum(source => source.Quantity));
            Assert.Equal("Department description", share.Department.Description);
            Assert.Equal("mg", share.RequisitionItem.UoM.Symbol);
            var dates = share.Distributions.Select(source => Array.FindIndex(fixture.Lines,
                line => line.Id == source.ShipmentInvoiceItem.Id)).ToArray();
            Assert.Equal(dates.Order(), dates);
            Assert.All(share.Distributions, source => {
                var line = source.ShipmentInvoiceItem;
                Assert.Equal("Maker", line.Manufacturer.Name);
                Assert.Equal("RM", line.Material.Code);
                Assert.Equal("PO", line.PurchaseOrder.Code);
                Assert.Equal(1.5m, line.Price);
                Assert.Equal("kg", line.PriceUoM);
                Assert.Equal("$", line.Currency.Symbol);
                Assert.Equal("$", line.PurchaseOrder.Currency.Symbol); // Next card's legacy path.
            });
        }
        var sources = section.Items.SelectMany(share => share.Distributions).ToArray();
        Assert.Equal(40m, sources.Where(source => source.ShipmentInvoiceItem.Id == fixture.Lines[0].Id).Sum(source => source.Quantity));
        Assert.Equal(20m, sources.Where(source => source.ShipmentInvoiceItem.Id == fixture.Lines[1].Id).Sum(source => source.Quantity));
        var json = JsonSerializer.Serialize(result.Value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        foreach (var unused in new[] { "createdBy", "warehouses", "totalStock", "reservedStock", "specification", "batches" })
            Assert.DoesNotContain($"\"{unused}\"", json);
        Assert.Empty(fixture.Context.ChangeTracker.Entries());
        Assert.All(await fixture.Context.ShipmentInvoiceItems.IgnoreAutoIncludes().ToListAsync(), line => Assert.False(line.Distributed));
        Assert.All(await fixture.Context.RequisitionItems.IgnoreAutoIncludes().ToListAsync(), request => Assert.Equal(0m, request.QuantityReceived));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task No_active_invoice_lines_returns_empty_sections(bool markDistributed)
    {
        await using var fixture = new DistributionPageFixture();
        await fixture.Seed();
        var lines = await fixture.Context.ShipmentInvoiceItems.IgnoreAutoIncludes().ToListAsync();
        if (markDistributed) lines.ForEach(line => line.Distributed = true);
        else
        {
            var supplierId = await fixture.Context.ShipmentInvoices.IgnoreAutoIncludes().Select(invoice => invoice.SupplierId).SingleAsync();
            var invoice = new DOMAIN.Entities.Shipments.ShipmentInvoice { Id = Guid.NewGuid(), SupplierId = supplierId };
            fixture.Context.Add(invoice);
            var shipment = await fixture.Context.ShipmentDocuments.IgnoreAutoIncludes().SingleAsync();
            shipment.ShipmentInvoiceId = invoice.Id;
        }
        await fixture.Context.SaveChangesAsync(); fixture.Context.ChangeTracker.Clear();
        var result = await fixture.Repository.GetMaterialDistribution(fixture.Shipment.Id);
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Sections);
        Assert.Empty(fixture.Context.ChangeTracker.Entries());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Fulfilled_or_unlinked_requests_are_not_allocated(bool fulfill)
    {
        await using var fixture = new DistributionPageFixture();
        await fixture.Seed();
        if (fulfill)
        {
            var requests = await fixture.Context.RequisitionItems.IgnoreAutoIncludes().ToListAsync();
            requests.ForEach(request => request.QuantityReceived = request.Quantity);
        }
        else
        {
            var order = await fixture.Context.PurchaseOrders.IgnoreAutoIncludes().SingleAsync();
            var supplierId = await fixture.Context.SourceRequisitions.IgnoreAutoIncludes().Select(source => source.SupplierId).SingleAsync();
            var source = new DOMAIN.Entities.Requisitions.SourceRequisition { Id = Guid.NewGuid(), SupplierId = supplierId };
            fixture.Context.Add(source);
            order.SourceRequisitionId = source.Id;
        }
        await fixture.Context.SaveChangesAsync(); fixture.Context.ChangeTracker.Clear();
        var result = await fixture.Repository.GetMaterialDistribution(fixture.Shipment.Id);
        Assert.True(result.IsFailure);
        Assert.Equal("Shipment.Document", result.Error.Code);
        Assert.Empty(fixture.Context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Multiple_materials_are_grouped_independently_and_completed_requests_are_excluded()
    {
        await using var fixture = new DistributionPageFixture();
        await fixture.Seed();
        var first = fixture.Lines[0];
        var material = new DOMAIN.Entities.Materials.Material { Id = Guid.NewGuid(), Code = "OTHER", Name = "Other" };
        fixture.Context.AddRange(material,
            new DOMAIN.Entities.Requisitions.RequisitionItem { Id = Guid.NewGuid(),
                RequisitionId = fixture.Requests[0].RequisitionId, MaterialId = material.Id,
                UoMId = first.UoMId, Quantity = 30m },
            new DOMAIN.Entities.Shipments.ShipmentInvoiceItem { Id = Guid.NewGuid(),
                ShipmentInvoiceId = first.ShipmentInvoiceId, MaterialId = material.Id,
                UoMId = first.UoMId, PurchaseOrderId = first.PurchaseOrderId,
                ManufacturerId = first.ManufacturerId, ReceivedQuantity = 15m, ExpectedQuantity = 15m });
        var completed = await fixture.Context.RequisitionItems.IgnoreAutoIncludes()
            .SingleAsync(item => item.Id == fixture.Requests[1].Id);
        completed.QuantityReceived = completed.Quantity;
        await fixture.Context.SaveChangesAsync(); fixture.Context.ChangeTracker.Clear();

        var result = await fixture.Repository.GetMaterialDistribution(fixture.Shipment.Id);
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Description : null);
        Assert.Equal(2, result.Value.Sections.Count);
        var second = Assert.Single(result.Value.Sections, section => section.Material.Id == material.Id);
        var share = Assert.Single(second.Items);
        Assert.Equal(30m, share.QuantityRequested);
        Assert.Equal(15m, share.QuantityAllocated);
        Assert.Equal(15m, share.QuantityRemaining);
        Assert.Single(Assert.Single(result.Value.Sections, section => section.Material.Id == first.MaterialId).Items);
        Assert.Empty(fixture.Context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Deleted_source_requisition_does_not_supply_eligible_requests()
    {
        await using var fixture = new DistributionPageFixture();
        await fixture.Seed();
        var source = await fixture.Context.SourceRequisitions.IgnoreAutoIncludes().SingleAsync();
        source.DeletedAt = DateTime.UtcNow;
        await fixture.Context.SaveChangesAsync(); fixture.Context.ChangeTracker.Clear();
        var result = await fixture.Repository.GetMaterialDistribution(fixture.Shipment.Id);
        Assert.True(result.IsFailure);
        Assert.Equal("Shipment.Document", result.Error.Code);
        Assert.Empty(fixture.Context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Missing_shipment_returns_not_found()
    {
        await using var fixture = new DistributionPageFixture();
        var result = await fixture.Repository.GetMaterialDistribution(Guid.NewGuid());
        Assert.True(result.IsFailure);
        Assert.Equal("ShipmentDoc.NotFound", result.Error.Code);
    }
}
