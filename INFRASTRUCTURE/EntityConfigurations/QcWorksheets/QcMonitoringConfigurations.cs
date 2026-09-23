using DOMAIN.Entities.QcWorksheets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace INFRASTRUCTURE.EntityConfigurations.QcWorksheets;

/// <summary>
/// Persistence mapping for Milestone 6 — scheduled routine testing and water validity windows.
/// <para>
/// Four new tables, all prefixed <c>Qc</c>, plus the foreign key Milestone 3 deliberately left
/// unconstrained on <c>QcTestRequestSubjects.SamplingPointId</c>. Nothing in the live
/// Material/Product/Packaging QC schema is referenced or altered, and none of the shelved
/// routine tables (<c>RoutineDefinition</c>, <c>RoutineExecution</c>, <c>RoutineSample</c>,
/// <c>RoutineTrack</c>) are touched.
/// </para>
/// <para>
/// Two live tables are referenced read-only, by foreign key alone: <c>BatchManufacturingRecords</c>
/// and <c>ProductionActivitySteps</c>, both <c>Restrict</c>, so what a water use record points at
/// cannot vanish from beneath the Quality Impact Assessment flag.
/// </para>
/// </summary>
public class SamplingPointConfiguration : IEntityTypeConfiguration<SamplingPoint>
{
    public void Configure(EntityTypeBuilder<SamplingPoint> builder)
    {
        builder.ToTable("QcSamplingPoints");
        builder.HasQueryFilter(item => !item.DeletedAt.HasValue);

        builder.Property(item => item.Code).HasMaxLength(100).IsRequired();
        builder.Property(item => item.Name).HasMaxLength(500).IsRequired();
        builder.Property(item => item.Area).HasMaxLength(200);

        // Not a unique index: the context soft-deletes by rewriting a Deleted entry as Modified
        // with DeletedAt set, so a unique constraint would keep a retired point's code reserved
        // for ever. Uniqueness among live points is enforced in SamplingPointRepository, which
        // sees the query filter — the same reasoning QcWorksheetFieldValues records.
        builder.HasIndex(item => item.Code);
        builder.HasIndex(item => item.Type);

        builder
            .HasOne(item => item.SamplingPointGroup)
            .WithMany()
            .HasForeignKey(item => item.SamplingPointGroupId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class MonitoringProgramConfiguration : IEntityTypeConfiguration<MonitoringProgram>
{
    public void Configure(EntityTypeBuilder<MonitoringProgram> builder)
    {
        builder.ToTable("QcMonitoringPrograms");
        builder.HasQueryFilter(item => !item.DeletedAt.HasValue);

        // The daily scan's whole query: Active programs inside their lead time. Indexed as the
        // pair it is actually filtered by.
        builder.HasIndex(item => new { item.Status, item.NextDueDate });

        builder.HasIndex(item => item.SamplingPointId);

        // The scan groups by (point type, specification); activation resolves a point's next due
        // date. Both read through this pair.
        builder.HasIndex(item => new { item.SamplingPointId, item.SpecificationId });

        // "Which schedules are pinned to this specification version" — asked whenever a
        // specification is revised, exactly as it is of TestRequest.
        builder.HasIndex(item => new { item.SpecificationId, item.SpecificationVersion });

        // Restrict: the pinned specification version must stay readable for as long as a schedule
        // points at it, since nothing here resolves forward to a successor.
        builder
            .HasOne(item => item.Specification)
            .WithMany()
            .HasForeignKey(item => item.SpecificationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(item => item.SamplingPoint)
            .WithMany()
            .HasForeignKey(item => item.SamplingPointId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class WaterQualityPeriodConfiguration : IEntityTypeConfiguration<WaterQualityPeriod>
{
    public void Configure(EntityTypeBuilder<WaterQualityPeriod> builder)
    {
        builder.ToTable("QcWaterQualityPeriods");
        builder.HasQueryFilter(item => !item.DeletedAt.HasValue);

        builder.Property(item => item.RetrospectiveReason).HasMaxLength(1000);
        builder.Property(item => item.HoldReason).HasMaxLength(1000);

        // "Which window covers this point right now", and the list view's own filters.
        builder.HasIndex(item => new { item.SamplingPointId, item.Status });
        builder.HasIndex(item => new { item.Status, item.ValidUntil });

        // The scaffolding guard: one window per backing Subject, checked before creating another.
        builder.HasIndex(item => item.TestRequestSubjectId);

        builder
            .HasOne(item => item.SamplingPoint)
            .WithMany()
            .HasForeignKey(item => item.SamplingPointId)
            .OnDelete(DeleteBehavior.Restrict);

        // Restrict: the Subject whose reviewed results back this window must stay readable, since
        // that link is the evidence the window rests on.
        builder
            .HasOne(item => item.TestRequestSubject)
            .WithMany()
            .HasForeignKey(item => item.TestRequestSubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(item => item.ActivatedBy)
            .WithMany()
            .HasForeignKey(item => item.ActivatedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(item => item.HeldBy)
            .WithMany()
            .HasForeignKey(item => item.HeldById)
            .OnDelete(DeleteBehavior.Restrict);

        // Cascade, unlike the references above: a use record has no meaning apart from the window
        // it was booked against.
        builder
            .HasMany(item => item.UseRecords)
            .WithOne(item => item.WaterQualityPeriod)
            .HasForeignKey(item => item.WaterQualityPeriodId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class WaterUseRecordConfiguration : IEntityTypeConfiguration<WaterUseRecord>
{
    public void Configure(EntityTypeBuilder<WaterUseRecord> builder)
    {
        builder.ToTable("QcWaterUseRecords");
        builder.HasQueryFilter(item => !item.DeletedAt.HasValue);

        // The hold cascade's own query — every Recorded use under one window.
        builder.HasIndex(item => new { item.WaterQualityPeriodId, item.Status });
        builder.HasIndex(item => item.UsedAt);

        // Live tables, referenced read-only.
        builder
            .HasOne(item => item.BatchManufacturingRecord)
            .WithMany()
            .HasForeignKey(item => item.BatchManufacturingRecordId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(item => item.ProductionActivityStep)
            .WithMany()
            .HasForeignKey(item => item.ProductionActivityStepId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(item => item.RecordedBy)
            .WithMany()
            .HasForeignKey(item => item.RecordedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
