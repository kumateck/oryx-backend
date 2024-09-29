using System.Configuration;
using System.Reflection;
using DOMAIN.Entities.Auth;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.BillOfMaterials;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Organizations;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Routes;
using DOMAIN.Entities.Sites;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.WorkOrders;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SHARED.Provider;
using Configuration = DOMAIN.Entities.Configurations.Configuration;

namespace INFRASTRUCTURE.Context;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantProvider tenantProvider) : IdentityDbContext<User, Role, Guid>(options)
{
    
    #region Auth
    public DbSet<PasswordReset> PasswordResets { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    
    #endregion

    #region Organization

    public DbSet<Organization> Organizations { get; set; }
    public DbSet<Site> Sites { get; set; }

    #endregion

    #region Resources

    public DbSet<Resource> Resources { get; set; }

    #endregion

    #region Material
    
    public DbSet<Material> Materials { get; set; }
    public DbSet<MaterialType> MaterialTypes { get; set; }
    public DbSet<MaterialCategory> MaterialCategories { get; set; }
    
    #endregion

    #region UnitOfMeasure

    public DbSet<UnitOfMeasure> UnitOfMeasures { get; set; }

    #endregion

    #region Products

    public DbSet<Product> Products { get; set; }
    public DbSet<ProductCategory> ProductCategories { get; set; }
    public DbSet<ProductBillOfMaterial> ProductBillOfMaterials { get; set; }
    public DbSet<FinishedProduct> FinishedProducts { get; set; }
    public DbSet<ProductPackage> ProductPackages { get; set; }
    public DbSet<PackageType> PackageTypes { get; set; }

    #endregion

    #region BoM

    public DbSet<BillOfMaterial> BillOfMaterials { get; set; }
    public DbSet<BillOfMaterialItem> BillOfMaterialItems { get; set; }

    #endregion

    #region WorkOrder

    public DbSet<WorkOrder> WorkOrders { get; set; }
    public DbSet<ProductionStep> ProductionSteps { get; set; }

    #endregion

    #region ProductionSchedule

    public DbSet<ProductionSchedule> ProductionSchedules { get; set; }
    public DbSet<MasterProductionSchedule> MasterProductionSchedules { get; set; }

    #endregion

    #region WorkCenter

    public DbSet<WorkCenter> WorkCenters { get; set; }

    #endregion

    #region Operation

    public DbSet<Operation> Operations { get; set; }

    #endregion

    #region Route

    public DbSet<Route> Routes { get; set; }
    public DbSet<RouteResource> RouteResources { get; set; }

    #endregion

    #region Configuration
    public DbSet<Configuration> Configurations { get; set; }
    #endregion

    #region TenantFilter
    private void ApplyTenantQueryFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : class, IBaseEntity, IOrganizationType
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(entity => entity.OrganizationName == tenantProvider.Tenant && !entity.DeletedAt.HasValue);
    }
    #endregion
    
    #region SoftDeleteFilter
    private static void ApplyDeletedAtFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : class, IBaseEntity
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);
    }
    #endregion
    
    
    public override int SaveChanges()
    {
        SaveEntity();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveEntity();
        return await base.SaveChangesAsync(cancellationToken);
    }
    
    private void SaveEntity()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e is { Entity: BaseEntity, State: EntityState.Added or EntityState.Modified or EntityState.Deleted });

        foreach (var entry in entries)
        {
            var entity = (BaseEntity)entry.Entity;

            switch (entry.State) 
            {
                case EntityState.Added:
                    entity.CreatedAt = DateTime.UtcNow;
                    break;
                
                case EntityState.Modified:
                    entity.UpdatedAt = DateTime.UtcNow;
                    break;
                
                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entity.DeletedAt = DateTime.UtcNow;
                    break;
            }
            
            if (entry.Entity is IOrganizationType organization)
            {
                organization.OrganizationName = tenantProvider.Tenant;
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<Role>().ToTable("roles");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("userroles");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("userclaims");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("userlogins");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("roleclaims");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("usertokens");
        
        //eager load relations
        modelBuilder.Entity<Product>().Navigation(fr => fr.Category).AutoInclude();
        modelBuilder.Entity<FinishedProduct>().Navigation(fr => fr.UoM).AutoInclude();
        modelBuilder.Entity<ProductPackage>().Navigation(fr => fr.Material).AutoInclude();
        modelBuilder.Entity<ProductPackage>().Navigation(fr => fr.PackageType).AutoInclude();
        modelBuilder.Entity<ProductBillOfMaterial>().Navigation(fr => fr.BillOfMaterial).AutoInclude();
        modelBuilder.Entity<BillOfMaterial>().Navigation(fr => fr.Items).AutoInclude();
        modelBuilder.Entity<BillOfMaterial>().Navigation(fr => fr.Product).AutoInclude();
        
        
        //apply global filters
         // Existing query filters
        modelBuilder.Entity<User>().HasQueryFilter(entity =>
            entity.OrganizationName == tenantProvider.Tenant && !entity.DeletedAt.HasValue);

        modelBuilder.Entity<Site>().HasQueryFilter(entity =>
            entity.OrganizationName == tenantProvider.Tenant && !entity.DeletedAt.HasValue);

        modelBuilder.Entity<PasswordReset>().HasQueryFilter(entity =>
            !entity.DeletedAt.HasValue || !entity.User.DeletedAt.HasValue);

        modelBuilder.Entity<Product>().HasQueryFilter(entity =>
            !entity.DeletedAt.HasValue);
        
        modelBuilder.Entity<ProductPackage>().HasQueryFilter(entity =>
            !entity.DeletedAt.HasValue || !entity.Product.DeletedAt.HasValue);

        modelBuilder.Entity<WorkCenter>().HasQueryFilter(entity =>
            !entity.DeletedAt.HasValue);

        modelBuilder.Entity<ProductCategory>().HasQueryFilter(entity =>
            !entity.DeletedAt.HasValue);
        
        modelBuilder.Entity<FinishedProduct>().HasQueryFilter(entity =>
            !entity.DeletedAt.HasValue || !entity.Product.DeletedAt.HasValue);

        modelBuilder.Entity<Resource>().HasQueryFilter(entity =>
            !entity.DeletedAt.HasValue);

        modelBuilder.Entity<UnitOfMeasure>().HasQueryFilter(entity =>
            !entity.DeletedAt.HasValue);

        modelBuilder.Entity<Operation>().HasQueryFilter(entity =>
            !entity.DeletedAt.HasValue);

        modelBuilder.Entity<Configuration>().HasQueryFilter(entity =>
            !entity.DeletedAt.HasValue);

        // Adjusted query filters to resolve warnings
        modelBuilder.Entity<WorkOrder>().HasQueryFilter(entity =>
            !entity.DeletedAt.HasValue);

        // ProductionSchedule depends on WorkOrder
        modelBuilder.Entity<ProductionSchedule>().HasQueryFilter(entity =>
            !entity.DeletedAt.HasValue && !entity.WorkOrder.DeletedAt.HasValue);

        // ProductionStep depends on WorkOrder
        modelBuilder.Entity<ProductionStep>().HasQueryFilter(entity =>
            !entity.DeletedAt.HasValue && !entity.WorkOrder.DeletedAt.HasValue);

        // Other adjusted query filters from previous steps
        modelBuilder.Entity<BillOfMaterial>().HasQueryFilter(entity =>
            !entity.DeletedAt.HasValue && !entity.Product.DeletedAt.HasValue);

        modelBuilder.Entity<BillOfMaterialItem>().HasQueryFilter(entity =>
            !entity.DeletedAt.HasValue && !entity.UoM.DeletedAt.HasValue);

        modelBuilder.Entity<MasterProductionSchedule>().HasQueryFilter(entity =>
            !entity.DeletedAt.HasValue && !entity.Product.DeletedAt.HasValue);

        modelBuilder.Entity<ProductBillOfMaterial>().HasQueryFilter(entity =>
            !entity.DeletedAt.HasValue && !entity.Product.DeletedAt.HasValue);

        modelBuilder.Entity<Route>().HasQueryFilter(entity =>
            !entity.DeletedAt.HasValue && !entity.Operation.DeletedAt.HasValue);

        modelBuilder.Entity<RouteResource>().HasQueryFilter(entity =>
            !entity.DeletedAt.HasValue && !entity.Resource.DeletedAt.HasValue);
        
        modelBuilder.Entity<MaterialType>().HasQueryFilter(entity =>
            !entity.DeletedAt.HasValue);
        
        modelBuilder.Entity<MaterialCategory>().HasQueryFilter(entity =>
            !entity.DeletedAt.HasValue);
        
        modelBuilder.Entity<PackageType>().HasQueryFilter(entity =>
            !entity.DeletedAt.HasValue);
    }
}