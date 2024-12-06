using System.Reflection;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Auth;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.BillOfMaterials;
using DOMAIN.Entities.Countries;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Organizations;
using DOMAIN.Entities.Procurement.Manufacturers;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Routes;
using DOMAIN.Entities.Sites;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;
using DOMAIN.Entities.WorkOrders;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Configuration = DOMAIN.Entities.Configurations.Configuration;

namespace INFRASTRUCTURE.Context;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options/* , ITenantProvider tenantProvider*/) : IdentityDbContext<User, Role, Guid>(options)
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
    
    public DbSet<MaterialBatch> MaterialBatches { get; set; }
    public DbSet<MaterialBatchEvent> MaterialBatchEvents { get; set; }
    public DbSet<MaterialBatchMovement> MaterialBatchMovements { get; set; }
    
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
    public DbSet<ProductionScheduleItem> ProductionScheduleItems { get; set; }
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

    #region Requisition

    public DbSet<Requisition> Requisitions { get; set; }
    public DbSet<RequisitionItem> RequisitionItems { get; set; }
    public DbSet<RequisitionApproval> RequisitionApprovals { get; set; }
    public DbSet<CompletedRequisition> CompletedRequisitions { get; set; }
    public DbSet<CompletedRequisitionItem> CompletedRequisitionItems { get; set; }
    
    public DbSet<SourceRequisition> SourceRequisitions { get; set; }
    public DbSet<SourceRequisitionItem> SourceRequisitionItems { get; set; }
    public DbSet<SourceRequisitionItemSupplier> SourceRequisitionItemSuppliers { get; set; }

    #endregion

    #region Approvals

    public DbSet<Approval> Approvals { get; set; }
    public DbSet<ApprovalStage> ApprovalStages { get; set; }

    #endregion

    #region Warehouse

    public DbSet<Warehouse> Warehouses { get; set; }
    public DbSet<WarehouseLocation> WarehouseLocations { get; set; }

    #endregion

    #region Procurement

    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<SupplierManufacturer> SupplierManufacturers { get; set; }
    public DbSet<Manufacturer> Manufacturers { get; set; }
    public DbSet<ManufacturerMaterial> ManufacturerMaterials { get; set; }

    #endregion

    #region Country

    public DbSet<Country> Countries { get; set; }

    #endregion

    #region Department

    public DbSet<Department> Departments { get; set; }

    #endregion
    

    // #region TenantFilter
    // private void ApplyTenantQueryFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : class, IBaseEntity, IOrganizationType
    // {
    //     modelBuilder.Entity<TEntity>().HasQueryFilter(entity => entity.OrganizationName == tenantProvider.Tenant && !entity.DeletedAt.HasValue);
    // }
    // #endregion
    
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
            
            // if (entry.Entity is IOrganizationType organization)
            // {
            //     organization.OrganizationName = tenantProvider.Tenant;
            // }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply configurations from the assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Call base method
        base.OnModelCreating(modelBuilder);

        ConfigureTableMappings(modelBuilder);
        ConfigureAutoIncludes(modelBuilder);
        ConfigureQueryFilters(modelBuilder);
    }

    private void ConfigureTableMappings(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<Role>().ToTable("roles");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("userroles");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("userclaims");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("userlogins");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("roleclaims");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("usertokens");
    }

    private void ConfigureAutoIncludes(ModelBuilder modelBuilder)
    {
        #region User Entities
        modelBuilder.Entity<User>().Navigation(p => p.Department).AutoInclude();
        #endregion
        
        #region Department Entities
        modelBuilder.Entity<Department>().Navigation(p => p.Warehouse).AutoInclude();
        #endregion
        
        #region Warehouse Entities
        modelBuilder.Entity<Warehouse>().Navigation(p => p.Locations).AutoInclude();
        #endregion
        
        #region Material Entities
        modelBuilder.Entity<Material>().Navigation(p => p.Batches).AutoInclude();
        modelBuilder.Entity<MaterialBatch>().Navigation(p => p.Events).AutoInclude();
        #endregion

        #region Product Entities
        modelBuilder.Entity<Product>().Navigation(p => p.Category).AutoInclude();
        modelBuilder.Entity<FinishedProduct>().Navigation(fp => fp.UoM).AutoInclude();
        modelBuilder.Entity<ProductPackage>().Navigation(pp => pp.Material).AutoInclude();
        modelBuilder.Entity<ProductPackage>().Navigation(pp => pp.PackageType).AutoInclude();
        modelBuilder.Entity<ProductBillOfMaterial>().Navigation(pbm => pbm.BillOfMaterial).AutoInclude();
        #endregion

        #region Bill of Material Entities
        modelBuilder.Entity<BillOfMaterial>().Navigation(bom => bom.Items).AutoInclude();
        modelBuilder.Entity<BillOfMaterial>().Navigation(bom => bom.Product).AutoInclude();
        modelBuilder.Entity<BillOfMaterialItem>().Navigation(bomi => bomi.ComponentMaterial).AutoInclude();
        modelBuilder.Entity<BillOfMaterialItem>().Navigation(bomi => bomi.ComponentProduct).AutoInclude();
        modelBuilder.Entity<BillOfMaterialItem>().Navigation(bomi => bomi.MaterialType).AutoInclude();
        modelBuilder.Entity<BillOfMaterialItem>().Navigation(bomi => bomi.UoM).AutoInclude();
        #endregion

        #region Route Entities
        modelBuilder.Entity<Route>().Navigation(r => r.Operation).AutoInclude();
        modelBuilder.Entity<Route>().Navigation(r => r.WorkCenter).AutoInclude();
        modelBuilder.Entity<Route>().Navigation(r => r.Resources).AutoInclude();
        modelBuilder.Entity<RouteResource>().Navigation(rr => rr.Resource).AutoInclude();
        #endregion

        #region Approval Entities
        modelBuilder.Entity<Approval>().Navigation(r => r.ApprovalStages).AutoInclude();
        modelBuilder.Entity<ApprovalStage>().Navigation(r => r.User).AutoInclude();
        modelBuilder.Entity<ApprovalStage>().Navigation(r => r.Role).AutoInclude();
        #endregion
        
        #region Requsition Entities
        modelBuilder.Entity<Requisition>().Navigation(r => r.CreatedBy).AutoInclude();
        modelBuilder.Entity<SourceRequisition>().Navigation(r => r.CreatedBy).AutoInclude();
        modelBuilder.Entity<RequisitionItem>().Navigation(r => r.UoM).AutoInclude();
        modelBuilder.Entity<SourceRequisitionItem>().Navigation(r => r.UoM).AutoInclude();
        modelBuilder.Entity<SourceRequisitionItemSupplier>().Navigation(r => r.Supplier).AutoInclude();
        #endregion
    }

    private void ConfigureQueryFilters(ModelBuilder modelBuilder)
    {
        #region Auth Filters
        modelBuilder.Entity<User>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);
        modelBuilder.Entity<PasswordReset>().HasQueryFilter(entity => !entity.DeletedAt.HasValue && !entity.User.DeletedAt.HasValue);
        #endregion

        #region Product Filters
        modelBuilder.Entity<Product>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);
        modelBuilder.Entity<ProductPackage>().HasQueryFilter(entity => !entity.DeletedAt.HasValue && !entity.Product.DeletedAt.HasValue);
        modelBuilder.Entity<ProductCategory>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);
        modelBuilder.Entity<FinishedProduct>().HasQueryFilter(entity => !entity.DeletedAt.HasValue && !entity.Product.DeletedAt.HasValue);
        #endregion

        #region Material Filters
        modelBuilder.Entity<Material>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);
        modelBuilder.Entity<MaterialBatch>().HasQueryFilter(entity => !entity.DeletedAt.HasValue && !entity.Material.DeletedAt.HasValue);
        modelBuilder.Entity<MaterialBatchEvent>().HasQueryFilter(entity => !entity.DeletedAt.HasValue && !entity.User.DeletedAt.HasValue);
        modelBuilder.Entity<MaterialBatchEvent>().HasQueryFilter(entity => !entity.DeletedAt.HasValue && !entity.Batch.DeletedAt.HasValue);
        modelBuilder.Entity<MaterialBatchMovement>().HasQueryFilter(entity => !entity.DeletedAt.HasValue && !entity.Batch.DeletedAt.HasValue);
        modelBuilder.Entity<MaterialCategory>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);
        modelBuilder.Entity<MaterialType>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);
        #endregion

        #region Requisition Filters
        modelBuilder.Entity<RequisitionApproval>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);
        #endregion

        #region WorkOrder Filters
        modelBuilder.Entity<WorkOrder>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);
        modelBuilder.Entity<ProductionStep>().HasQueryFilter(entity => !entity.DeletedAt.HasValue && !entity.WorkOrder.DeletedAt.HasValue);
        #endregion

        #region BoM Filters
        modelBuilder.Entity<BillOfMaterial>().HasQueryFilter(entity => !entity.DeletedAt.HasValue && !entity.Product.DeletedAt.HasValue);
        modelBuilder.Entity<ProductBillOfMaterial>().HasQueryFilter(entity => !entity.DeletedAt.HasValue && !entity.BillOfMaterial.DeletedAt.HasValue);
        modelBuilder.Entity<ProductBillOfMaterial>().HasQueryFilter(entity => !entity.DeletedAt.HasValue && !entity.Product.DeletedAt.HasValue);
        modelBuilder.Entity<BillOfMaterialItem>().HasQueryFilter(entity => !entity.DeletedAt.HasValue && !entity.UoM.DeletedAt.HasValue);
        #endregion

        #region Route Filters
        modelBuilder.Entity<Route>().HasQueryFilter(entity => !entity.DeletedAt.HasValue && !entity.Operation.DeletedAt.HasValue);
        modelBuilder.Entity<RouteResource>().HasQueryFilter(entity => !entity.DeletedAt.HasValue && !entity.Resource.DeletedAt.HasValue);
        #endregion

        #region Configuration Filters
        modelBuilder.Entity<Configuration>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);
        #endregion

        #region Miscellaneous Filters
        modelBuilder.Entity<Resource>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);
        modelBuilder.Entity<UnitOfMeasure>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);
        modelBuilder.Entity<Operation>().HasQueryFilter(entity => !entity.DeletedAt.HasValue);
        #endregion

        #region MasterProductionSchedule Filters
        modelBuilder.Entity<MasterProductionSchedule>()
            .HasQueryFilter(mps => !mps.Product.DeletedAt.HasValue);
        modelBuilder.Entity<ProductionSchedule>()
            .HasQueryFilter(mps => !mps.DeletedAt.HasValue);
        modelBuilder.Entity<ProductionSchedule>()
            .HasQueryFilter(mps => !mps.Product.DeletedAt.HasValue);
        modelBuilder.Entity<ProductionScheduleItem>()
            .HasQueryFilter(mps => !mps.ProductionSchedule.DeletedAt.HasValue);
        #endregion

        #region Requisition Filters
        modelBuilder.Entity<Requisition>()
            .HasQueryFilter(r => !r.DeletedAt.HasValue);
        modelBuilder.Entity<CompletedRequisition>()
            .HasQueryFilter(r => !r.Requisition.DeletedAt.HasValue);
        modelBuilder.Entity<RequisitionItem>()
            .HasQueryFilter(r => !r.Requisition.DeletedAt.HasValue);
        modelBuilder.Entity<CompletedRequisitionItem>()
            .HasQueryFilter(r => !r.CompletedRequisition.DeletedAt.HasValue);
        modelBuilder.Entity<SourceRequisition>()
            .HasQueryFilter(r => !r.Requisition.DeletedAt.HasValue);
        modelBuilder.Entity<SourceRequisition>()
            .HasQueryFilter(r => !r.DeletedAt.HasValue);
        modelBuilder.Entity<SourceRequisitionItem>()
            .HasQueryFilter(r => !r.SourceRequisition.DeletedAt.HasValue);
        modelBuilder.Entity<SourceRequisitionItemSupplier>()
            .HasQueryFilter(r => !r.SourceRequisitionItem.DeletedAt.HasValue);
        #endregion

        #region Approval Filters
        modelBuilder.Entity<Approval>().HasQueryFilter(a => !a.DeletedAt.HasValue);
        modelBuilder.Entity<ApprovalStage>().HasQueryFilter(a => !a.Approval.DeletedAt.HasValue);
        #endregion

        #region Procurement Filters
        modelBuilder.Entity<Supplier>().HasQueryFilter(a => !a.DeletedAt.HasValue);
        modelBuilder.Entity<SupplierManufacturer>().HasQueryFilter(a => !a.Supplier.DeletedAt.HasValue);
        modelBuilder.Entity<Manufacturer>().HasQueryFilter(a => !a.DeletedAt.HasValue);
        modelBuilder.Entity<ManufacturerMaterial>().HasQueryFilter(a => !a.Manufacturer.DeletedAt.HasValue);
        #endregion

        #region Department Filters
        modelBuilder.Entity<Department>().HasQueryFilter(a => !a.DeletedAt.HasValue);
        #endregion

        #region Warehouse Filters
        modelBuilder.Entity<Warehouse>().HasQueryFilter(a => !a.DeletedAt.HasValue);
        modelBuilder.Entity<WarehouseLocation>().HasQueryFilter(a => !a.DeletedAt.HasValue);
        modelBuilder.Entity<WarehouseLocation>().HasQueryFilter(a => !a.Warehouse.DeletedAt.HasValue);
        #endregion
    }
}