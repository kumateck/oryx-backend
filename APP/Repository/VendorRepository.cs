using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Vendors;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class VendorRepository(ApplicationDbContext context, IMapper mapper) : IVendorRepository
{
    public async Task<Result<Guid>> CreateVendor(CreateVendorRequest request)
    {
        var existingSupplier =
            await context.Vendors.FirstOrDefaultAsync(nps => nps.Name == request.Name);

        if (existingSupplier != null)
            return Error.Validation("Vendor.Exists", "Vendor already exists");

        var validInventoryIds = await context.Items
            .Where(s => request.ItemIds.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync();

        var missingIds = request.ItemIds.Except(validInventoryIds).ToList();
        if (missingIds.Count != 0)
            return Error.NotFound("Items.NotFound", $"Some items not found: {string.Join(", ", missingIds)}");

        var vendor = mapper.Map<Vendor>(request);
        await context.AddAsync(vendor);
        await context.SaveChangesAsync();
        return vendor.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<VendorDto>>>> GetVendors(int page, int pageSize, string searchQuery)
    {
        var query = context.Vendors.AsQueryable();

        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize,
            mapper.Map<VendorDto>);
    }

    public async Task<Result<VendorDto>> GetVendor(Guid id)
    {
        var supplier = await context.Vendors.FirstOrDefaultAsync(nps => nps.Id == id);
        return supplier is null
            ? Error.NotFound("Vendor.NotFound", "Vendor not found")
            : mapper.Map<VendorDto>(supplier);
    }

    public async Task<Result> UpdateVendor(Guid id, CreateVendorRequest request)
    {
        var supplier = await context.Vendors.FirstOrDefaultAsync(nps => nps.Id == id);
        if (supplier == null)
            return Error.NotFound("Vendor.NotFound", "Vendor not found");
        
        var validInventoryIds = await context.Items
            .Where(s => request.ItemIds.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync();

        var missingIds = request.ItemIds.Except(validInventoryIds).ToList();
        if (missingIds.Count != 0)
            return Error.NotFound("Items.NotFound", $"Some items not found: {string.Join(", ", missingIds)}");
        
        mapper.Map(request, supplier);
        context.Vendors.Update(supplier);
        await context.SaveChangesAsync();
        return Result.Success();

    }

    public async Task<Result> DeleteVendor(Guid id, Guid userId)
    {
        var supplier = await context.Vendors.FirstOrDefaultAsync(nps => nps.Id == id);

        if (supplier == null)
            return Error.NotFound("Vendor.NotFound", "Vendor not found");
        
        supplier.DeletedAt = DateTime.UtcNow;
        supplier.LastDeletedById = userId;
        
        context.Vendors.Update(supplier);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}