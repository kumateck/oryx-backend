using APP.Services.Materials;
using DOMAIN.Entities.Materials.StockMigrations;
using Xunit;

namespace APP.Tests.Utils;

public class OpeningBalanceDatesTests
{
    [Fact]
    public void Uses_earliest_manufacturing_and_latest_expiry_only_for_the_same_material_and_batch()
    {
        StockMigrationLine Row(string material, string batch, int made, int expiry) => new()
        {
            MaterialCode = material, BatchNumber = batch, QuantityInSheet = 10,
            ManufacturingDate = new DateTime(2023, made, 1), ExpiryDate = new DateTime(2028, expiry, 1),
        };
        var a = Row("RMS037", "041001D23", 5, 4);
        var b = Row("RMS037", "041001d23", 4, 5);
        var otherBatch = Row("RMS037", "FOLIDYN-OTHER-BATCH", 2, 12);
        var otherMaterial = Row("OTHER", "041001D23", 1, 10);
        OpeningBalanceDates.Reconcile([a, b, otherBatch, otherMaterial]);
        Assert.Equal(new DateTime(2023, 4, 1), a.ManufacturingDate);
        Assert.Equal(new DateTime(2028, 5, 1), a.ExpiryDate);
        Assert.Equal(a.ManufacturingDate, b.ManufacturingDate);
        Assert.Equal(a.ExpiryDate, b.ExpiryDate);
        Assert.Equal(new DateTime(2028, 12, 1), otherBatch.ExpiryDate);
        Assert.Equal(new DateTime(2023, 1, 1), otherMaterial.ManufacturingDate);
        Assert.Contains("earliest manufacturing", a.Corrections);
    }

    [Fact]
    public void Excluded_rows_do_not_extend_a_batch_and_expiry_keeps_the_whole_month()
    {
        var live = new StockMigrationLine { MaterialCode = "M", BatchNumber = "B", QuantityInSheet = 1, ExpiryDate = new DateTime(2024, 2, 1) };
        var excluded = new StockMigrationLine { MaterialCode = "M", BatchNumber = "B", QuantityInSheet = 1, ExpiryDate = new DateTime(2030, 1, 1), Excluded = true };
        OpeningBalanceDates.Reconcile([live, excluded]);
        Assert.Equal(new DateTime(2024, 2, 29, 0, 0, 0, DateTimeKind.Utc), OpeningBalanceDates.ExpiryMonthEnd(live.ExpiryDate));
    }
}
