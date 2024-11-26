using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;
using DOMAIN.Entities.Procurement.Manufacturers;
using DOMAIN.Entities.Procurement.Suppliers;

namespace APP.Repository;

public class ProcurementRepository(ApplicationDbContext context, IMapper mapper) : IProcurementRepository
{
    // ************* CRUD for Manufacturer *************

    public async Task<Result<Guid>> CreateManufacturer(CreateManufacturerRequest request, Guid userId)
    {
        var manufacturer = mapper.Map<Manufacturer>(request);
        manufacturer.CreatedById = userId;
        await context.Manufacturers.AddAsync(manufacturer);
        await context.SaveChangesAsync();

        return manufacturer.Id;
    }

    public async Task<Result<ManufacturerDto>> GetManufacturer(Guid manufacturerId)
    {
        var manufacturer = await context.Manufacturers
            .Include(m => m.Materials)
            .FirstOrDefaultAsync(m => m.Id == manufacturerId);

        return manufacturer is null
            ? Error.NotFound("Manufacturer.NotFound", "Manufacturer not found")
            : mapper.Map<ManufacturerDto>(manufacturer);
    }
    
    public async Task<Result<Paginateable<IEnumerable<ManufacturerDto>>>> GetManufacturers(int page, int pageSize, string searchQuery)
    {
        var query = context.Manufacturers
            .Include(m => m.Materials)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, m => m.Name, m => m.Address);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<ManufacturerDto>
        );
    }
    
    public async Task<Result<IEnumerable<ManufacturerDto>>> GetManufacturersByMaterial(Guid materialId)
    {
       return mapper.Map<List<ManufacturerDto>>( await context.Manufacturers
            .Include(m => m.Materials).ThenInclude(m => m.Material)
            .Where(m => m.Materials.Select(ma => ma.MaterialId).Contains(materialId))
            .ToListAsync());
    }
    
    public async Task<Result> UpdateManufacturer(CreateManufacturerRequest request, Guid manufacturerId, Guid userId)
    {
        var existingManufacturer = await context.Manufacturers.FirstOrDefaultAsync(m => m.Id == manufacturerId);
        if (existingManufacturer is null)
        {
            return Error.NotFound("Manufacturer.NotFound", "Manufacturer not found");
        }

        mapper.Map(request, existingManufacturer);
        existingManufacturer.LastUpdatedById = userId;

        context.Manufacturers.Update(existingManufacturer);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    // Delete Manufacturer (soft delete)
    public async Task<Result> DeleteManufacturer(Guid manufacturerId, Guid userId)
    {
        var manufacturer = await context.Manufacturers.FirstOrDefaultAsync(m => m.Id == manufacturerId);
        if (manufacturer is null)
        {
            return Error.NotFound("Manufacturer.NotFound", "Manufacturer not found");
        }

        manufacturer.DeletedAt = DateTime.UtcNow;
        manufacturer.LastDeletedById = userId;

        context.Manufacturers.Update(manufacturer);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    // ************* CRUD for Supplier *************

    public async Task<Result<Guid>> CreateSupplier(CreateSupplierRequest request, Guid userId)
    {
        var supplier = mapper.Map<Supplier>(request);
        supplier.CreatedById = userId;
        await context.Suppliers.AddAsync(supplier);
        await context.SaveChangesAsync();

        return supplier.Id;
    }

    public async Task<Result<SupplierDto>> GetSupplier(Guid supplierId)
    {
        var supplier = await context.Suppliers
            .Include(s => s.AssociatedManufacturers).ThenInclude(sm => sm.Manufacturer)
            .Include(s => s.AssociatedManufacturers).ThenInclude(sm => sm.Material)
            .FirstOrDefaultAsync(s => s.Id == supplierId);

        return supplier is null
            ? Error.NotFound("Supplier.NotFound", "Supplier not found")
            : mapper.Map<SupplierDto>(supplier);
    }
    
    public async Task<Result<Paginateable<IEnumerable<SupplierDto>>>> GetSuppliers(int page, int pageSize, string searchQuery)
    {
        var query = context.Suppliers
            .Include(s => s.AssociatedManufacturers).ThenInclude(sm => sm.Manufacturer)
            .Include(s => s.AssociatedManufacturers).ThenInclude(sm => sm.Material)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, s => s.Name, s => s.ContactPerson);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<SupplierDto>
        );
    }
    public async Task<Result<IEnumerable<SupplierDto>>> GetSupplierByMaterial(Guid materialId)
    {
        return mapper.Map<List<SupplierDto>>( await context.Suppliers
            .Include(s => s.AssociatedManufacturers).ThenInclude(sm => sm.Manufacturer)
            .Include(s => s.AssociatedManufacturers).ThenInclude(sm => sm.Material)
            .Where(m => m.AssociatedManufacturers.Select(ma => ma.MaterialId).Contains(materialId))
            .ToListAsync());
    }

    public async Task<Result> UpdateSupplier(CreateSupplierRequest request, Guid supplierId, Guid userId)
    {
        var existingSupplier = await context.Suppliers.FirstOrDefaultAsync(s => s.Id == supplierId);
        if (existingSupplier is null)
        {
            return Error.NotFound("Supplier.NotFound", "Supplier not found");
        }

        mapper.Map(request, existingSupplier);
        existingSupplier.LastUpdatedById = userId;

        context.Suppliers.Update(existingSupplier);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteSupplier(Guid supplierId, Guid userId)
    {
        var supplier = await context.Suppliers.FirstOrDefaultAsync(s => s.Id == supplierId);
        if (supplier is null)
        {
            return Error.NotFound("Supplier.NotFound", "Supplier not found");
        }

        supplier.DeletedAt = DateTime.UtcNow;
        supplier.LastDeletedById = userId;

        context.Suppliers.Update(supplier);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}
