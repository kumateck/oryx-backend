using DOMAIN.Entities.Procurement.Suppliers;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class SupplierRelationshipRepository
{
    public async Task<Result<List<SupplierContactDto>>> GetContacts(Guid supplierId)
    {
        var supplier = await RequireSupplier(supplierId);
        if (!supplier.IsSuccess) return supplier.Error;
        var items = await context.SupplierContacts.AsNoTracking()
            .Where(item => item.SupplierId == supplierId)
            .OrderByDescending(item => item.IsPrimary).ThenBy(item => item.Name)
            .ToListAsync();
        return items.Select(ToContactDto).ToList();
    }

    public async Task<Result<Guid>> CreateContact(Guid supplierId, SupplierContactRequest request, Guid userId)
    {
        var supplier = await RequireSupplier(supplierId);
        if (!supplier.IsSuccess) return supplier.Error;
        if (request.IsPrimary)
            await ClearPrimaryContact(supplierId, null);
        var entity = new SupplierContact { SupplierId = supplierId, CreatedById = userId };
        Apply(entity, request);
        await context.SupplierContacts.AddAsync(entity);
        await context.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<Result> UpdateContact(
        Guid supplierId, Guid id, SupplierContactRequest request, Guid userId)
    {
        var entity = await context.SupplierContacts.FirstOrDefaultAsync(item =>
            item.Id == id && item.SupplierId == supplierId);
        if (entity is null)
            return Error.NotFound("SupplierContact.NotFound", "Supplier contact not found.");
        if (request.IsPrimary)
            await ClearPrimaryContact(supplierId, id);
        Apply(entity, request);
        entity.LastUpdatedById = userId;
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteContact(Guid supplierId, Guid id, Guid userId)
    {
        var entity = await context.SupplierContacts.FirstOrDefaultAsync(item =>
            item.Id == id && item.SupplierId == supplierId);
        if (entity is null)
            return Error.NotFound("SupplierContact.NotFound", "Supplier contact not found.");
        SoftDelete(entity, userId);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    private async Task ClearPrimaryContact(Guid supplierId, Guid? exceptId)
    {
        var current = await context.SupplierContacts.Where(item => item.SupplierId == supplierId
            && item.IsPrimary && (!exceptId.HasValue || item.Id != exceptId)).ToListAsync();
        foreach (var item in current) item.IsPrimary = false;
    }

    private static void Apply(SupplierContact entity, SupplierContactRequest request)
    {
        entity.Name = request.Name.Trim();
        entity.Role = request.Role?.Trim();
        entity.Email = request.Email?.Trim();
        entity.Phone = request.Phone?.Trim();
        entity.IsPrimary = request.IsPrimary;
    }

    private static SupplierContactDto ToContactDto(SupplierContact item) => new()
    {
        Id = item.Id, CreatedAt = item.CreatedAt, SupplierId = item.SupplierId,
        Name = item.Name, Role = item.Role, Email = item.Email, Phone = item.Phone,
        IsPrimary = item.IsPrimary,
    };
}
