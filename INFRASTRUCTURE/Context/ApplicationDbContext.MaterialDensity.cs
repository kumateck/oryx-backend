using DOMAIN.Entities.Materials;
using Microsoft.EntityFrameworkCore;

namespace INFRASTRUCTURE.Context;

public partial class ApplicationDbContext
{
    public DbSet<MaterialDensityMeasurement> MaterialDensityMeasurements => Set<MaterialDensityMeasurement>();
    public DbSet<MaterialCountingUnitChange> MaterialCountingUnitChanges => Set<MaterialCountingUnitChange>();
    private static void ConfigureMaterialDensity(ModelBuilder builder)
    {
        var measurement = builder.Entity<MaterialDensityMeasurement>();
        measurement.Property(row => row.PreviousDepartmentDensitiesJson).HasColumnType("jsonb");
        measurement.HasOne<Material>().WithMany().HasForeignKey(row => row.MaterialId).OnDelete(DeleteBehavior.Restrict);
        measurement.HasOne<DOMAIN.Entities.Users.User>().WithMany().HasForeignKey(row => row.ActorId).OnDelete(DeleteBehavior.Restrict);
        var unitChange = builder.Entity<MaterialCountingUnitChange>();
        unitChange.Property(row => row.PreviewJson).HasColumnType("jsonb");
        unitChange.Property(row => row.DensityGramsPerMilliliter).HasPrecision(18, 6);
        unitChange.HasIndex(row => new { row.MaterialId, row.DepartmentId, row.RecordedAt });
        unitChange.HasOne<Material>().WithMany().HasForeignKey(row => row.MaterialId).OnDelete(DeleteBehavior.Restrict);
        unitChange.HasOne<MaterialDepartment>().WithMany().HasForeignKey(row => row.MaterialDepartmentId).OnDelete(DeleteBehavior.Restrict);
        unitChange.HasOne<DOMAIN.Entities.Departments.Department>().WithMany().HasForeignKey(row => row.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        unitChange.HasOne<DOMAIN.Entities.Users.User>().WithMany().HasForeignKey(row => row.ActorId).OnDelete(DeleteBehavior.Restrict);
        unitChange.HasOne<DOMAIN.Entities.Base.UnitOfMeasure>().WithMany().HasForeignKey(row => row.FromUnitId).OnDelete(DeleteBehavior.Restrict);
        unitChange.HasOne<DOMAIN.Entities.Base.UnitOfMeasure>().WithMany().HasForeignKey(row => row.ToUnitId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Material>().ToTable(table => table.HasCheckConstraint("CK_Material_Density", "\"DensityGramsPerMilliliter\" IS NULL OR \"DensityGramsPerMilliliter\" > 0"));
    }
    private void ValidateMaterialDensityMeasurements()
    {
        if (ChangeTracker.Entries<MaterialDensityMeasurement>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Material density measurements are append-only.");
        if (ChangeTracker.Entries<MaterialCountingUnitChange>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Material counting-unit changes are append-only.");
    }
}
