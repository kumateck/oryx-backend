using DOMAIN.Entities.FinishedGoods;
using DOMAIN.Entities.ProductionOrders;
using Microsoft.EntityFrameworkCore;

namespace INFRASTRUCTURE.Context;

public partial class ApplicationDbContext
{
    public DbSet<FinishedGoodsRecall> FinishedGoodsRecalls { get; set; }
    public DbSet<FinishedGoodsRecallContact> FinishedGoodsRecallContacts { get; set; }
    public DbSet<FinishedGoodsRecallRecovery> FinishedGoodsRecallRecoveries { get; set; }
    public DbSet<FinishedGoodsRecallSettlement> FinishedGoodsRecallSettlements { get; set; }
    private static void ConfigureFinishedGoodsControls(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FinishedGoodsRecall>().HasIndex(r => r.Code).IsUnique();
        modelBuilder.Entity<FinishedGoodsRecall>().HasIndex(r => r.BatchManufacturingRecordId).IsUnique();
        modelBuilder.Entity<FinishedGoodsRecallContact>().HasIndex(r => new { r.FinishedGoodsRecallId, r.AllocationId }).IsUnique();
        modelBuilder.Entity<AllocateProductionOrder>().OwnsMany(a => a.DeliveryLines);
        modelBuilder.Entity<AllocateProductionOrder>().OwnsMany(a => a.CostingLines);
        modelBuilder.Entity<FinishedGoodsRecallSettlement>().HasIndex(s => new { s.FinishedGoodsRecallId, s.Reference }).IsUnique();
        modelBuilder.Entity<DOMAIN.Entities.Invoices.Invoice>().HasIndex(i => i.Code).IsUnique().HasFilter("\"Code\" IS NOT NULL");
        modelBuilder.Entity<DOMAIN.Entities.Invoices.Invoice>().Property(i => i.Code).HasMaxLength(100);
        modelBuilder.Entity<DOMAIN.Entities.Materials.Batch.FinishedGoodsTransferNote>().Property(n => n.PackingStyleSnapshot).HasMaxLength(1000);
    }
}
