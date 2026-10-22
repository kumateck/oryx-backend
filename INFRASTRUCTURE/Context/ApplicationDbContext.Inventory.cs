using DOMAIN.Entities.Items;
using DOMAIN.Entities.StockEntries;
using Microsoft.EntityFrameworkCore;

namespace INFRASTRUCTURE.Context;

public partial class ApplicationDbContext
{
    private static void ConfigureInventoryConcurrency(ModelBuilder modelBuilder)
    {
        // Compare existing columns in UPDATE predicates; no new database column is required.
        modelBuilder.Entity<StockEntry>().Property(e => e.Status).IsConcurrencyToken();
        modelBuilder.Entity<Item>().Property(i => i.AvailableQuantity).IsConcurrencyToken();
        modelBuilder.Entity<Item>().Property(i => i.Code).IsConcurrencyToken();
        modelBuilder.Entity<Item>().Property(i => i.Store).IsConcurrencyToken();
        modelBuilder.Entity<Item>().Property(i => i.UnitOfMeasureId).IsConcurrencyToken();
        modelBuilder.Entity<Item>().Property(i => i.Classification).IsConcurrencyToken();
    }
}
