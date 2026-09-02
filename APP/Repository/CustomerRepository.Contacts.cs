using DOMAIN.Entities.Customers;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class CustomerRepository
{
    public async Task<Result<List<CustomerContactDto>>> GetContacts(Guid customerId)
    {
        if (!await CustomerExists(customerId))
            return Error.NotFound("Customer.NotFound", "Customer not found.");
        var contacts = await context.CustomerContacts.AsNoTracking()
            .Where(item => item.CustomerId == customerId)
            .OrderByDescending(item => item.IsPrimary).ThenBy(item => item.Name).ToListAsync();
        return contacts.Select(MapContact).ToList();
    }

    public async Task<Result<Guid>> CreateContact(Guid customerId, CustomerContactRequest request, Guid userId)
    {
        if (!await CustomerExists(customerId))
            return Error.NotFound("Customer.NotFound", "Customer not found.");
        if (request.IsPrimary) await ClearPrimaryContact(customerId, null, userId);
        var entity = new CustomerContact { CustomerId = customerId, CreatedById = userId };
        AssignContact(entity, request);
        context.CustomerContacts.Add(entity);
        await context.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<Result> UpdateContact(
        Guid customerId, Guid id, CustomerContactRequest request, Guid userId)
    {
        var entity = await context.CustomerContacts.FirstOrDefaultAsync(item =>
            item.Id == id && item.CustomerId == customerId);
        if (entity is null) return Error.NotFound("CustomerContact.NotFound", "Customer contact not found.");
        if (request.IsPrimary) await ClearPrimaryContact(customerId, id, userId);
        AssignContact(entity, request);
        entity.UpdatedAt = DateTime.UtcNow;
        entity.LastUpdatedById = userId;
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteContact(Guid customerId, Guid id, Guid userId)
    {
        var entity = await context.CustomerContacts.FirstOrDefaultAsync(item =>
            item.Id == id && item.CustomerId == customerId);
        if (entity is null) return Error.NotFound("CustomerContact.NotFound", "Customer contact not found.");
        entity.DeletedAt = DateTime.UtcNow;
        entity.LastDeletedById = userId;
        await context.SaveChangesAsync();
        return Result.Success();
    }

    private async Task ClearPrimaryContact(Guid customerId, Guid? exceptId, Guid userId)
    {
        var contacts = await context.CustomerContacts.Where(item => item.CustomerId == customerId
            && item.IsPrimary && (!exceptId.HasValue || item.Id != exceptId.Value)).ToListAsync();
        foreach (var contact in contacts)
        {
            contact.IsPrimary = false;
            contact.UpdatedAt = DateTime.UtcNow;
            contact.LastUpdatedById = userId;
        }
    }

    private static void AssignContact(CustomerContact entity, CustomerContactRequest request)
    {
        entity.Name = request.Name.Trim();
        entity.Role = request.Role?.Trim();
        entity.Email = request.Email?.Trim();
        entity.Phone = request.Phone?.Trim();
        entity.IsPrimary = request.IsPrimary;
    }

    private static CustomerContactDto MapContact(CustomerContact item) => new()
    {
        Id = item.Id, CreatedAt = item.CreatedAt, CustomerId = item.CustomerId,
        Name = item.Name, Role = item.Role, Email = item.Email, Phone = item.Phone,
        IsPrimary = item.IsPrimary,
    };

    private Task<bool> CustomerExists(Guid customerId)
        => context.Customers.AnyAsync(item => item.Id == customerId);
}
