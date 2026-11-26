using APP.Repository;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Products.Production;
using Xunit;

namespace APP.Tests.Repository;

public sealed class ProductionMaterialRepairDensityTests
{
    // Foligrow Syrup L025K027: Glycerine reserved as 118,953.21 ml with no density recorded, while the department now
    // counts it in mg. The repair could not convert it and refused with "the before/after units cannot be reconciled".
    private static MaterialBatchReservedQuantity Reserved(decimal? recorded, decimal? material) => new()
    {
        Quantity = 118_953.2117367168913560666138m, DensityGramsPerMilliliter = recorded,
        MaterialBatch = new MaterialBatch { Material = new Material { Name = "Glycerine", DensityGramsPerMilliliter = material } }
    };

    [Fact]
    public void A_reservation_without_a_recorded_density_converts_with_the_configuration_then_the_material_density()
    {
        var mg = new ProductionMaterialRequirement { Unit = "mg", DensityGramsPerMilliliter = 1.261m };
        var unset = new ProductionMaterialRequirement { Unit = "mg", DensityGramsPerMilliliter = 0 };
        Assert.Equal(1.261m, ProductionMaterialRepairPlanning.ReservationDensity(Reserved(null, 1.3m), mg));
        Assert.Equal(1.261m, ProductionMaterialRepairPlanning.ReservationDensity(Reserved(null, 1.261m), unset));
        Assert.Equal(1.261m, ProductionMaterialRepairPlanning.ReservationDensity(Reserved(null, 1.261m), null));
        // A density recorded on the reservation is what it was allocated with, so it wins.
        Assert.Equal(1.25m, ProductionMaterialRepairPlanning.ReservationDensity(Reserved(1.25m, 1.261m), mg));
        Assert.Null(ProductionMaterialRepairPlanning.ReservationDensity(Reserved(null, null), unset));

        var reservation = Reserved(null, 1.261m);
        var milligrams = MaterialDensity.Convert(reservation.Quantity, "ml", "mg", ProductionMaterialRepairPlanning.ReservationDensity(reservation, unset));
        Assert.NotNull(milligrams);
        Assert.Equal(150_000_000m, decimal.Round(milligrams!.Value, 0));
    }
}
