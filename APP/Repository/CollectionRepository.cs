using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using AutoMapper.QueryableExtensions;
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
using DOMAIN.Entities.Products.Equipments;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.ShiftAssignments;
using DOMAIN.Entities.Shipments;
using DOMAIN.Entities.Sites;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class CollectionRepository(ApplicationDbContext context, IMapper mapper)
    : ICollectionRepository
{
    public async Task<Result<IEnumerable<CollectionItemDto>>> GetItemCollection(
        string itemType,
        MaterialKind? materialKind
    )
    {
        var query = GetItemCollectionQuery(itemType, materialKind);

        if (query == null)
        {
            return Error.Validation("Item", "Invalid item type");
        }

        var items = await query.ToListAsync();
        return Result.Success(items.AsEnumerable());
    }

    private IQueryable<CollectionItemDto> GetItemCollectionQuery(
        string itemType,
        MaterialKind? materialKind = null
    )
    {
        return itemType switch
        {
            nameof(ProductCategory) => context
                .ProductCategories.AsNoTracking()
                .ProjectTo<CollectionItemDto>(mapper.ConfigurationProvider),
            nameof(Resource) => context
                .Resources.AsNoTracking()
                .ProjectTo<CollectionItemDto>(mapper.ConfigurationProvider),
            nameof(UnitOfMeasure) => context
                .UnitOfMeasures.AsNoTracking()
                .ProjectTo<CollectionItemDto>(mapper.ConfigurationProvider),
            nameof(PackageStyle) => context
                .PackageStyles.AsNoTracking()
                .ProjectTo<CollectionItemDto>(mapper.ConfigurationProvider),
            nameof(DeliveryMode) => context
                .DeliveryModes.AsNoTracking()
                .ProjectTo<CollectionItemDto>(mapper.ConfigurationProvider),
            nameof(TermsOfPayment) => context
                .TermsOfPayments.AsNoTracking()
                .ProjectTo<CollectionItemDto>(mapper.ConfigurationProvider),
            nameof(WorkCenter) => context
                .WorkCenters.AsNoTracking()
                .ProjectTo<CollectionItemDto>(mapper.ConfigurationProvider),
            nameof(Operation) => context
                .Operations.AsNoTracking()
                .ProjectTo<CollectionItemDto>(mapper.ConfigurationProvider),
            nameof(MaterialType) => context
                .MaterialTypes.AsNoTracking()
                .ProjectTo<CollectionItemDto>(mapper.ConfigurationProvider),
            nameof(MaterialCategory) => GetMaterialCategoriesQuery(materialKind),
            nameof(ShiftCategory) => context
                .ShiftCategories.AsNoTracking()
                .Where(sc => sc.DeletedAt == null)
                .ProjectTo<CollectionItemDto>(mapper.ConfigurationProvider),
            nameof(PackageType) => context
                .PackageTypes.AsNoTracking()
                .ProjectTo<CollectionItemDto>(mapper.ConfigurationProvider),
            nameof(User) => context
                .Users.AsNoTracking()
                .ProjectTo<CollectionItemDto>(mapper.ConfigurationProvider),
            nameof(Role) => context
                .Roles.AsNoTracking()
                .ProjectTo<CollectionItemDto>(mapper.ConfigurationProvider),
            nameof(Country) => context
                .Countries.AsNoTracking()
                .OrderBy(c => c.Name)
                .ProjectTo<CollectionItemDto>(mapper.ConfigurationProvider),
            nameof(WarehouseLocation) => context
                .WarehouseLocations.AsNoTracking()
                .OrderBy(c => c.Name)
                .ProjectTo<CollectionItemDto>(mapper.ConfigurationProvider),
            nameof(Warehouse) => context
                .Warehouses.AsNoTracking()
                .OrderBy(c => c.Name)
                .ProjectTo<CollectionItemDto>(mapper.ConfigurationProvider),
            nameof(WarehouseLocationRack) => context
                .WarehouseLocationRacks.AsNoTracking()
                .OrderBy(c => c.Name)
                .ProjectTo<CollectionItemDto>(mapper.ConfigurationProvider),
            nameof(WarehouseLocationShelf) => context
                .WarehouseLocationShelves.AsNoTracking()
                .OrderBy(c => c.Name)
                .ProjectTo<CollectionItemDto>(mapper.ConfigurationProvider),
            nameof(Currency) => context
                .Currencies.AsNoTracking()
                .OrderBy(c => c.Name)
                .ProjectTo<CollectionItemDto>(mapper.ConfigurationProvider),
            nameof(ShipmentDiscrepancyType) => context
                .ShipmentDiscrepancyTypes.AsNoTracking()
                .OrderBy(c => c.Name)
                .ProjectTo<CollectionItemDto>(mapper.ConfigurationProvider),
            nameof(Department) => context
                .Departments.AsNoTracking()
                .OrderBy(c => c.Name)
                .ProjectTo<CollectionItemDto>(mapper.ConfigurationProvider),
            nameof(Charge) => context
                .Charges.AsNoTracking()
                .OrderBy(c => c.Name)
                .ProjectTo<CollectionItemDto>(mapper.ConfigurationProvider),
            nameof(ProductState) => context
                .ProductStates.AsNoTracking()
                .OrderBy(c => c.Name)
                .ProjectTo<CollectionItemDto>(mapper.ConfigurationProvider),
            nameof(FinishedGoodsTransferNote) => context
                .FinishedGoodsTransferNotes.AsNoTracking()
                .OrderBy(c => c.TransferNoteNumber)
                .ProjectTo<CollectionItemDto>(mapper.ConfigurationProvider),
            nameof(MarketType) => context
                .MarketTypes.AsNoTracking()
                .OrderBy(c => c.Name)
                .ProjectTo<CollectionItemDto>(mapper.ConfigurationProvider),
            nameof(Instrument) => context
                .Instruments.AsNoTracking()
                .OrderBy(c => c.Name)
                .ProjectTo<CollectionItemDto>(mapper.ConfigurationProvider),
            nameof(ItemCategory) => context
                .ItemCategories.AsNoTracking()
                .OrderBy(c => c.Name)
                .ProjectTo<CollectionItemDto>(mapper.ConfigurationProvider),
            nameof(WarehouseLocationName) => context
                .WarehouseLocationNames.AsNoTracking()
                .OrderBy(c => c.Name)
                .ProjectTo<CollectionItemDto>(mapper.ConfigurationProvider),
            nameof(QcEquipmentCategory) => context
                .QcEquipmentCategories.AsNoTracking()
                .OrderBy(c => c.Name)
                .ProjectTo<CollectionItemDto>(mapper.ConfigurationProvider),
            nameof(Reagent) => context
                .Reagents.AsNoTracking()
                .OrderBy(c => c.Name)
                .ProjectTo<CollectionItemDto>(mapper.ConfigurationProvider),
            nameof(Site) => context
                .Sites.AsNoTracking()
                .OrderBy(c => c.Name)
                .ProjectTo<CollectionItemDto>(mapper.ConfigurationProvider),
            _ => null,
        };
    }

    private IQueryable<CollectionItemDto> GetMaterialCategoriesQuery(MaterialKind? materialKind)
    {
        var query = context.MaterialCategories.AsNoTracking();
        if (materialKind != null)
        {
            query = query.Where(m => m.MaterialKind == materialKind);
        }
        return query.ProjectTo<CollectionItemDto>(mapper.ConfigurationProvider);
    }

    public async Task<Result<Dictionary<string, IEnumerable<CollectionItemDto>>>> GetItemCollection(
        List<string> itemTypes,
        MaterialKind? materialKind = null
    )
    {
        var result = new Dictionary<string, IEnumerable<CollectionItemDto>>();
        var invalidItemTypes = new List<string>();

        foreach (var itemType in itemTypes)
        {
            var query = GetItemCollectionQuery(itemType, materialKind);
            if (query != null)
            {
                result[itemType] = await query.ToListAsync();
            }
            else
            {
                invalidItemTypes.Add(itemType);
            }
        }

        if (invalidItemTypes.Count == 0)
            return Result.Success(result);
        var invalidItems = string.Join(", ", invalidItemTypes);
        return Error.Validation("Item", $"Invalid item types: {invalidItems}");
    }

    public async Task<Result<IEnumerable<PackageStyleDto>>> GetPackageStyles()
    {
        var items = await context
            .PackageStyles.AsNoTracking()
            .ProjectTo<PackageStyleDto>(mapper.ConfigurationProvider)
            .ToListAsync();
        return Result.Success(items.AsEnumerable());
    }

    public async Task<Result<IEnumerable<DeliveryModeDto>>> GetDeliveryModes()
    {
        var items = await context
            .DeliveryModes.AsNoTracking()
            .ProjectTo<DeliveryModeDto>(mapper.ConfigurationProvider)
            .ToListAsync();
        return Result.Success(items.AsEnumerable());
    }

    public async Task<Result<IEnumerable<TermsOfPaymentDto>>> GetTermsOfPayments()
    {
        var items = await context
            .TermsOfPayments.AsNoTracking()
            .ProjectTo<TermsOfPaymentDto>(mapper.ConfigurationProvider)
            .ToListAsync();
        return Result.Success(items.AsEnumerable());
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
            nameof(ItemCategory),
            nameof(WarehouseLocationName),
            nameof(QcEquipmentCategory),
            nameof(Reagent),
            nameof(Site),
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

            case nameof(WarehouseLocationName):
                var warehouseLocationName = mapper.Map<WarehouseLocationName>(request);
                await context.WarehouseLocationNames.AddAsync(warehouseLocationName);
                await context.SaveChangesAsync();
                return warehouseLocationName.Id;

            case nameof(QcEquipmentCategory):
                var qcEquipmentCategory = mapper.Map<QcEquipmentCategory>(request);
                await context.QcEquipmentCategories.AddAsync(qcEquipmentCategory);
                await context.SaveChangesAsync();
                return qcEquipmentCategory.Id;

            case nameof(Reagent):
                var reagent = mapper.Map<Reagent>(request);
                await context.Reagents.AddAsync(reagent);
                await context.SaveChangesAsync();
                return reagent.Id;

            case nameof(Site):
                var site = mapper.Map<Site>(request);
                await context.Sites.AddAsync(site);
                await context.SaveChangesAsync();
                return site.Id;

            default:
                return Error.Validation("Item", "Invalid item type");
        }
    }

    public async Task<Result<Guid>> UpdateItem(
        CreateItemRequest request,
        Guid itemId,
        string itemType,
        Guid userId
    )
    {
        var nameExists = await CheckIfNameExists(itemType, request.Name, itemId);
        if (nameExists)
        {
            return Error.Validation("Name", "An item with this name already exists.");
        }

        switch (itemType)
        {
            case nameof(ProductCategory):
                var productCategory = await context.ProductCategories.FirstOrDefaultAsync(p =>
                    p.Id == itemId
                );
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
                var unitOfMeasure = await context.UnitOfMeasures.FirstOrDefaultAsync(p =>
                    p.Id == itemId
                );
                mapper.Map(request, unitOfMeasure);
                unitOfMeasure.LastUpdatedById = userId;
                context.UnitOfMeasures.Update(unitOfMeasure);
                await context.SaveChangesAsync();
                return unitOfMeasure.Id;

            case nameof(PackageStyle):
                var packageStyle = await context.PackageStyles.FirstOrDefaultAsync(p =>
                    p.Id == itemId
                );
                mapper.Map(request, packageStyle);
                packageStyle.LastUpdatedById = userId;
                context.PackageStyles.Update(packageStyle);
                await context.SaveChangesAsync();
                return packageStyle.Id;

            case nameof(DeliveryMode):
                var deliveryMode = await context.DeliveryModes.FirstOrDefaultAsync(p =>
                    p.Id == itemId
                );
                mapper.Map(request, deliveryMode);
                deliveryMode.LastUpdatedById = userId;
                context.DeliveryModes.Update(deliveryMode);
                await context.SaveChangesAsync();
                return deliveryMode.Id;

            case nameof(TermsOfPayment):
                var termsOfPayment = await context.TermsOfPayments.FirstOrDefaultAsync(p =>
                    p.Id == itemId
                );
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
                var materialType = await context.MaterialTypes.FirstOrDefaultAsync(p =>
                    p.Id == itemId
                );
                mapper.Map(request, materialType);
                materialType.LastUpdatedById = userId;
                context.MaterialTypes.Update(materialType);
                await context.SaveChangesAsync();
                return materialType.Id;

            case nameof(MaterialCategory):
                var materialCategory = await context.MaterialCategories.FirstOrDefaultAsync(p =>
                    p.Id == itemId
                );
                mapper.Map(request, materialCategory);
                materialCategory.LastUpdatedById = userId;
                context.MaterialCategories.Update(materialCategory);
                await context.SaveChangesAsync();
                return materialCategory.Id;

            case nameof(ShiftCategory):
                var shiftCategory = await context.ShiftCategories.FirstOrDefaultAsync(p =>
                    p.Id == itemId && p.LastDeletedById == null
                );
                mapper.Map(request, shiftCategory);
                shiftCategory.LastUpdatedById = userId;
                context.ShiftCategories.Update(shiftCategory);
                await context.SaveChangesAsync();
                return shiftCategory.Id;

            case nameof(PackageType):
                var productPackageType = await context.PackageTypes.FirstOrDefaultAsync(p =>
                    p.Id == itemId
                );
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
                var shipmentDiscrepancyType =
                    await context.ShipmentDiscrepancyTypes.FirstOrDefaultAsync(p => p.Id == itemId);
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
                var productState = await context.ProductStates.FirstOrDefaultAsync(p =>
                    p.Id == itemId
                );
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
                var itemCategory = await context.ItemCategories.FirstOrDefaultAsync(p =>
                    p.Id == itemId
                );
                mapper.Map(request, itemCategory);
                context.ItemCategories.Update(itemCategory);
                await context.SaveChangesAsync();
                return itemCategory.Id;

            case nameof(WarehouseLocationName):
                var warehouseLocationName =
                    await context.WarehouseLocationNames.FirstOrDefaultAsync(p => p.Id == itemId);
                mapper.Map(request, warehouseLocationName);
                context.WarehouseLocationNames.Update(warehouseLocationName);
                await context.SaveChangesAsync();
                return warehouseLocationName.Id;

            case nameof(QcEquipmentCategory):
                var qcEquipmentCategory = await context.QcEquipmentCategories.FirstOrDefaultAsync(
                    p => p.Id == itemId
                );
                mapper.Map(request, qcEquipmentCategory);
                context.QcEquipmentCategories.Update(qcEquipmentCategory);
                await context.SaveChangesAsync();
                return qcEquipmentCategory.Id;

            case nameof(Reagent):
                var reagent = await context.Reagents.FirstOrDefaultAsync(p => p.Id == itemId);
                mapper.Map(request, reagent);
                context.Reagents.Update(reagent);
                await context.SaveChangesAsync();
                return reagent.Id;

            case nameof(Site):
                var site = await context.Sites.FirstOrDefaultAsync(p => p.Id == itemId);
                mapper.Map(request, site);
                context.Sites.Update(site);
                await context.SaveChangesAsync();
                return site.Id;

            default:
                return Error.Validation("Item", "Invalid item type");
        }
    }

    // Helper Method to Check for Duplicate Names
    private async Task<bool> CheckIfNameExists(
        string itemType,
        string name,
        Guid? excludedId = null
    )
    {
        return itemType switch
        {
            nameof(ProductCategory) => await context.ProductCategories.AnyAsync(p =>
                p.Name == name && (!excludedId.HasValue || p.Id != excludedId.Value)
            ),
            nameof(Resource) => await context.Resources.AnyAsync(p =>
                p.Name == name && (!excludedId.HasValue || p.Id != excludedId.Value)
            ),
            nameof(UnitOfMeasure) => await context.UnitOfMeasures.AnyAsync(p =>
                p.Name == name && (!excludedId.HasValue || p.Id != excludedId.Value)
            ),
            nameof(PackageStyle) => await context.PackageStyles.AnyAsync(p =>
                p.Name == name && (!excludedId.HasValue || p.Id != excludedId.Value)
            ),
            nameof(DeliveryMode) => await context.DeliveryModes.AnyAsync(p =>
                p.Name == name && (!excludedId.HasValue || p.Id != excludedId.Value)
            ),
            nameof(TermsOfPayment) => await context.TermsOfPayments.AnyAsync(p =>
                p.Name == name && (!excludedId.HasValue || p.Id != excludedId.Value)
            ),
            nameof(WorkCenter) => await context.WorkCenters.AnyAsync(p =>
                p.Name == name && (!excludedId.HasValue || p.Id != excludedId.Value)
            ),
            nameof(Operation) => await context.Operations.AnyAsync(p =>
                p.Name == name && (!excludedId.HasValue || p.Id != excludedId.Value)
            ),
            nameof(MaterialType) => await context.MaterialTypes.AnyAsync(p =>
                p.Name == name && (!excludedId.HasValue || p.Id != excludedId.Value)
            ),
            nameof(MaterialCategory) => await context.MaterialCategories.AnyAsync(p =>
                p.Name == name && (!excludedId.HasValue || p.Id != excludedId.Value)
            ),
            nameof(ShiftCategory) => await context.ShiftCategories.AnyAsync(p =>
                p.Name == name && (!excludedId.HasValue || p.Id != excludedId.Value)
            ),
            nameof(PackageType) => await context.PackageTypes.AnyAsync(p =>
                p.Name == name && (!excludedId.HasValue || p.Id != excludedId.Value)
            ),
            nameof(Currency) => await context.Currencies.AnyAsync(p =>
                p.Name == name && (!excludedId.HasValue || p.Id != excludedId.Value)
            ),
            nameof(ShipmentDiscrepancyType) => await context.ShipmentDiscrepancyTypes.AnyAsync(p =>
                p.Name == name && (!excludedId.HasValue || p.Id != excludedId.Value)
            ),
            nameof(Charge) => await context.Charges.AnyAsync(p =>
                p.Name == name && (!excludedId.HasValue || p.Id != excludedId.Value)
            ),
            nameof(ProductState) => await context.ProductStates.AnyAsync(p =>
                p.Name == name && (!excludedId.HasValue || p.Id != excludedId.Value)
            ),
            nameof(MarketType) => await context.MarketTypes.AnyAsync(p =>
                p.Name == name && (!excludedId.HasValue || p.Id != excludedId.Value)
            ),
            nameof(Instrument) => await context.Instruments.AnyAsync(p =>
                p.Name == name && (!excludedId.HasValue || p.Id != excludedId.Value)
            ),
            nameof(ItemCategory) => await context.ItemCategories.AnyAsync(p =>
                p.Name == name && (!excludedId.HasValue || p.Id != excludedId.Value)
            ),
            nameof(WarehouseLocationName) => await context.WarehouseLocationNames.AnyAsync(p =>
                p.Name == name && (!excludedId.HasValue || p.Id != excludedId.Value)
            ),
            nameof(QcEquipmentCategory) => await context.QcEquipmentCategories.AnyAsync(p =>
                p.Name == name && (!excludedId.HasValue || p.Id != excludedId.Value)
            ),
            nameof(Reagent) => await context.Reagents.AnyAsync(p =>
                p.Name == name && (!excludedId.HasValue || p.Id != excludedId.Value)
            ),
            nameof(Site) => await context.Sites.AnyAsync(p =>
                p.Name == name && (!excludedId.HasValue || p.Id != excludedId.Value)
            ),
            _ => false,
        };
    }

    public async Task<Result> SoftDeleteItem(Guid itemId, string itemType, Guid userId)
    {
        var currentTime = DateTime.UtcNow; // or use DateTime.Now based on your timezone requirements

        switch (itemType)
        {
            case nameof(ProductCategory):
                var productCategory = await context.ProductCategories.FirstOrDefaultAsync(p =>
                    p.Id == itemId
                );
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
                var unitOfMeasure = await context.UnitOfMeasures.FirstOrDefaultAsync(p =>
                    p.Id == itemId
                );
                if (unitOfMeasure == null)
                    return Error.Validation("UnitOfMeasure", "Not found");
                unitOfMeasure.DeletedAt = currentTime;
                unitOfMeasure.LastDeletedById = userId;
                context.UnitOfMeasures.Update(unitOfMeasure);
                await context.SaveChangesAsync();
                return Result.Success();

            case nameof(PackageStyle):
                var packageStyle = await context.PackageStyles.FirstOrDefaultAsync(p =>
                    p.Id == itemId
                );
                if (packageStyle == null)
                    return Error.Validation("PackageStyle", "Not found");
                packageStyle.DeletedAt = currentTime;
                packageStyle.LastDeletedById = userId;
                context.PackageStyles.Update(packageStyle);
                await context.SaveChangesAsync();
                return Result.Success();

            case nameof(DeliveryMode):
                var deliveryMode = await context.DeliveryModes.FirstOrDefaultAsync(p =>
                    p.Id == itemId
                );
                if (deliveryMode == null)
                    return Error.Validation("DeliveryMode", "Not found");
                deliveryMode.DeletedAt = currentTime;
                deliveryMode.LastDeletedById = userId;
                context.DeliveryModes.Update(deliveryMode);
                await context.SaveChangesAsync();
                return Result.Success();

            case nameof(TermsOfPayment):
                var termsOfPayment = await context.TermsOfPayments.FirstOrDefaultAsync(p =>
                    p.Id == itemId
                );
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
                var materialType = await context.MaterialTypes.FirstOrDefaultAsync(p =>
                    p.Id == itemId
                );
                if (materialType == null)
                    return Error.Validation("MaterialType", "Not found");
                materialType.DeletedAt = currentTime;
                materialType.LastDeletedById = userId;
                context.MaterialTypes.Update(materialType);
                await context.SaveChangesAsync();
                return Result.Success();

            case nameof(MaterialCategory):
                var materialCategory = await context.MaterialCategories.FirstOrDefaultAsync(p =>
                    p.Id == itemId
                );
                if (materialCategory == null)
                    return Error.Validation("MaterialCategory", "Not found");
                materialCategory.DeletedAt = currentTime;
                materialCategory.LastDeletedById = userId;
                context.MaterialCategories.Update(materialCategory);
                await context.SaveChangesAsync();
                return Result.Success();

            case nameof(ShiftCategory):
                var shiftCategory = await context.ShiftCategories.FirstOrDefaultAsync(p =>
                    p.Id == itemId && p.LastDeletedById == null
                );
                if (shiftCategory == null)
                    return Error.Validation("ShiftCategory", "Not found");
                shiftCategory.DeletedAt = currentTime;
                shiftCategory.LastDeletedById = userId;
                context.ShiftCategories.Update(shiftCategory);
                await context.SaveChangesAsync();
                return Result.Success();

            case nameof(PackageType):
                var productPackageType = await context.PackageTypes.FirstOrDefaultAsync(p =>
                    p.Id == itemId
                );
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
                var shipmentDiscrepancyType =
                    await context.ShipmentDiscrepancyTypes.FirstOrDefaultAsync(p => p.Id == itemId);
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
                var productState = await context.ProductStates.FirstOrDefaultAsync(p =>
                    p.Id == itemId
                );
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
                var itemCategory = await context.ItemCategories.FirstOrDefaultAsync(p =>
                    p.Id == itemId
                );
                if (itemCategory == null)
                    return Error.Validation("Item", "Not found");
                itemCategory.DeletedAt = currentTime;
                itemCategory.LastDeletedById = userId;
                context.ItemCategories.Update(itemCategory);
                await context.SaveChangesAsync();
                return Result.Success();

            case nameof(WarehouseLocationName):
                var warehouseLocationName =
                    await context.WarehouseLocationNames.FirstOrDefaultAsync(p => p.Id == itemId);
                if (warehouseLocationName == null)
                    return Error.Validation("WarehouseName", "Not found");
                context.WarehouseLocationNames.Remove(warehouseLocationName);
                await context.SaveChangesAsync();
                return Result.Success();

            case nameof(QcEquipmentCategory):
                var qcEquipmentCategory = await context.QcEquipmentCategories.FirstOrDefaultAsync(
                    p => p.Id == itemId
                );
                if (qcEquipmentCategory == null)
                    return Error.Validation("QcEquipmentCategory", "Not found");
                qcEquipmentCategory.DeletedAt = currentTime;
                qcEquipmentCategory.LastDeletedById = userId;
                context.QcEquipmentCategories.Update(qcEquipmentCategory);
                await context.SaveChangesAsync();
                return Result.Success();

            case nameof(Reagent):
                var reagent = await context.Reagents.FirstOrDefaultAsync(p => p.Id == itemId);
                if (reagent == null)
                    return Error.Validation("Reagent", "Not found");
                reagent.DeletedAt = currentTime;
                reagent.LastDeletedById = userId;
                context.Reagents.Update(reagent);
                await context.SaveChangesAsync();
                return Result.Success();

            case nameof(Site):
                var site = await context.Sites.FirstOrDefaultAsync(p => p.Id == itemId);
                if (site == null)
                    return Error.Validation("Site", "Not found");
                site.DeletedAt = currentTime;
                site.LastDeletedById = userId;
                context.Sites.Update(site);
                await context.SaveChangesAsync();
                return Result.Success();

            default:
                return Error.Validation("Item", "Invalid item type");
        }
    }

    public async Task<Result> CreateUoM(CreateUnitOfMeasure request)
    {
        if (await context.UnitOfMeasures.AnyAsync(u => u.Symbol == request.Symbol))
        {
            return Error.Validation("Symbol", "Symbol already exists");
        }

        if (await context.UnitOfMeasures.AnyAsync(u => u.Name == request.Name))
        {
            return Error.Validation("Name", "Name already exists");
        }
        var uom = mapper.Map<UnitOfMeasure>(request);
        await context.UnitOfMeasures.AddAsync(uom);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<Paginateable<IEnumerable<UnitOfMeasureDto>>>> GetUoM(
        FilterUnitOfMeasure filter
    )
    {
        var query = context.UnitOfMeasures.AsQueryable();

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

    public async Task<Result<IEnumerable<OperationDto>>> GetOperations(Guid? departmentId)
    {
        var query = context.Operations.AsNoTracking().OrderBy(o => o.Order).AsQueryable();

        if (departmentId.HasValue)
        {
            query = query.Where(q => q.DepartmentId == departmentId);
        }

        var items = await query.ProjectTo<OperationDto>(mapper.ConfigurationProvider).ToListAsync();
        return Result.Success(items.AsEnumerable());
    }

    public async Task<Result<UnitOfMeasureDto>> GetUoM(Guid uomId)
    {
        var uom = await context
            .UnitOfMeasures.AsNoTracking()
            .ProjectTo<UnitOfMeasureDto>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(p => p.Id == uomId);

        return uom is null ? Error.NotFound("Uom", "Uom not found") : Result.Success(uom);
    }

    public async Task<Result> UpdateUoM(CreateUnitOfMeasure request, Guid id)
    {
        var uom = await context.UnitOfMeasures.FirstOrDefaultAsync(u => u.Id == id);
        if (uom is null)
            return Error.NotFound("Uom", "Uom not found");

        if (await context.UnitOfMeasures.AnyAsync(u => u.Symbol == request.Symbol))
        {
            return Error.Validation("Symbol", "Symbol already exists");
        }

        if (await context.UnitOfMeasures.AnyAsync(u => u.Name == request.Name))
        {
            return Error.Validation("Name", "Name already exists");
        }

        mapper.Map(request, uom);

        context.UnitOfMeasures.Update(uom);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteUoM(Guid uomId)
    {
        var uom = await context.UnitOfMeasures.FirstOrDefaultAsync(u => u.Id == uomId);
        if (uom is null)
            return Error.NotFound("Uom", "Uom not found");

        uom.DeletedAt = DateTime.UtcNow;
        context.UnitOfMeasures.Update(uom);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}
