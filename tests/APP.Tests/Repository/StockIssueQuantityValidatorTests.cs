using APP.Repository;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Requisitions;
using Xunit;

namespace APP.Tests.Repository;

public class StockIssueQuantityValidatorTests
{
    [Theory]
    [InlineData(100, true)]
    [InlineData(90, false)]
    [InlineData(110, false)]
    public void IssueRequiresReservationToEqualApprovedLine(decimal reserved, bool valid)
    {
        var materialId = Guid.NewGuid();
        var requisition = new Requisition
        {
            Items = [new RequisitionItem { MaterialId = materialId, Quantity = 100 }],
        };
        var reservations = new List<MaterialBatchReservedQuantity>
        {
            new() { MaterialBatch = new MaterialBatch { MaterialId = materialId }, Quantity = reserved },
        };

        var result = StockIssueQuantityValidator.Validate(requisition, reservations);

        Assert.Equal(valid, result.IsSuccess);
        if (!valid)
            Assert.Equal("Stock.ReservationMismatch", result.Error.Code);
    }

    [Fact]
    public void IssueRejectsDuplicateMaterialLines()
    {
        var materialId = Guid.NewGuid();
        var requisition = new Requisition
        {
            Items =
            [
                new RequisitionItem { MaterialId = materialId, Quantity = 50 },
                new RequisitionItem { MaterialId = materialId, Quantity = 50 },
            ],
        };

        var result = StockIssueQuantityValidator.Validate(requisition, []);

        Assert.True(result.IsFailure);
        Assert.Equal("Stock.DuplicateMaterial", result.Error.Code);
    }
}
