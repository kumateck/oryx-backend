using DOMAIN.Entities.QcWorksheets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace INFRASTRUCTURE.EntityConfigurations.QcWorksheets;

/// <summary>
/// Persistence mapping for Milestone 5, certificates. Two new tables, <c>QcCoas</c> and
/// <c>QcCoaRows</c>; nothing existing is altered.
/// <para>
/// The live certificate tables — <c>CommercialCertificates</c>, <c>CommercialCoaItems</c>,
/// <c>RoutineCertificates</c> — are neither referenced nor touched. The two paths coexist.
/// </para>
/// </summary>
public class CoaConfiguration : IEntityTypeConfiguration<Coa>
{
    public void Configure(EntityTypeBuilder<Coa> builder)
    {
        builder.ToTable("QcCoas");
        builder.HasQueryFilter(item => !item.DeletedAt.HasValue);

        builder.Property(item => item.CertificateCode).HasMaxLength(100).IsRequired();
        builder.Property(item => item.SpecificationCode).HasMaxLength(100);
        builder.Property(item => item.ProductOrMaterialName).HasMaxLength(500);
        builder.Property(item => item.BatchNumber).HasMaxLength(200);
        builder.Property(item => item.AreaOrRoom).HasMaxLength(500);
        builder.Property(item => item.RevisionReason).HasMaxLength(1000);

        // Unique across every certificate, including revisions, which take their own derived code.
        // Two documents in circulation under one number is the failure this prevents.
        builder.HasIndex(item => item.CertificateCode).IsUnique();

        // The register is read by status and shape, newest first — the list screen's only query.
        builder.HasIndex(item => new { item.Status, item.CertificateShape });
        builder.HasIndex(item => item.IssuedAt);

        // "Does this round already hold a live certificate" is asked after every worksheet review
        // and every OOS closure, so it is asked far more often than anything else here.
        builder.HasIndex(item => new { item.TestRequestId, item.Status });

        // Restrict throughout: a certificate is a regulatory document, and nothing it names may be
        // deleted out from under it.
        builder
            .HasOne(item => item.TestRequest)
            .WithMany()
            .HasForeignKey(item => item.TestRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        // The pinned Specification version row. Held so the certificate's provenance stays
        // resolvable; never read through to render an issued document, which uses the snapshotted
        // code and version instead.
        builder
            .HasOne(item => item.Specification)
            .WithMany()
            .HasForeignKey(item => item.SpecificationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Self-referencing supersession. Restrict is the point of the whole design: a superseded
        // certificate stays retrievable forever and is never deleted, so a cascade here would
        // defeat the record.
        builder
            .HasOne(item => item.Supersedes)
            .WithMany()
            .HasForeignKey(item => item.SupersedesId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(item => item.IssuedBy)
            .WithMany()
            .HasForeignKey(item => item.IssuedById)
            .OnDelete(DeleteBehavior.Restrict);

        // Rows are part of the certificate, not records in their own right: they are created with
        // it, never separately, and have no meaning apart from it.
        builder
            .HasMany(item => item.Rows)
            .WithOne(row => row.Coa)
            .HasForeignKey(row => row.CoaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Persistence mapping for one printed certificate line. Every column here is a snapshot taken at
/// generation time — the foreign keys exist for traceability, not for rendering.
/// </summary>
public class CoaRowConfiguration : IEntityTypeConfiguration<CoaRow>
{
    public void Configure(EntityTypeBuilder<CoaRow> builder)
    {
        builder.ToTable("QcCoaRows");
        builder.HasQueryFilter(item => !item.DeletedAt.HasValue);

        builder.Property(item => item.DisplayLabel).HasMaxLength(255).IsRequired();
        builder.Property(item => item.GroupName).HasMaxLength(200);
        builder.Property(item => item.SubjectRef).HasMaxLength(200);
        builder.Property(item => item.SubjectLabel).HasMaxLength(500);
        builder.Property(item => item.AcceptanceCriteria).HasMaxLength(2000);
        builder.Property(item => item.ResultValue).HasMaxLength(2000);

        // The viewer reads one certificate's rows in print order, and nothing else reads this
        // table at all.
        builder.HasIndex(item => new { item.CoaId, item.TestRequestSubjectId, item.DisplayOrder });

        builder
            .HasOne(item => item.TestRequestSubject)
            .WithMany()
            .HasForeignKey(item => item.TestRequestSubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        // Traceability only. The row prints its own snapshotted label, criteria and result; this
        // link records which Characteristic they were taken from, and is never read to render.
        builder
            .HasOne(item => item.SpecificationCharacteristic)
            .WithMany()
            .HasForeignKey(item => item.SpecificationCharacteristicId)
            .OnDelete(DeleteBehavior.Restrict);

        // Which worksheet the result came from — the original, or the retest a QA disposition
        // accepted in its place.
        builder
            .HasOne(item => item.SourceWorksheetInstance)
            .WithMany()
            .HasForeignKey(item => item.SourceWorksheetInstanceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
