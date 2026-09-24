using DOMAIN.Entities.QcWorksheets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace INFRASTRUCTURE.EntityConfigurations.QcWorksheets;

/// <summary>
/// Persistence mapping for Milestone 3, the execution layer. Every table here is new and
/// prefixed <c>Qc</c>; nothing in the live Material/Product/Packaging QC schema — including
/// <c>AnalyticalTestRequest</c> and its worksheet-adjacent tables — is referenced or altered.
/// <para>
/// Two live tables are referenced read-only, by foreign key alone: <c>MaterialBatches</c> and
/// <c>BatchManufacturingRecords</c>, both <c>Restrict</c> so a batch under test cannot vanish
/// from beneath the round that tests it.
/// </para>
/// </summary>
public class TestRequestConfiguration : IEntityTypeConfiguration<TestRequest>
{
    public void Configure(EntityTypeBuilder<TestRequest> builder)
    {
        builder.ToTable("QcTestRequests");
        builder.HasQueryFilter(item => !item.DeletedAt.HasValue);

        builder.Property(item => item.ArNumber).HasMaxLength(100).IsRequired();
        builder.Property(item => item.IssueNumber).HasMaxLength(100);
        builder.Property(item => item.UnscheduledReason).HasMaxLength(1000);

        builder.HasIndex(item => item.ArNumber);
        builder.HasIndex(item => item.Status);
        builder.HasIndex(item => new { item.Type, item.Status });

        // "Which rounds are pinned to this specification version" is the question asked when a
        // specification is revised, so the pin is indexed as a pair.
        builder.HasIndex(item => new { item.SpecificationId, item.SpecificationVersion });

        // Restrict: the specification a round is pinned to must stay readable for as long as
        // the round exists, which is what makes the ARD self-reconstructing.
        builder
            .HasOne(item => item.Specification)
            .WithMany()
            .HasForeignKey(item => item.SpecificationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(item => item.IssuedBy)
            .WithMany()
            .HasForeignKey(item => item.IssuedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasMany(item => item.Subjects)
            .WithOne(item => item.TestRequest)
            .HasForeignKey(item => item.TestRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class TestRequestSubjectConfiguration : IEntityTypeConfiguration<TestRequestSubject>
{
    public void Configure(EntityTypeBuilder<TestRequestSubject> builder)
    {
        builder.ToTable("QcTestRequestSubjects");
        builder.HasQueryFilter(item => !item.DeletedAt.HasValue);

        builder.Property(item => item.SubjectRef).HasMaxLength(200).IsRequired();
        builder.Property(item => item.SubjectLabel).HasMaxLength(500);
        builder.Property(item => item.ArNumber).HasMaxLength(100);

        builder.HasIndex(item => item.TestRequestId);

        // A round routinely covers 60-90 rooms, so "which subject is this code" is a hot
        // lookup within a round rather than across the table.
        builder.HasIndex(item => new { item.TestRequestId, item.SubjectRef });

        // Deliberately not unique: the same sampling point is tested round after round.
        builder.HasIndex(item => item.SubjectRef);

        builder
            .HasOne(item => item.SamplingPointGroup)
            .WithMany()
            .HasForeignKey(item => item.SamplingPointGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        // Live tables, referenced read-only. Restrict so a batch cannot be deleted out from
        // under an in-flight round — Milestone 4's OOS quarantine acts on this batch.
        builder
            .HasOne(item => item.MaterialBatch)
            .WithMany()
            .HasForeignKey(item => item.MaterialBatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(item => item.BatchManufacturingRecord)
            .WithMany()
            .HasForeignKey(item => item.BatchManufacturingRecordId)
            .OnDelete(DeleteBehavior.Restrict);

        // SamplingPointId is intentionally left as a bare column with no relationship: the
        // SamplingPoint entity arrives in Milestone 6, which adds the constraint then.
        builder.HasIndex(item => item.SamplingPointId);

        builder
            .HasMany(item => item.WorksheetInstances)
            .WithOne(item => item.TestRequestSubject)
            .HasForeignKey(item => item.TestRequestSubjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class WorksheetInstanceConfiguration : IEntityTypeConfiguration<WorksheetInstance>
{
    public void Configure(EntityTypeBuilder<WorksheetInstance> builder)
    {
        builder.ToTable("QcWorksheetInstances");
        builder.HasQueryFilter(item => !item.DeletedAt.HasValue);

        builder.HasIndex(item => item.TestRequestSubjectId);
        builder.HasIndex(item => item.Status);

        // The Test Room's "My Work" query, and the reviewer queue, in one index each.
        builder.HasIndex(item => new { item.AssignedToId, item.Status });
        builder.HasIndex(item => new { item.AnalysisType, item.Status });

        // ReferencedResult resolution scans by (template, status) looking for a Reviewed
        // source instance of a given template — this is that lookup.
        builder.HasIndex(item => new { item.WorksheetTemplateId, item.Status });
        builder.HasIndex(item => new { item.WorksheetTemplateId, item.WorksheetTemplateVersion });

        // Restrict: the pinned template version must remain readable for the life of the
        // instance. Resolving it forward to a newer version is forbidden, so the pinned row is
        // the only thing that can render this worksheet.
        builder
            .HasOne(item => item.WorksheetTemplate)
            .WithMany()
            .HasForeignKey(item => item.WorksheetTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(item => item.AssignedTo)
            .WithMany()
            .HasForeignKey(item => item.AssignedToId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(item => item.AssignedBy)
            .WithMany()
            .HasForeignKey(item => item.AssignedById)
            .OnDelete(DeleteBehavior.Restrict);

        // Self-referencing, written only by Milestone 4's retest flow. Restrict because the
        // original result must stay visible beside its retest, never replaced by it.
        builder
            .HasOne(item => item.RetestOfInstance)
            .WithMany()
            .HasForeignKey(item => item.RetestOfInstanceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasMany(item => item.FieldValues)
            .WithOne(item => item.WorksheetInstance)
            .HasForeignKey(item => item.WorksheetInstanceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(item => item.Reassignments)
            .WithOne(item => item.WorksheetInstance)
            .HasForeignKey(item => item.WorksheetInstanceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(item => item.CorrectionReturns)
            .WithOne(item => item.WorksheetInstance)
            .HasForeignKey(item => item.WorksheetInstanceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class WorksheetFieldValueConfiguration : IEntityTypeConfiguration<WorksheetFieldValue>
{
    public void Configure(EntityTypeBuilder<WorksheetFieldValue> builder)
    {
        builder.ToTable("QcWorksheetFieldValues");
        builder.HasQueryFilter(item => !item.DeletedAt.HasValue);

        builder.Property(item => item.FieldKey).HasMaxLength(100).IsRequired();
        builder.Property(item => item.ColumnKey).HasMaxLength(100);

        // The merge query on every worksheet read.
        builder.HasIndex(item => new { item.WorksheetInstanceId, item.FieldKey });

        // ReferencedResult resolution matches a source instance by its entered batch number,
        // which is a (ColumnKey, Value) lookup across instances.
        builder.HasIndex(item => new { item.ColumnKey, item.Value });

        // Deliberately not a unique index on (instance, field, row, column): values are
        // soft-deleted, so a unique constraint would keep a replaced value's slot occupied
        // forever. One-value-per-cell is enforced in WorksheetInstanceRepository, which sees
        // the soft-delete query filter.
        builder
            .HasOne(item => item.EnteredBy)
            .WithMany()
            .HasForeignKey(item => item.EnteredById)
            .OnDelete(DeleteBehavior.Restrict);

        // Restrict: the instance a referenced value was resolved from must stay readable, since
        // that link is the traceability the ReferencedResult field exists to provide.
        builder
            .HasOne(item => item.ResolvedFromInstance)
            .WithMany()
            .HasForeignKey(item => item.ResolvedFromInstanceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class WorksheetInstanceReassignmentConfiguration
    : IEntityTypeConfiguration<WorksheetInstanceReassignment>
{
    public void Configure(EntityTypeBuilder<WorksheetInstanceReassignment> builder)
    {
        builder.ToTable("QcWorksheetInstanceReassignments");
        builder.HasQueryFilter(item => !item.DeletedAt.HasValue);

        builder.Property(item => item.Reason).HasMaxLength(1000).IsRequired();

        builder.HasIndex(item => new { item.WorksheetInstanceId, item.ReassignedAt });

        builder
            .HasOne(item => item.FromUser)
            .WithMany()
            .HasForeignKey(item => item.FromUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(item => item.ToUser)
            .WithMany()
            .HasForeignKey(item => item.ToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(item => item.ReassignedBy)
            .WithMany()
            .HasForeignKey(item => item.ReassignedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class WorksheetInstanceCorrectionReturnConfiguration
    : IEntityTypeConfiguration<WorksheetInstanceCorrectionReturn>
{
    public void Configure(EntityTypeBuilder<WorksheetInstanceCorrectionReturn> builder)
    {
        builder.ToTable("QcWorksheetInstanceCorrectionReturns");
        builder.HasQueryFilter(item => !item.DeletedAt.HasValue);

        builder.Property(item => item.Reason).HasMaxLength(1000).IsRequired();

        // The history of one worksheet's correction cycles, read in order.
        builder.HasIndex(item => new { item.WorksheetInstanceId, item.ReturnedAt });

        builder
            .HasOne(item => item.ReturnedBy)
            .WithMany()
            .HasForeignKey(item => item.ReturnedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
