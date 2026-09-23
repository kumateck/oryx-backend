using DOMAIN.Entities.QcWorksheets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace INFRASTRUCTURE.EntityConfigurations.QcWorksheets;

/// <summary>
/// Persistence mapping for Milestone 4, the formal OOS workflow. One new table,
/// <c>QcOosCases</c>; nothing existing is altered.
/// <para>
/// Three live tables are referenced by foreign key alone and never reshaped:
/// <c>MaterialBatches</c> and <c>BatchManufacturingRecords</c> (the quarantine links) — both
/// <c>Restrict</c>, so a batch cannot be deleted out from under the case holding it — and
/// nothing at all is added to either. The disposition's writes to their <c>Status</c> columns
/// are application-level updates to columns that already exist; this milestone introduces no
/// schema change to either table.
/// </para>
/// <para>
/// The live <c>OosInvestigations</c> table is neither referenced nor touched: the two coexist.
/// </para>
/// </summary>
public class OosCaseConfiguration : IEntityTypeConfiguration<OosCase>
{
    public void Configure(EntityTypeBuilder<OosCase> builder)
    {
        builder.ToTable("QcOosCases");
        builder.HasQueryFilter(item => !item.DeletedAt.HasValue);

        builder.Property(item => item.FieldKey).HasMaxLength(100).IsRequired();
        builder.Property(item => item.ObservedValue).HasMaxLength(2000);
        builder.Property(item => item.BreachedLimit).HasMaxLength(2000);

        builder.HasIndex(item => item.Status);
        builder.HasIndex(item => item.OpenedAt);

        // The OOS queue is read by status, newest first — that is the list screen's only query.
        builder.HasIndex(item => new { item.Status, item.OpenedAt });

        // "Does this worksheet have an open case against this field" is asked on every submit,
        // to keep a resubmission after a correction cycle from opening a duplicate case.
        builder.HasIndex(item => new { item.WorksheetInstanceId, item.FieldKey });

        // Restrict: the worksheet whose value triggered the case must stay readable for as long
        // as the case exists. The case is only meaningful beside the result that caused it.
        builder
            .HasOne(item => item.WorksheetInstance)
            .WithMany()
            .HasForeignKey(item => item.WorksheetInstanceId)
            .OnDelete(DeleteBehavior.Restrict);

        // The far side of WorksheetInstance.RetestOfInstanceId. Restrict for the same reason:
        // the original and its retest are both part of the record, and neither may vanish.
        builder
            .HasOne(item => item.RetestWorksheetInstance)
            .WithMany()
            .HasForeignKey(item => item.RetestWorksheetInstanceId)
            .OnDelete(DeleteBehavior.Restrict);

        // The limit that was breached has to remain reconstructable from the case alone, which
        // is the same reasoning behind hard version pinning everywhere else in this module.
        builder
            .HasOne(item => item.SpecificationCharacteristic)
            .WithMany()
            .HasForeignKey(item => item.SpecificationCharacteristicId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(item => item.InvestigatedBy)
            .WithMany()
            .HasForeignKey(item => item.InvestigatedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(item => item.RetestAuthorizedBy)
            .WithMany()
            .HasForeignKey(item => item.RetestAuthorizedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(item => item.DispositionBy)
            .WithMany()
            .HasForeignKey(item => item.DispositionById)
            .OnDelete(DeleteBehavior.Restrict);

        // Live tables, referenced by FK only. No column is added to either, and no navigation
        // collection is declared on their side — nothing about MaterialBatch or
        // BatchManufacturingRecord changes shape because of this milestone.
        builder
            .HasOne(item => item.QuarantinedMaterialBatch)
            .WithMany()
            .HasForeignKey(item => item.QuarantinedMaterialBatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(item => item.QuarantinedBatchManufacturingRecord)
            .WithMany()
            .HasForeignKey(item => item.QuarantinedBatchManufacturingRecordId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
