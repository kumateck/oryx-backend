using DOMAIN.Entities.Materials.WhitePlacebo;
using Microsoft.EntityFrameworkCore;

namespace INFRASTRUCTURE.Context;

public partial class ApplicationDbContext
{
    public DbSet<WhitePlaceboRecipe> WhitePlaceboRecipes => Set<WhitePlaceboRecipe>();
    public DbSet<WhitePlaceboPreparation> WhitePlaceboPreparations => Set<WhitePlaceboPreparation>();

    private static void ConfigureWhitePlacebo(ModelBuilder builder)
    {
        builder.Entity<DOMAIN.Entities.Warehouses.ShelfMaterialBatch>().Property(item => item.Quantity).IsConcurrencyToken();
        builder.Entity<DOMAIN.Entities.Materials.Batch.MaterialBatch>().Property(item => item.ConsumedQuantity).IsConcurrencyToken();
        var recipe = builder.Entity<WhitePlaceboRecipe>();
        recipe.HasQueryFilter(item => item.DeletedAt == null);
        recipe.HasIndex(item => item.DepartmentId).IsUnique();
        recipe.Property(item => item.Revision).IsConcurrencyToken();
        recipe.HasOne(item => item.Material).WithMany().HasForeignKey(item => item.MaterialId).OnDelete(DeleteBehavior.Restrict);
        recipe.HasOne(item => item.UoM).WithMany().HasForeignKey(item => item.UoMId).OnDelete(DeleteBehavior.Restrict);
        recipe.HasMany(item => item.Ingredients).WithOne().HasForeignKey(item => item.WhitePlaceboRecipeId);
        var ingredient = builder.Entity<WhitePlaceboRecipeIngredient>();
        ingredient.HasQueryFilter(item => item.DeletedAt == null);
        ingredient.HasOne(item => item.Material).WithMany().HasForeignKey(item => item.MaterialId).OnDelete(DeleteBehavior.Restrict);
        ingredient.HasOne(item => item.UoM).WithMany().HasForeignKey(item => item.UoMId).OnDelete(DeleteBehavior.Restrict);
        var preparation = builder.Entity<WhitePlaceboPreparation>();
        preparation.HasQueryFilter(item => item.DeletedAt == null);
        preparation.HasIndex(item => new { item.MaterialId, item.BatchNumber }).IsUnique();
        preparation.HasIndex(item => item.RequisitionId).IsUnique();
        preparation.Property(item => item.IssuedAt).IsConcurrencyToken();
        preparation.Property(item => item.OutputBatchId).IsConcurrencyToken();
        preparation.HasOne(item => item.Material).WithMany().HasForeignKey(item => item.MaterialId).OnDelete(DeleteBehavior.Restrict);
        preparation.HasOne(item => item.UoM).WithMany().HasForeignKey(item => item.UoMId).OnDelete(DeleteBehavior.Restrict);
        preparation.HasOne(item => item.Requisition).WithMany().HasForeignKey(item => item.RequisitionId).OnDelete(DeleteBehavior.Restrict);
        preparation.HasOne(item => item.OutputBatch).WithMany().HasForeignKey(item => item.OutputBatchId).OnDelete(DeleteBehavior.Restrict);
        preparation.HasMany(item => item.Inputs).WithOne().HasForeignKey(item => item.WhitePlaceboPreparationId);
        builder.Entity<WhitePlaceboPreparationInput>().HasQueryFilter(item => item.DeletedAt == null);
        builder.Entity<WhitePlaceboPreparationInput>().HasOne(item => item.MaterialBatch).WithMany()
            .HasForeignKey(item => item.MaterialBatchId).OnDelete(DeleteBehavior.Restrict);
    }
}
