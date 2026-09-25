using APP.Repository;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.Shipments;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public class ShipmentClearancePaymentTests
{
    [Theory]
    [InlineData(false, false, "BillingSheet.ApprovalRequired")]
    [InlineData(true, false, "BillingSheet.PaymentRequired")]
    [InlineData(true, true, null)]
    public async Task ClearingRequiresApprovedSheetAndPaidCharges(
        bool approved, bool paid, string? expectedError)
    {
        await using var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            new TestCurrentUser());
        var invoiceId = Guid.NewGuid();
        var shipment = new ShipmentDocument
        {
            Id = Guid.NewGuid(), ShipmentInvoiceId = invoiceId,
            Status = ShipmentStatus.AtPort,
        };
        var sheet = new BillingSheet
        {
            Id = Guid.NewGuid(), InvoiceId = invoiceId, Approved = approved,
            Charges = [new BillingSheetCharge { Id = Guid.NewGuid(), Paid = paid }],
        };
        context.ShipmentDocuments.Add(shipment);
        context.BillingSheets.Add(sheet);
        await context.SaveChangesAsync();

        var repository = new ProcurementRepository(context, null!, null!, null!,
            null!, null!, null!);
        var result = await repository.UpdateShipmentStatus(
            shipment.Id, ShipmentStatus.Cleared, Guid.NewGuid());

        if (expectedError is null)
        {
            Assert.True(result.IsSuccess);
            Assert.Equal(BillingSheetStatus.Paid, sheet.Status);
            Assert.Equal(ShipmentStatus.Cleared, shipment.Status);
        }
        else
        {
            Assert.True(result.IsFailure);
            Assert.Equal(expectedError, result.Error.Code);
            Assert.Equal(ShipmentStatus.AtPort, shipment.Status);
        }
    }

    private sealed class TestCurrentUser : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }
}
