using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Charges;
using DOMAIN.Entities.Countries;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Instruments;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.ShiftAssignments;
using DOMAIN.Entities.Shipments;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class CollectionRepository(ApplicationDbContext context, IMapper mapper) : ICollectionRepository
{
    public async Task<Result<IEnumerable<CollectionItemDto>>> GetItemCollection(string itemType, MaterialKind? materialKind)
    {
        return itemType switch
        {
            nameof(ProductCategory) => mapper.Map<List<CollectionItemDto>>(
                await context.ProductCategories.ToListAsync()),
            nameof(Resource) => mapper.Map<List<CollectionItemDto>>(await context.Resources.ToListAsync()),
            nameof(UnitOfMeasure) => mapper.Map<List<CollectionItemDto>>(await context.UnitOfMeasures.ToListAsync()),
            nameof(PackageStyle) => mapper.Map<List<CollectionItemDto>>(await context.PackageStyles.ToListAsync()),
            nameof(DeliveryMode) => mapper.Map<List<CollectionItemDto>>(await context.DeliveryModes.ToListAsync()),
            nameof(TermsOfPayment) => mapper.Map<List<CollectionItemDto>>(await context.TermsOfPayments.ToListAsync()),
            nameof(WorkCenter) => mapper.Map<List<CollectionItemDto>>(await context.WorkCenters.ToListAsync()),
            nameof(Operation) => mapper.Map<List<CollectionItemDto>>(await context.Operations.ToListAsync()),
            nameof(MaterialType) => mapper.Map<List<CollectionItemDto>>(await context.MaterialTypes.ToListAsync()),
            nameof(MaterialCategory) => await GetMaterialCategories(materialKind),
            nameof(ShiftCategory) => mapper.Map<List<CollectionItemDto>>(await context.ShiftCategories.Where(sc => sc.DeletedAt == null).ToListAsync()),
            nameof(PackageType) => mapper.Map<List<CollectionItemDto>>(await context.PackageTypes.ToListAsync()),
            nameof(User) => mapper.Map<List<CollectionItemDto>>(await context.Users.ToListAsync()),
            nameof(Role) => mapper.Map<List<CollectionItemDto>>(await context.Roles.ToListAsync()),
            nameof(Country) => mapper.Map<List<CollectionItemDto>>(await context.Countries.OrderBy(c => c.Name).ToListAsync()),
            nameof(WarehouseLocation) => mapper.Map<List<CollectionItemDto>>(await context.WarehouseLocations.OrderBy(c => c.Name).Include(w => w.Warehouse).ToListAsync()),
            nameof(Warehouse) => mapper.Map<List<CollectionItemDto>>(await context.Warehouses.OrderBy(c => c.Name).ToListAsync()),
            nameof(WarehouseLocationRack) => mapper.Map<List<CollectionItemDto>>(await context.WarehouseLocationRacks.OrderBy(c => c.Name).ToListAsync()),
            nameof(WarehouseLocationShelf) => mapper.Map<List<CollectionItemDto>>(await context.WarehouseLocationShelves.OrderBy(c => c.Name).ToListAsync()),
            nameof(Currency) => mapper.Map<List<CollectionItemDto>>(await context.Currencies.OrderBy(c => c.Name).ToListAsync()),
            nameof(ShipmentDiscrepancyType) => mapper.Map<List<CollectionItemDto>>(await context.ShipmentDiscrepancyTypes.OrderBy(c => c.Name).ToListAsync()),
            nameof(Department) => mapper.Map<List<CollectionItemDto>>(await context.Departments.OrderBy(c => c.Name).ToListAsync()),
            nameof(Charge) => mapper.Map<List<CollectionItemDto>>(await context.Charges.OrderBy(c => c.Name).ToListAsync()),
            nameof(ProductState) => mapper.Map<List<CollectionItemDto>>(await context.ProductStates.OrderBy(c => c.Name).ToListAsync()),
            nameof(MarketType) => mapper.Map<List<CollectionItemDto>>(await context.MarketTypes.OrderBy(c => c.Name).ToListAsync()),
            nameof(Instrument) => mapper.Map<List<CollectionItemDto>>(await context.Instruments.OrderBy(c => c.Name).ToListAsync()),
            nameof(ItemCategory) => mapper.Map<List<CollectionItemDto>>(await context.ItemCategories.OrderBy(c => c.Name).ToListAsync()),
            _ => Error.Validation("Item", "Invalid item type")
        };
    }
    
    private async Task<List<CollectionItemDto>> GetMaterialCategories(MaterialKind? materialKind)
    {
        var materialCategories = await context.MaterialCategories.ToListAsync();
        if (materialKind != null)
        {
            materialCategories = materialCategories.Where(m => m.MaterialKind == materialKind).ToList();
        }
        return mapper.Map<List<CollectionItemDto>>(materialCategories);
    }
    
    public async Task<Result<Dictionary<string, IEnumerable<CollectionItemDto>>>> GetItemCollection(List<string> itemTypes, MaterialKind? materialKind = null)
    {
        var result = new Dictionary<string, IEnumerable<CollectionItemDto>>();
        var invalidItemTypes = new List<string>();

        foreach (var itemType in itemTypes)
        {
            switch (itemType)
            {
                case nameof(ProductCategory):
                    var productCategories = await context.ProductCategories.ToListAsync();
                    result[itemType] = mapper.Map<List<CollectionItemDto>>(productCategories);
                    break;

                case nameof(Resource):
                    var resources = await context.Resources.ToListAsync();
                    result[itemType] = mapper.Map<List<CollectionItemDto>>(resources);
                    break;

                case nameof(UnitOfMeasure):
                    var units = await context.UnitOfMeasures.ToListAsync();
                    result[itemType] = mapper.Map<List<CollectionItemDto>>(units);
                    break;
                
                case nameof(PackageStyle):
                    var packageStyles = await context.PackageStyles.ToListAsync();
                    result[itemType] = mapper.Map<List<CollectionItemDto>>(packageStyles);
                    break;
                
                case nameof(TermsOfPayment):
                    var termsOfPayments = await context.TermsOfPayments.ToListAsync();
                    result[itemType] = mapper.Map<List<CollectionItemDto>>(termsOfPayments);
                    break;
                
                case nameof(DeliveryMode):
                    var deliveryModes = await context.DeliveryModes.ToListAsync();
                    result[itemType] = mapper.Map<List<CollectionItemDto>>(deliveryModes);
                    break;

                case nameof(WorkCenter):
                    var workCenters = await context.WorkCenters.ToListAsync();
                    result[itemType] = mapper.Map<List<CollectionItemDto>>(workCenters);
                    break;

                case nameof(Operation):
                    var operations = await context.Operations.ToListAsync();
                    result[itemType] = mapper.Map<List<CollectionItemDto>>(operations);
                    break;
                
                case nameof(MaterialType):
                    var materialType = await context.MaterialTypes.ToListAsync();
                    result[itemType] = mapper.Map<List<CollectionItemDto>>(materialType);
                    break;
                
                case nameof(MaterialCategory):
                    var materialCategory = await context.MaterialCategories.ToListAsync();
                    if(materialKind != null) materialCategory = materialCategory.Where(m => m.MaterialKind == materialKind).ToList();
                    result[itemType] = mapper.Map<List<CollectionItemDto>>(materialCategory);
                    break;
                
                case nameof(ShiftCategory):
                    var shiftCategory = await context.ShiftCategories.Where(sc => sc.DeletedAt == null).ToListAsync();
                    result[itemType] = mapper.Map<List<CollectionItemDto>>(shiftCategory);
                    break;
                
                case nameof(PackageType):
                    var packageType = await context.PackageTypes.ToListAsync();
                    result[itemType] = mapper.Map<List<CollectionItemDto>>(packageType);
                    break;
                
                case nameof(User):
                    var user = await context.Users.ToListAsync();
                    result[itemType] = mapper.Map<List<CollectionItemDto>>(user);
                    break;
                
                case nameof(Role):
                    var role = await context.Roles.ToListAsync();
                    result[itemType] = mapper.Map<List<CollectionItemDto>>(role);
                    break;
                
                case nameof(Country):
                    var countries = await context.Countries.OrderBy(c => c.Name).ToListAsync();
                    result[itemType] = mapper.Map<List<CollectionItemDto>>(countries);
                    break;
                
                case nameof(WarehouseLocation):
                    var warehouseLocations = await context.WarehouseLocations.OrderBy(c => c.Name).Include(w => w.Warehouse).ToListAsync();
                    result[itemType] = mapper.Map<List<CollectionItemDto>>(warehouseLocations);
                    break;
                
                case nameof(Warehouse):
                    var warehouses = await context.Warehouses.OrderBy(c => c.Name).ToListAsync();
                    result[itemType] = mapper.Map<List<CollectionItemDto>>(warehouses);
                    break;
                
                case nameof(Currency):
                    var currencies = await context.Currencies.OrderBy(c => c.Name).ToListAsync();
                    result[itemType] = mapper.Map<List<CollectionItemDto>>(currencies);
                    break;
                
                case nameof(WarehouseLocationRack):
                    var warehouseLocationRacks = await context.WarehouseLocationRacks.OrderBy(c => c.Name).ToListAsync();
                    result[itemType] = mapper.Map<List<CollectionItemDto>>(warehouseLocationRacks);
                    break;
                
                case nameof(WarehouseLocationShelf):
                    var warehouseLocationShelves = await context.WarehouseLocationShelves.OrderBy(c => c.Name).ToListAsync();
                    result[itemType] = mapper.Map<List<CollectionItemDto>>(warehouseLocationShelves);
                    break;
                
                case nameof(ShipmentDiscrepancyType):
                    var shipmentDiscrepancyTypes = await context.ShipmentDiscrepancyTypes.OrderBy(c => c.Name).ToListAsync();
                    result[itemType] = mapper.Map<List<CollectionItemDto>>(shipmentDiscrepancyTypes);
                    break;
                
                case nameof(Department):
                    var departments = await context.Departments.OrderBy(c => c.Name).ToListAsync();
                    result[itemType] = mapper.Map<List<CollectionItemDto>>(departments);
                    break;
                
                case nameof(Charge):
                    var charges = await context.Charges.OrderBy(c => c.Name).ToListAsync();
                    result[itemType] = mapper.Map<List<CollectionItemDto>>(charges);
                    break;
                
                case nameof(ProductState):
                    var productStates = await context.ProductStates.OrderBy(c => c.Name).ToListAsync();
                    result[itemType] = mapper.Map<List<CollectionItemDto>>(productStates);
                    break;
                
                case nameof(MarketType):
                    var marketType = await context.MarketTypes.OrderBy(c => c.Name).ToListAsync();
                    result[itemType] = mapper.Map<List<CollectionItemDto>>(marketType);
                    break;
                
                case nameof(Instrument):
                    var instrument = await context.Instruments.OrderBy(c => c.Name).ToListAsync();
                    result[itemType] = mapper.Map<List<CollectionItemDto>>(instrument);
                    break; 
                
                case nameof(ItemCategory):
                    var itemCategory = await context.ItemCategories.OrderBy(c => c.Name).ToListAsync();
                    result[itemType] = mapper.Map<List<CollectionItemDto>>(itemCategory);
                    break; 

                default:
                    invalidItemTypes.Add(itemType);
                    break;
            }
        }

        if (invalidItemTypes.Count == 0) return Result.Success(result);
        var invalidItems = string.Join(", ", invalidItemTypes);
        return Error.Validation("Item", $"Invalid item types: {invalidItems}");
    }
    
    public async Task<Result<IEnumerable<PackageStyleDto>>> GetPackageStyles()
    {
        return mapper.Map<List<PackageStyleDto>>(await context.PackageStyles.ToListAsync());
    }
    
    public async Task<Result<IEnumerable<DeliveryModeDto>>> GetDeliveryModes()
    {
        return mapper.Map<List<DeliveryModeDto>>(await context.DeliveryModes.ToListAsync());
    }
    
    public async Task<Result<IEnumerable<TermsOfPaymentDto>>> GetTermsOfPayments()
    {
        return mapper.Map<List<TermsOfPaymentDto>>(await context.TermsOfPayments.ToListAsync());
    }

    public Result<IEnumerable<string>> GetItemTypes()
    {
        return new List<string>
        {
            nameof(ProductCategory),
            nameof(Resource),
            nameof(UnitOfMeasure),
            nameof(PackageStyle),
            nameof(TermsOfPayment),
            nameof(DeliveryMode),
            nameof(WorkCenter),
            nameof(Operation),
            nameof(MaterialType),
            nameof(MaterialCategory),
            nameof(ShiftCategory),
            nameof(PackageType),
            nameof(User),
            nameof(Role),
            nameof(Country),
            nameof(WarehouseLocation),
            nameof(ShipmentDiscrepancyType),
            nameof(Charge),
            nameof(ProductState),
            nameof(FinishedGoodsTransferNote),
            nameof(MarketType),
            nameof(Instrument),
            nameof(ItemCategory)
        };
    }
    
    public async Task<Result<Guid>> CreateItem(CreateItemRequest request, string itemType)
    {
        var nameExists = await CheckIfNameExists(itemType, request.Name);
        if (nameExists)
        {
            return Error.Validation("Name", "An item with this name already exists.");
        }

        switch (itemType)
        {
            case nameof(ProductCategory):
                var productCategory = mapper.Map<ProductCategory>(request);
                await context.ProductCategories.AddAsync(productCategory);
                await context.SaveChangesAsync();
                return productCategory.Id;
            
            case nameof(Resource):
                var resource = mapper.Map<Resource>(request);
                await context.Resources.AddAsync(resource);
                await context.SaveChangesAsync();
                return resource.Id;
            
            case nameof(UnitOfMeasure):
                var unitOfMeasure = mapper.Map<UnitOfMeasure>(request);
                await context.UnitOfMeasures.AddAsync(unitOfMeasure);
                await context.SaveChangesAsync();
                return unitOfMeasure.Id;
            
            case nameof(PackageStyle):
                var packageStyle = mapper.Map<PackageStyle>(request);
                await context.PackageStyles.AddAsync(packageStyle);
                await context.SaveChangesAsync();
                return packageStyle.Id;
            
            case nameof(TermsOfPayment):
                var termsOfPayment = mapper.Map<TermsOfPayment>(request);
                await context.TermsOfPayments.AddAsync(termsOfPayment);
                await context.SaveChangesAsync();
                return termsOfPayment.Id;
            
            case nameof(DeliveryMode):
                var deliveryMode = mapper.Map<DeliveryMode>(request);
                await context.DeliveryModes.AddAsync(deliveryMode);
                await context.SaveChangesAsync();
                return deliveryMode.Id;
            
            case nameof(WorkCenter):
                var workCenter = mapper.Map<WorkCenter>(request);
                await context.WorkCenters.AddAsync(workCenter);
                await context.SaveChangesAsync();
                return workCenter.Id;
            
            case nameof(Operation):
                var operation = mapper.Map<Operation>(request);
                await context.Operations.AddAsync(operation);
                await context.SaveChangesAsync();
                return operation.Id;
            
            case nameof(MaterialType):
                var materialType = mapper.Map<MaterialType>(request);
                await context.MaterialTypes.AddAsync(materialType);
                await context.SaveChangesAsync();
                return materialType.Id;
            
            case nameof(MaterialCategory):
                var materialCategory = mapper.Map<MaterialCategory>(request);
                await context.MaterialCategories.AddAsync(materialCategory);
                await context.SaveChangesAsync();
                return materialCategory.Id;
            
            case nameof(ShiftCategory):
                var shiftCategory = mapper.Map<ShiftCategory>(request);
                await context.ShiftCategories.AddAsync(shiftCategory);
                await context.SaveChangesAsync();
                return shiftCategory.Id;
            
            case nameof(PackageType):
                var productPackageType = mapper.Map<PackageType>(request);
                await context.PackageTypes.AddAsync(productPackageType);
                await context.SaveChangesAsync();
                return productPackageType.Id;
            
            case nameof(Currency):
                var currency = mapper.Map<Currency>(request);
                await context.Currencies.AddAsync(currency);
                await context.SaveChangesAsync();
                return currency.Id;
            
            case nameof(ShipmentDiscrepancyType):
                var shipmentDiscrepancyType = mapper.Map<ShipmentDiscrepancyType>(request);
                await context.ShipmentDiscrepancyTypes.AddAsync(shipmentDiscrepancyType);
                await context.SaveChangesAsync();
                return shipmentDiscrepancyType.Id;
            
            case nameof(Charge):
                var charge = mapper.Map<Charge>(request);
                await context.Charges.AddAsync(charge);
                await context.SaveChangesAsync();
                return charge.Id;
            
            case nameof(ProductState):
                var productState = mapper.Map<ProductState>(request);
                await context.ProductStates.AddAsync(productState);
                await context.SaveChangesAsync();
                return productState.Id;
            
            case nameof(MarketType):
                var marketType = mapper.Map<MarketType>(request);
                await context.MarketTypes.AddAsync(marketType);
                await context.SaveChangesAsync();
                return marketType.Id;
            
            case nameof(Instrument):
                var instrument = mapper.Map<Instrument>(request);
                await context.Instruments.AddAsync(instrument);
                await context.SaveChangesAsync();
                return instrument.Id;
            
            case nameof(ItemCategory):
                var itemCategory = mapper.Map<ItemCategory>(request);
                await context.ItemCategories.AddAsync(itemCategory);
                await context.SaveChangesAsync();
                return itemCategory.Id;
            
            default:
                return Error.Validation("Item", "Invalid item type");
        }
    }

    public async Task<Result<Guid>> UpdateItem(CreateItemRequest request, Guid itemId, string itemType, Guid userId)
    {
        var nameExists = await CheckIfNameExists(itemType, request.Name, itemId);
        if (nameExists)
        {
            return Error.Validation("Name", "An item with this name already exists.");
        }

        switch (itemType)
        {
            case nameof(ProductCategory):
                var productCategory = await context.ProductCategories.FirstOrDefaultAsync(p => p.Id == itemId);
                mapper.Map(request, productCategory);
                productCategory.LastUpdatedById = userId;
                context.ProductCategories.Update(productCategory);
                await context.SaveChangesAsync();
                return productCategory.Id;
        
            case nameof(Resource):
                var resource = await context.Resources.FirstOrDefaultAsync(p => p.Id == itemId);
                mapper.Map(request, resource);
                resource.LastUpdatedById = userId;
                context.Resources.Update(resource);
                await context.SaveChangesAsync();
                return resource.Id;
        
            case nameof(UnitOfMeasure):
                var unitOfMeasure = await context.UnitOfMeasures.FirstOrDefaultAsync(p => p.Id == itemId);
                mapper.Map(request, unitOfMeasure);
                unitOfMeasure.LastUpdatedById = userId;
                context.UnitOfMeasures.Update(unitOfMeasure);
                await context.SaveChangesAsync();
                return unitOfMeasure.Id;
            
            case nameof(PackageStyle):
                var packageStyle = await context.PackageStyles.FirstOrDefaultAsync(p => p.Id == itemId);
                mapper.Map(request, packageStyle);
                packageStyle.LastUpdatedById = userId;
                context.PackageStyles.Update(packageStyle);
                await context.SaveChangesAsync();
                return packageStyle.Id;
            
            case nameof(DeliveryMode):
                var deliveryMode = await context.DeliveryModes.FirstOrDefaultAsync(p => p.Id == itemId);
                mapper.Map(request, deliveryMode);
                deliveryMode.LastUpdatedById = userId;
                context.DeliveryModes.Update(deliveryMode);
                await context.SaveChangesAsync();
                return deliveryMode.Id;
            
            case nameof(TermsOfPayment):
                var termsOfPayment = await context.TermsOfPayments.FirstOrDefaultAsync(p => p.Id == itemId);
                mapper.Map(request, termsOfPayment);
                termsOfPayment.LastUpdatedById = userId;
                context.TermsOfPayments.Update(termsOfPayment);
                await context.SaveChangesAsync();
                return termsOfPayment.Id;
        
            case nameof(WorkCenter):
                var workCenter = await context.WorkCenters.FirstOrDefaultAsync(p => p.Id == itemId);
                mapper.Map(request, workCenter);
                workCenter.LastUpdatedById = userId;
                context.WorkCenters.Update(workCenter);
                await context.SaveChangesAsync();
                return workCenter.Id;
        
            case nameof(Operation):
                var operation = await context.Operations.FirstOrDefaultAsync(p => p.Id == itemId);
                mapper.Map(request, operation);
                operation.LastUpdatedById = userId;
                context.Operations.Update(operation);
                await context.SaveChangesAsync();
                return operation.Id;
            
            case nameof(MaterialType):
                var materialType = await context.MaterialTypes.FirstOrDefaultAsync(p => p.Id == itemId);
                mapper.Map(request, materialType);
                materialType.LastUpdatedById = userId;
                context.MaterialTypes.Update(materialType);
                await context.SaveChangesAsync();
                return materialType.Id;
            
            case nameof(MaterialCategory):
                var materialCategory = await context.MaterialCategories.FirstOrDefaultAsync(p => p.Id == itemId);
                mapper.Map(request, materialCategory);
                materialCategory.LastUpdatedById = userId;
                context.MaterialCategories.Update(materialCategory);
                await context.SaveChangesAsync();
                return materialCategory.Id;
            
            case nameof(ShiftCategory):
                var shiftCategory = await context.ShiftCategories.FirstOrDefaultAsync(p => p.Id == itemId && p.LastDeletedById == null);
                mapper.Map(request, shiftCategory);
                shiftCategory.LastUpdatedById = userId;
                context.ShiftCategories.Update(shiftCategory);
                await context.SaveChangesAsync();
                return shiftCategory.Id;
            
            case nameof(PackageType):
                var productPackageType = await context.PackageTypes.FirstOrDefaultAsync(p => p.Id == itemId);
                mapper.Map(request, productPackageType);
                productPackageType.LastUpdatedById = userId;
                context.PackageTypes.Update(productPackageType);
                await context.SaveChangesAsync();
                return productPackageType.Id;
            
            case nameof(Currency):
                var currency = await context.Currencies.FirstOrDefaultAsync(p => p.Id == itemId);
                mapper.Map(request, currency);
                currency.LastUpdatedById = userId;
                context.Currencies.Update(currency);
                await context.SaveChangesAsync();
                return currency.Id;
            
            case nameof(ShipmentDiscrepancyType):
                var shipmentDiscrepancyType = await context.ShipmentDiscrepancyTypes.FirstOrDefaultAsync(p => p.Id == itemId);
                mapper.Map(request, shipmentDiscrepancyType);
                shipmentDiscrepancyType.LastUpdatedById = userId;
                context.ShipmentDiscrepancyTypes.Update(shipmentDiscrepancyType);
                await context.SaveChangesAsync();
                return shipmentDiscrepancyType.Id;
            
            case nameof(Charge):
                var charge = await context.Charges.FirstOrDefaultAsync(p => p.Id == itemId);
                mapper.Map(request, charge);
                context.Charges.Update(charge);
                await context.SaveChangesAsync();
                return charge.Id;
            
            case nameof(ProductState):
                var productState = await context.ProductStates.FirstOrDefaultAsync(p => p.Id == itemId);
                mapper.Map(request, productState);
                context.ProductStates.Update(productState);
                await context.SaveChangesAsync();
                return productState.Id;
            
            case nameof(MarketType):
                var marketType = await context.MarketTypes.FirstOrDefaultAsync(p => p.Id == itemId);
                mapper.Map(request, marketType);
                context.MarketTypes.Update(marketType);
                await context.SaveChangesAsync();
                return marketType.Id;
            
            case nameof(Instrument):
                var instrument = await context.Instruments.FirstOrDefaultAsync(p => p.Id == itemId);
                mapper.Map(request, instrument);
                context.Instruments.Update(instrument);
                await context.SaveChangesAsync();
                return instrument.Id;
            
            case nameof(ItemCategory):
                var itemCategory = await context.ItemCategories.FirstOrDefaultAsync(p => p.Id == itemId);
                mapper.Map(request, itemCategory);
                context.ItemCategories.Update(itemCategory);
                await context.SaveChangesAsync();
                return itemCategory.Id;
        
            default:
                return Error.Validation("Item", "Invalid item type");
        }
    }

    // Helper Method to Check for Duplicate Names
    private async Task<bool> CheckIfNameExists(string itemType, string name, Guid? excludedId = null)
    {
        return itemType switch
        {
            nameof(ProductCategory) => await context.ProductCategories.AnyAsync(p => p.Name == name && (!excludedId.HasValue || p.Id != excludedId.Value)),
            nameof(Resource) => await context.Resources.AnyAsync(p => p.Name == name && (!excludedId.HasValue || p.Id != excludedId.Value)),
            nameof(UnitOfMeasure) => await context.UnitOfMeasures.AnyAsync(p => p.Name == name && (!excludedId.HasValue || p.Id != excludedId.Value)),
            nameof(PackageStyle) => await context.PackageStyles.AnyAsync(p => p.Name == name && (!excludedId.HasValue || p.Id != excludedId.Value)),
            nameof(DeliveryMode) => await context.DeliveryModes.AnyAsync(p => p.Name == name && (!excludedId.HasValue || p.Id != excludedId.Value)),
            nameof(TermsOfPayment) => await context.TermsOfPayments.AnyAsync(p => p.Name == name && (!excludedId.HasValue || p.Id != excludedId.Value)),
            nameof(WorkCenter) => await context.WorkCenters.AnyAsync(p => p.Name == name && (!excludedId.HasValue || p.Id != excludedId.Value)),
            nameof(Operation) => await context.Operations.AnyAsync(p => p.Name == name && (!excludedId.HasValue || p.Id != excludedId.Value)),
            nameof(MaterialType) => await context.MaterialTypes.AnyAsync(p => p.Name == name && (!excludedId.HasValue || p.Id != excludedId.Value)),
            nameof(MaterialCategory) => await context.MaterialCategories.AnyAsync(p => p.Name == name && (!excludedId.HasValue || p.Id != excludedId.Value)),
            nameof(ShiftCategory) => await context.ShiftCategories.AnyAsync(p => p.Name == name && (!excludedId.HasValue || p.Id != excludedId.Value)),
            nameof(PackageType) => await context.PackageTypes.AnyAsync(p => p.Name == name && (!excludedId.HasValue || p.Id != excludedId.Value)),
            nameof(Currency) => await context.Currencies.AnyAsync(p => p.Name == name && (!excludedId.HasValue || p.Id != excludedId.Value)),
            nameof(ShipmentDiscrepancyType) => await context.ShipmentDiscrepancyTypes.AnyAsync(p => p.Name == name && (!excludedId.HasValue || p.Id != excludedId.Value)),
            nameof(Charge) => await context.Charges.AnyAsync(p => p.Name == name && (!excludedId.HasValue || p.Id != excludedId.Value)),
            nameof(ProductState) => await context.ProductStates.AnyAsync(p => p.Name == name && (!excludedId.HasValue || p.Id != excludedId.Value)),
            nameof(MarketType) => await context.MarketTypes.AnyAsync(p => p.Name == name && (!excludedId.HasValue || p.Id != excludedId.Value)),
            nameof(Instrument) => await context.Instruments.AnyAsync(p => p.Name == name && (!excludedId.HasValue || p.Id != excludedId.Value)),
            nameof(ItemCategory) => await context.ItemCategories.AnyAsync(p => p.Name == name && (!excludedId.HasValue || p.Id != excludedId.Value)),
            _ => false
        };
    }
    
    public async Task<Result> SoftDeleteItem(Guid itemId, string itemType, Guid userId)
    {
        var currentTime = DateTime.UtcNow;  // or use DateTime.Now based on your timezone requirements

        switch (itemType)
        {
            case nameof(ProductCategory):
                var productCategory = await context.ProductCategories.FirstOrDefaultAsync(p => p.Id == itemId);
                if (productCategory == null)
                    return Error.Validation("ProductCategory", "Not found");
                productCategory.DeletedAt = currentTime;
                productCategory.LastDeletedById = userId;
                context.ProductCategories.Update(productCategory);
                await context.SaveChangesAsync();
                return Result.Success();
            
            case nameof(Resource):
                var resource = await context.Resources.FirstOrDefaultAsync(p => p.Id == itemId);
                if (resource == null)
                    return Error.Validation("Resource", "Not found");
                resource.DeletedAt = currentTime;
                resource.LastDeletedById = userId;
                context.Resources.Update(resource);
                await context.SaveChangesAsync();
                return Result.Success();
            
            case nameof(UnitOfMeasure):
                var unitOfMeasure = await context.UnitOfMeasures.FirstOrDefaultAsync(p => p.Id == itemId);
                if (unitOfMeasure == null)
                    return Error.Validation("UnitOfMeasure", "Not found");
                unitOfMeasure.DeletedAt = currentTime;
                unitOfMeasure.LastDeletedById = userId;
                context.UnitOfMeasures.Update(unitOfMeasure);
                await context.SaveChangesAsync();
                return Result.Success();
            
            case nameof(PackageStyle):
                var packageStyle = await context.PackageStyles.FirstOrDefaultAsync(p => p.Id == itemId);
                if (packageStyle == null)
                    return Error.Validation("PackageStyle", "Not found");
                packageStyle.DeletedAt = currentTime;
                packageStyle.LastDeletedById = userId;
                context.PackageStyles.Update(packageStyle);
                await context.SaveChangesAsync();
                return Result.Success();
            
            case nameof(DeliveryMode):
                var deliveryMode = await context.DeliveryModes.FirstOrDefaultAsync(p => p.Id == itemId);
                if (deliveryMode == null)
                    return Error.Validation("DeliveryMode", "Not found");
                deliveryMode.DeletedAt = currentTime;
                deliveryMode.LastDeletedById = userId;
                context.DeliveryModes.Update(deliveryMode);
                await context.SaveChangesAsync();
                return Result.Success();
            
            case nameof(TermsOfPayment):
                var termsOfPayment = await context.TermsOfPayments.FirstOrDefaultAsync(p => p.Id == itemId);
                if (termsOfPayment == null)
                    return Error.Validation("TermsOfPayment", "Not found");
                termsOfPayment.DeletedAt = currentTime;
                termsOfPayment.LastDeletedById = userId;
                context.TermsOfPayments.Update(termsOfPayment);
                await context.SaveChangesAsync();
                return Result.Success();
            
            case nameof(WorkCenter):
                var workCenter = await context.WorkCenters.FirstOrDefaultAsync(p => p.Id == itemId);
                if (workCenter == null)
                    return Error.Validation("WorkCenter", "Not found");
                workCenter.DeletedAt = currentTime;
                workCenter.LastDeletedById = userId;
                context.WorkCenters.Update(workCenter);
                await context.SaveChangesAsync();
                return Result.Success();
            
            case nameof(Operation):
                var operation = await context.Operations.FirstOrDefaultAsync(p => p.Id == itemId);
                if (operation == null)
                    return Error.Validation("Operation", "Not found");
                operation.DeletedAt = currentTime;
                operation.LastDeletedById = userId;
                context.Operations.Update(operation);
                await context.SaveChangesAsync();
                return Result.Success();
            
            case nameof(MaterialType):
                var materialType = await context.MaterialTypes.FirstOrDefaultAsync(p => p.Id == itemId);
                if (materialType == null)
                    return Error.Validation("MaterialType", "Not found");
                materialType.DeletedAt = currentTime;
                materialType.LastDeletedById = userId;
                context.MaterialTypes.Update(materialType);
                await context.SaveChangesAsync();
                return Result.Success();
            
            case nameof(MaterialCategory):
                var materialCategory = await context.MaterialCategories.FirstOrDefaultAsync(p => p.Id == itemId);
                if (materialCategory == null)
                    return Error.Validation("MaterialCategory", "Not found");
                materialCategory.DeletedAt = currentTime;
                materialCategory.LastDeletedById = userId;
                context.MaterialCategories.Update(materialCategory);
                await context.SaveChangesAsync();
                return Result.Success();
            
            case nameof(ShiftCategory):
                var shiftCategory =  await context.ShiftCategories.FirstOrDefaultAsync(p => p.Id == itemId && p.LastDeletedById == null);
                if (shiftCategory == null)
                    return Error.Validation("ShiftCategory", "Not found");
                shiftCategory.DeletedAt = currentTime;
                shiftCategory.LastDeletedById = userId;
                context.ShiftCategories.Update(shiftCategory);
                await context.SaveChangesAsync();
                return Result.Success();
            
            case nameof(PackageType):
                var productPackageType = await context.PackageTypes.FirstOrDefaultAsync(p => p.Id == itemId);
                if (productPackageType == null)
                    return Error.Validation("MaterialCategory", "Not found");
                productPackageType.DeletedAt = currentTime;
                productPackageType.LastDeletedById = userId;
                context.PackageTypes.Update(productPackageType);
                await context.SaveChangesAsync();
                return Result.Success();
            
            case nameof(Currency):
                var currency = await context.Currencies.FirstOrDefaultAsync(p => p.Id == itemId);
                if (currency == null)
                    return Error.Validation("Currency", "Not found");
                currency.DeletedAt = currentTime;
                currency.LastDeletedById = userId;
                context.Currencies.Update(currency);
                await context.SaveChangesAsync();
                return Result.Success();
            
            case nameof(ShipmentDiscrepancyType):
                var shipmentDiscrepancyType = await context.ShipmentDiscrepancyTypes.FirstOrDefaultAsync(p => p.Id == itemId);
                if (shipmentDiscrepancyType == null)
                    return Error.Validation("ShipmentDiscrepancy", "Not found");
                shipmentDiscrepancyType.DeletedAt = currentTime;
                shipmentDiscrepancyType.LastDeletedById = userId;
                context.ShipmentDiscrepancyTypes.Update(shipmentDiscrepancyType);
                await context.SaveChangesAsync();
                return Result.Success();
            
            case nameof(Charge):
                var charge = await context.Charges.FirstOrDefaultAsync(p => p.Id == itemId);
                if (charge == null)
                    return Error.Validation("Charge", "Not found");
                charge.DeletedAt = currentTime;
                charge.LastDeletedById = userId;
                context.Charges.Update(charge);
                await context.SaveChangesAsync();
                return Result.Success();
            
            case nameof(ProductState):
                var productState = await context.ProductStates.FirstOrDefaultAsync(p => p.Id == itemId);
                if (productState == null)
                    return Error.Validation("Charge", "Not found");
                productState.DeletedAt = currentTime;
                productState.LastDeletedById = userId;
                context.ProductStates.Update(productState);
                await context.SaveChangesAsync();
                return Result.Success();
            
            case nameof(MarketType):
                var marketType = await context.MarketTypes.FirstOrDefaultAsync(p => p.Id == itemId);
                if (marketType == null)
                    return Error.Validation("Charge", "Not found");
                marketType.DeletedAt = currentTime;
                marketType.LastDeletedById = userId;
                context.MarketTypes.Update(marketType);
                await context.SaveChangesAsync();
                return Result.Success();
            
            case nameof(Instrument):
                var instrument = await context.Instruments.FirstOrDefaultAsync(p => p.Id == itemId);
                if (instrument == null)
                    return Error.Validation("Charge", "Not found");
                instrument.DeletedAt = currentTime;
                instrument.LastDeletedById = userId;
                context.Instruments.Update(instrument);
                await context.SaveChangesAsync();
                return Result.Success();  
            
            case nameof(ItemCategory):
                var itemCategory = await context.ItemCategories.FirstOrDefaultAsync(p => p.Id == itemId);
                if (itemCategory == null)
                    return Error.Validation("Charge", "Not found");
                itemCategory.DeletedAt = currentTime;
                itemCategory.LastDeletedById = userId;
                context.ItemCategories.Update(itemCategory);
                await context.SaveChangesAsync();
                return Result.Success();  
            
            default:
                return Error.Validation("Item", "Invalid item type");
        }    
    }

    public async Task<Result> CreateUoM(CreateUnitOfMeasure request)
    {
        var uom = mapper.Map<UnitOfMeasure>(request);
        await context.UnitOfMeasures.AddAsync(uom);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<Paginateable<IEnumerable<UnitOfMeasureDto>>>> GetUoM(FilterUnitOfMeasure filter)
    {
        var query = context.UnitOfMeasures.
            AsQueryable();

        if (string.IsNullOrEmpty(filter.SearchQuery))
        {
            query = query.WhereSearch(filter.SearchQuery, q => q.Name, q => q.Description);
        }

        if (filter.Types.Count != 0)
        {
            query = query.Where(q => filter.Types.Contains(q.Type));
        }

        if (filter.Categories.Count != 0)
        {
            query = query.Where(q => filter.Categories.Contains(q.Category));
        }
        
        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            filter,
            mapper.Map<UnitOfMeasureDto>
        );
    }
    
    public async Task<Result<UnitOfMeasureDto>> GetUoM(Guid uomId)
    {
       return mapper.Map<UnitOfMeasureDto>(
           await context.UnitOfMeasures.FirstOrDefaultAsync(p => p.Id == uomId));
    }
    
    
    public async Task<Result> UpdateUoM(CreateUnitOfMeasure request, Guid id)
    {
        var uom = await context.UnitOfMeasures.
            FirstOrDefaultAsync(u => u.Id == id);
        if(uom is null) return Error.NotFound("Uom", "Uom not found");
        
        mapper.Map(request, uom);
        
        context.UnitOfMeasures.Update(uom);
        await context.SaveChangesAsync();
        return Result.Success();
    }
    
    
    public async Task<Result> DeleteUoM(Guid uomId)
    {
        var uom = await context.UnitOfMeasures.
            FirstOrDefaultAsync(u => u.Id == uomId);
        if(uom is null) return Error.NotFound("Uom", "Uom not found");
        
        uom.DeletedAt = DateTime.UtcNow;
        context.UnitOfMeasures.Update(uom);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}