using System.Reflection;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.Auth;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.BillOfMaterials;
using DOMAIN.Entities.BinCards;
using DOMAIN.Entities.Checklists;
using DOMAIN.Entities.Countries;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Grns;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Organizations;
using DOMAIN.Entities.Procurement.Manufacturers;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.ProductionSchedules.StockTransfers;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Equipments;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Routes;
using DOMAIN.Entities.Shipments;
using DOMAIN.Entities.Sites;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;
using DOMAIN.Entities.WorkOrders;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Configuration = DOMAIN.Entities.Configurations.Configuration;

namespace INFRASTRUCTURE.Context;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUserService currentUserService) : IdentityDbContext<User, Role, Guid>(options)
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
    
    public DbSet<Sr> Srs { get; set; }
    
    public DbSet<MaterialBatch> MaterialBatches { get; set; }
    public DbSet<MaterialBatchEvent> MaterialBatchEvents { get; set; }
    public DbSet<MassMaterialBatchMovement> MassMaterialBatchMovements { get; set; }
    public DbSet<DistributedRequisitionMaterial> DistributedRequisitionMaterials { get; set; }
    
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
    public DbSet<ProductionScheduleProduct> ProductionScheduleProducts { get; set; }
    public DbSet<StockTransfer> StockTransfers { get; set; }
    public DbSet<StockTransferSource> StockTransferSources { get; set; }

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
    public DbSet<RouteResponsibleUser> RouteResponsibleUsers { get; set; }
    public DbSet<RouteResponsibleRole> RouteResponsibleRoles { get; set; }
    public DbSet<RouteWorkCenter> RouteWorkCenters { get; set; }

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
    public DbSet<SupplierQuotation> SupplierQuotations { get; set; }
    public DbSet<SupplierQuotationItem> SupplierQuotationItems { get; set; }

    #endregion

    #region Approvals

    public DbSet<Approval> Approvals { get; set; }
    public DbSet<ApprovalStage> ApprovalStages { get; set; }

    #endregion

    #region Warehouse

    public DbSet<Warehouse> Warehouses { get; set; }
    public DbSet<WarehouseLocation> WarehouseLocations { get; set; }
    public DbSet<WarehouseLocationRack> WarehouseLocationRacks { get; set; }
    public DbSet<WarehouseLocationShelf> WarehouseLocationShelves { get; set; }
    public DbSet<ShelfMaterialBatch> ShelfMaterialBatches { get; set; } 
    public DbSet<WarehouseArrivalLocation> WarehouseArrivalLocations { get; set; }

    #endregion

    #region BinCardInformation

    public DbSet<BinCardInformation> BinCardInformation { get; set; }

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

    #region Attachment

    public DbSet<Attachment> Attachments { get; set; }

    #endregion

    #region Currency

    public DbSet<Currency> Currencies { get; set; }

    #endregion

    #region Purchase Order

    public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
    public DbSet<PurchaseOrderInvoice> PurchaseOrderInvoices { get; set; }
    public DbSet<BillingSheet> BillingSheets { get; set; }
    public DbSet<RevisedPurchaseOrder> RevisedPurchaseOrders { get; set; }

    #endregion
    
    #region Shipment Document

    public DbSet<ShipmentDocument> ShipmentDocuments { get; set; }
    public DbSet<ShipmentInvoice> ShipmentInvoices { get; set; }
    public DbSet<ShipmentDiscrepancy> ShipmentDiscrepancies { get; set; }
    public DbSet<ShipmentDiscrepancyType> ShipmentDiscrepancyTypes { get; set; }
    public DbSet<ShipmentInvoiceItem> ShipmentInvoiceItems { get; set; }

    #endregion

    #region Form

    public DbSet<Form> Forms { get; set; }
    public DbSet<FormSection> FormSections { get; set; }
    public DbSet<FormField> FormFields { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<QuestionOption> QuestionOptions { get; set; }
    public DbSet<Response> Responses { get; set; }
    public DbSet<FormResponse> FormResponses { get; set; }
    public DbSet<FormReviewer> FormReviewers { get; set; }
    public DbSet<FormAssignee> FormAssignees { get; set; }

    #endregion

    #region Production

    public DbSet<BatchManufacturingRecord> BatchManufacturingRecords { get; set; }
    public DbSet<BatchPackagingRecord> BatchPackagingRecords { get; set; }
    public DbSet<ProductionActivity> ProductionActivities { get; set; }
    public DbSet<ProductionActivityStep> ProductionActivitySteps { get; set; }
    public DbSet<ProductionActivityStepUser> ProductionActivityStepUsers { get; set; }
    public DbSet<ProductionActivityStepResource> ProductionActivityStepResources { get; set; }
    public DbSet<ProductionActivityStepWorkCenter> ProductionActivityStepWorkCenters { get; set; }
    public DbSet<ProductionActivityLog> ProductionActivityLogs { get; set; }
    
    #endregion

    #region Checklist

    public DbSet<Checklist> Checklists { get; set; }

    #endregion
    
    #region GRN
    
    public DbSet<Grn> Grns { get; set; }
    
    #endregion

    #region Equipment

    public DbSet<Equipment> Equipments { get; set; }

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
                    entity.CreatedById = currentUserService.UserId;
                    break;
                
                case EntityState.Modified:
                    entity.UpdatedAt = DateTime.UtcNow;
                    entity.LastUpdatedById = currentUserService.UserId;
                    break;
                
                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entity.DeletedAt = DateTime.UtcNow;
                    entity.LastDeletedById = currentUserService.UserId;
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
        modelBuilder.Entity<Department>().Navigation(p => p.Warehouses).AutoInclude();
        // modelBuilder.Entity<DepartmentWarehouse>().Navigation(p => p.Warehouse).AutoInclude();
        #endregion
        
        #region Warehouse Entities
        modelBuilder.Entity<Warehouse>().Navigation(p => p.Locations).AutoInclude();
        modelBuilder.Entity<WarehouseLocation>().Navigation(p => p.Racks).AutoInclude();
        modelBuilder.Entity<WarehouseLocationRack>().Navigation(p => p.Shelves).AutoInclude();

        #endregion
        
        #region Material Entities
        modelBuilder.Entity<Material>().Navigation(p => p.Batches).AutoInclude();
        modelBuilder.Entity<MaterialBatch>().Navigation(p => p.Events).AutoInclude();
        modelBuilder.Entity<ManufacturerMaterial>().Navigation(p => p.Material).AutoInclude();
        #endregion

        #region Product Entities
        modelBuilder.Entity<Product>().Navigation(p => p.Category).AutoInclude();
        modelBuilder.Entity<Product>().Navigation(p => p.BaseUoM).AutoInclude();
        modelBuilder.Entity<Product>().Navigation(p => p.BasePackingUoM).AutoInclude();
        modelBuilder.Entity<FinishedProduct>().Navigation(fp => fp.UoM).AutoInclude();
        modelBuilder.Entity<ProductPackage>().Navigation(pp => pp.Material).AutoInclude();
        modelBuilder.Entity<ProductBillOfMaterial>().Navigation(pbm => pbm.BillOfMaterial).AutoInclude();
        #endregion

        #region Bill of Material Entities
        modelBuilder.Entity<BillOfMaterial>().Navigation(bom => bom.Items).AutoInclude();
        modelBuilder.Entity<BillOfMaterial>().Navigation(bom => bom.Product).AutoInclude();
        modelBuilder.Entity<BillOfMaterialItem>().Navigation(bomi => bomi.Material).AutoInclude();
        modelBuilder.Entity<BillOfMaterialItem>().Navigation(bomi => bomi.MaterialType).AutoInclude();
        modelBuilder.Entity<BillOfMaterialItem>().Navigation(bomi => bomi.BaseUoM).AutoInclude();
        #endregion

        #region Route Entities
        modelBuilder.Entity<Route>().Navigation(r => r.Operation).AutoInclude();
        modelBuilder.Entity<RouteResponsibleUser>().Navigation(r => r.User).AutoInclude();
        modelBuilder.Entity<RouteResponsibleRole>().Navigation(r => r.Role).AutoInclude();
        modelBuilder.Entity<RouteWorkCenter>().Navigation(r => r.WorkCenter).AutoInclude();
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
        #endregion

        #region Purchase Order Entites

        modelBuilder.Entity<PurchaseOrder>().Navigation(p => p.Supplier).AutoInclude();
        modelBuilder.Entity<PurchaseOrder>().Navigation(p => p.Items).AutoInclude();
        modelBuilder.Entity<PurchaseOrderItem>().Navigation(p => p.Material).AutoInclude();
        modelBuilder.Entity<PurchaseOrderItem>().Navigation(p => p.UoM).AutoInclude();
        modelBuilder.Entity<PurchaseOrderItem>().Navigation(p => p.Currency).AutoInclude();
        
        modelBuilder.Entity<RevisedPurchaseOrderItem>().Navigation(p => p.Material).AutoInclude();
        modelBuilder.Entity<RevisedPurchaseOrderItem>().Navigation(p => p.UoM).AutoInclude();
        modelBuilder.Entity<RevisedPurchaseOrderItem>().Navigation(p => p.Currency).AutoInclude();
        
        modelBuilder.Entity<PurchaseOrderInvoice>().Navigation(p => p.BatchItems).AutoInclude();
        modelBuilder.Entity<PurchaseOrderInvoice>().Navigation(p => p.Charges).AutoInclude();
        modelBuilder.Entity<BatchItem>().Navigation(b => b.Manufacturer).AutoInclude();
        modelBuilder.Entity<Charge>().Navigation(b => b.Currency).AutoInclude();

        modelBuilder.Entity<BillingSheet>().Navigation(b => b.Supplier).AutoInclude();
        modelBuilder.Entity<ShipmentInvoice>().Navigation(b => b.Supplier).AutoInclude();

        #endregion

        #region Material Entities

        modelBuilder.Entity<MaterialBatch>().Navigation(p => p.UoM).AutoInclude();

        #endregion

        #region Supplier Entities

        modelBuilder.Entity<Supplier>().Navigation(p => p.Country).AutoInclude();
        modelBuilder.Entity<Supplier>().Navigation(p => p.Currency).AutoInclude();

        #endregion

        #region Form Entities

        modelBuilder.Entity<Form>().Navigation(p => p.Sections).AutoInclude();
        modelBuilder.Entity<FormSection>().Navigation(p => p.Fields).AutoInclude();
        modelBuilder.Entity<FormField>().Navigation(p => p.Question).AutoInclude();
        
        modelBuilder.Entity<Question>().Navigation(p => p.Options).AutoInclude();
        
        #endregion

        #region Production Activity

        modelBuilder.Entity<ProductionActivityStepResource>().Navigation(p => p.Resource).AutoInclude();
        modelBuilder.Entity<ProductionActivityStepUser>().Navigation(p => p.User).AutoInclude();
        modelBuilder.Entity<ProductionActivityStepWorkCenter>().Navigation(p => p.WorkCenter).AutoInclude();
        modelBuilder.Entity<ProductionActivityLog>().Navigation(p => p.User).AutoInclude();

        #endregion

        #region Equipment

        modelBuilder.Entity<Equipment>().Navigation(p => p.Department).AutoInclude();
        modelBuilder.Entity<Equipment>().Navigation(p => p.UoM).AutoInclude();

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
        modelBuilder.Entity<MaterialBatch>().HasQueryFilter(entity => !entity.DeletedAt.HasValue && !entity.Material.DeletedAt.HasValue);// && entity.Status == BatchStatus.Available);
        modelBuilder.Entity<Sr>().HasQueryFilter(entity => !entity.DeletedAt.HasValue && !entity.MaterialBatch.DeletedAt.HasValue);// && entity.Status == BatchStatus.Available);
        modelBuilder.Entity<ShelfMaterialBatch>().HasQueryFilter(entity => !entity.DeletedAt.HasValue && !entity.MaterialBatch.DeletedAt.HasValue);// && entity.Status == BatchStatus.Available);
        modelBuilder.Entity<MaterialBatchEvent>().HasQueryFilter(entity =>
            !entity.DeletedAt.HasValue && !entity.Batch.DeletedAt.HasValue && !entity.User.DeletedAt.HasValue);// && !entity.Batch.IsFrozen);
        modelBuilder.Entity<MassMaterialBatchMovement>().HasQueryFilter(entity => !entity.DeletedAt.HasValue && !entity.Batch.DeletedAt.HasValue);//  && !entity.Batch.IsFrozen);
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
        modelBuilder.Entity<BillOfMaterialItem>().HasQueryFilter(entity => entity.Material != null);
        #endregion

        #region Route Filters
        modelBuilder.Entity<Route>().HasQueryFilter(entity => !entity.DeletedAt.HasValue && !entity.Operation.DeletedAt.HasValue);
        modelBuilder.Entity<RouteResponsibleUser>().HasQueryFilter(entity => !entity.Route.DeletedAt.HasValue && !entity.User.DeletedAt.HasValue);
        modelBuilder.Entity<RouteResponsibleRole>().HasQueryFilter(entity => !entity.Route.DeletedAt.HasValue && !entity.Role.DeletedAt.HasValue);
        modelBuilder.Entity<RouteWorkCenter>().HasQueryFilter(entity => !entity.Route.DeletedAt.HasValue && !entity.WorkCenter.DeletedAt.HasValue);
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
        modelBuilder.Entity<ProductionScheduleProduct>()
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
            .HasQueryFilter(r => !r.DeletedAt.HasValue);
        modelBuilder.Entity<SourceRequisitionItem>()
            .HasQueryFilter(r => !r.SourceRequisition.DeletedAt.HasValue);
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
        // modelBuilder.Entity<DepartmentWarehouse>().HasQueryFilter(a => !a.Department.DeletedAt.HasValue);
        #endregion

        #region Warehouse Filters
        
        modelBuilder.Entity<Warehouse>().HasQueryFilter(a => !a.DeletedAt.HasValue);

        modelBuilder.Entity<WarehouseLocation>().HasQueryFilter(
            a => !a.DeletedAt.HasValue && !a.Warehouse.DeletedAt.HasValue);

        modelBuilder.Entity<WarehouseLocationRack>().HasQueryFilter(
            a => !a.DeletedAt.HasValue && !a.WarehouseLocation.DeletedAt.HasValue);

        modelBuilder.Entity<WarehouseLocationShelf>().HasQueryFilter(
            a => !a.DeletedAt.HasValue && !a.WarehouseLocationRack.DeletedAt.HasValue);
        modelBuilder.Entity<WarehouseArrivalLocation>().HasQueryFilter(
            a => !a.DeletedAt.HasValue && !a.Warehouse.DeletedAt.HasValue);

        #endregion

        #region Attachment Filter

        modelBuilder.Entity<Attachment>().HasQueryFilter(a => !a.DeletedAt.HasValue);

        #endregion

        #region Currency

        modelBuilder.Entity<Currency>().HasQueryFilter(a => !a.DeletedAt.HasValue);

        #endregion

        #region Purchase Order

        modelBuilder.Entity<PurchaseOrder>().HasQueryFilter(a => !a.DeletedAt.HasValue);
        modelBuilder.Entity<PurchaseOrderItem>().HasQueryFilter(a => !a.PurchaseOrder.DeletedAt.HasValue);
        modelBuilder.Entity<PurchaseOrderInvoice>().HasQueryFilter(a => !a.DeletedAt.HasValue);
        modelBuilder.Entity<PurchaseOrderInvoice>().HasQueryFilter(a => !a.PurchaseOrder.DeletedAt.HasValue);
        modelBuilder.Entity<BatchItem>().HasQueryFilter(a => !a.PurchaseOrderInvoice.DeletedAt.HasValue);
        modelBuilder.Entity<Charge>().HasQueryFilter(a => !a.PurchaseOrderInvoice.DeletedAt.HasValue);
        modelBuilder.Entity<BillingSheet>().HasQueryFilter(a => !a.DeletedAt.HasValue);
        modelBuilder.Entity<RevisedPurchaseOrder>().HasQueryFilter(a => !a.PurchaseOrder.DeletedAt.HasValue);
        modelBuilder.Entity<RevisedPurchaseOrderItem>().HasQueryFilter(a => !a.RevisedPurchaseOrder.DeletedAt.HasValue);

        #endregion

        #region SupplierQuotation

        modelBuilder.Entity<SupplierQuotation>().HasQueryFilter(a => !a.Supplier.DeletedAt.HasValue);
        modelBuilder.Entity<SupplierQuotationItem>().HasQueryFilter(a => !a.Material.DeletedAt.HasValue);

        #endregion

        #region Shipment Document
        
        modelBuilder.Entity<ShipmentDocument>().HasQueryFilter(a => !a.DeletedAt.HasValue);
        modelBuilder.Entity<ShipmentDiscrepancy>().HasQueryFilter(a => !a.DeletedAt.HasValue && !a.ShipmentDocument.DeletedAt.HasValue);
        modelBuilder.Entity<ShipmentInvoice>().HasQueryFilter(a => !a.DeletedAt.HasValue );
        modelBuilder.Entity<ShipmentDiscrepancyItem>().HasQueryFilter(a =>  !a.ShipmentDiscrepancy.DeletedAt.HasValue);
        modelBuilder.Entity<ShipmentInvoiceItem>().HasQueryFilter(a => !a.ShipmentInvoice.DeletedAt.HasValue);
        
        #endregion

        #region Form

        modelBuilder.Entity<Form>().HasQueryFilter(a => !a.DeletedAt.HasValue);
        modelBuilder.Entity<FormSection>().HasQueryFilter(a => !a.Form.DeletedAt.HasValue);
        modelBuilder.Entity<FormField>().HasQueryFilter(a => !a.FormSection.DeletedAt.HasValue);
        modelBuilder.Entity<FormAssignee>().HasQueryFilter(a => !a.User.DeletedAt.HasValue && !a.Form.DeletedAt.HasValue);
        modelBuilder.Entity<FormReviewer>().HasQueryFilter(a => !a.User.DeletedAt.HasValue && !a.Form.DeletedAt.HasValue);
        modelBuilder.Entity<FormResponse>().HasQueryFilter(a => !a.FormField.DeletedAt.HasValue);
        modelBuilder.Entity<Response>().HasQueryFilter(a => !a.Form.DeletedAt.HasValue);
        
        modelBuilder.Entity<Question>().HasQueryFilter(a => !a.DeletedAt.HasValue);
        modelBuilder.Entity<QuestionOption>().HasQueryFilter(a => !a.Question.DeletedAt.HasValue);
        #endregion

        #region Production

        modelBuilder.Entity<BatchManufacturingRecord>().HasQueryFilter(a => !a.Product.DeletedAt.HasValue);
        modelBuilder.Entity<BatchPackagingRecord>().HasQueryFilter(a => !a.Product.DeletedAt.HasValue);
        
        modelBuilder.Entity<ProductionActivity>().HasQueryFilter(a => !a.Product.DeletedAt.HasValue);
        modelBuilder.Entity<ProductionActivityStep>().HasQueryFilter(a => !a.Operation.DeletedAt.HasValue);
        modelBuilder.Entity<ProductionActivityStepResource>().HasQueryFilter(a => !a.Resource.DeletedAt.HasValue);
        modelBuilder.Entity<ProductionActivityStepWorkCenter>().HasQueryFilter(a => !a.WorkCenter.DeletedAt.HasValue);
        modelBuilder.Entity<ProductionActivityStepUser>().HasQueryFilter(a => !a.User.DeletedAt.HasValue);
        modelBuilder.Entity<ProductionActivityLog>().HasQueryFilter(a => a.ProductionActivity != null);

        #endregion
        
        #region Stock Transfer

        modelBuilder.Entity<StockTransfer>().HasQueryFilter(entity => !entity.DeletedAt.HasValue && !entity.Material.DeletedAt.HasValue);// && entity.Status == BatchStatus.Available);
        modelBuilder.Entity<StockTransferSource>().HasQueryFilter(entity => !entity.DeletedAt.HasValue && !entity.FromDepartment.DeletedAt.HasValue && !entity.ToDepartment.DeletedAt.HasValue);// && entity.Status == BatchStatus.Available);


        #endregion

        #region Equipment

        modelBuilder.Entity<Equipment>().HasQueryFilter(entity => !entity.DeletedAt.HasValue && !entity.Department.DeletedAt.HasValue);

        #endregion
    }
}