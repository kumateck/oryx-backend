using DOMAIN.Entities.Procurement.Suppliers;
using Xunit;

namespace APP.Tests.Repository;

public class SupplierComplianceTests
{
    private static readonly DateTime AsOf = new(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ExpiringCertifications_IncludesBoundaryAndExcludesFollowingDay()
    {
        await using var context = SupplierRelationshipTestContext.Create();
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "GMP Supplier" };
        context.AddRange(
            supplier,
            Certification(supplier, "GMP-30", AsOf.AddDays(30)),
            Certification(supplier, "GMP-31", AsOf.AddDays(31))
        );
        await context.SaveChangesAsync();
        var repository = SupplierRelationshipTestContext.CreateRepository(context);

        var result = await repository.GetExpiringCertifications(30, AsOf);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value);
        Assert.Equal("GMP-30", item.CertificateNumber);
    }

    [Fact]
    public async Task RequalificationDue_IncludesOverdueAndBoundary()
    {
        await using var context = SupplierRelationshipTestContext.Create();
        context.Suppliers.AddRange(
            ApprovedSupplier("Overdue", AsOf.AddDays(-1)),
            ApprovedSupplier("Boundary", AsOf.AddDays(30)),
            ApprovedSupplier("Later", AsOf.AddDays(31))
        );
        await context.SaveChangesAsync();
        var repository = SupplierRelationshipTestContext.CreateRepository(context);

        var result = await repository.GetRequalificationDue(30, AsOf);

        Assert.True(result.IsSuccess);
        Assert.Equal(["Overdue", "Boundary"], result.Value.Select(item => item.SupplierName));
    }

    private static SupplierCertification Certification(Supplier supplier, string number, DateTime expiry)
        => new()
        {
            Id = Guid.NewGuid(), SupplierId = supplier.Id, Supplier = supplier,
            CertificationType = SupplierCertificationType.Gmp,
            CertificateNumber = number, IssuingBody = "FDA", IssueDate = AsOf.AddYears(-1),
            ExpiryDate = expiry,
        };

    private static Supplier ApprovedSupplier(string name, DateTime dueDate) => new()
    {
        Id = Guid.NewGuid(), Name = name, Status = SupplierStatus.Approved,
        ApprovedAt = dueDate.AddYears(-1), RequalificationDueDate = dueDate,
    };
}
