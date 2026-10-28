using DOMAIN.Entities.ItemTransactionLogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace INFRASTRUCTURE.EntityConfigurations.Inventory;

public sealed class ItemTransactionLogConfiguration : IEntityTypeConfiguration<ItemTransactionLog>
{
    public void Configure(EntityTypeBuilder<ItemTransactionLog> builder)
    {
        // The original migration stores names such as "Purchased" and "Issued" in text.
        builder.Property(row => row.TransactionType).HasConversion<string>().HasColumnType("text");
    }
}
