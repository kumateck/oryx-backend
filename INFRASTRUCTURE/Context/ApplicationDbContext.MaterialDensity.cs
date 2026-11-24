using DOMAIN.Entities.Materials;
using Microsoft.EntityFrameworkCore;

namespace INFRASTRUCTURE.Context;

public partial class ApplicationDbContext
{
    public DbSet<MaterialDensityMeasurement> MaterialDensityMeasurements => Set<MaterialDensityMeasurement>();
    private static void ConfigureMaterialDensity(ModelBuilder builder)
    {
        var measurement = builder.Entity<MaterialDensityMeasurement>();
        measurement.Property(row => row.PreviousDepartmentDensitiesJson).HasColumnType("jsonb");
        measurement.HasOne<Material>().WithMany().HasForeignKey(row => row.MaterialId).OnDelete(DeleteBehavior.Restrict);
        measurement.HasOne<DOMAIN.Entities.Users.User>().WithMany().HasForeignKey(row => row.ActorId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Material>().ToTable(table => table.HasCheckConstraint("CK_Material_Density", "\"DensityGramsPerMilliliter\" IS NULL OR \"DensityGramsPerMilliliter\" > 0"));
    }
    private void ValidateMaterialDensityMeasurements()
    {
        if (ChangeTracker.Entries<MaterialDensityMeasurement>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Material density measurements are append-only.");
    }
}
