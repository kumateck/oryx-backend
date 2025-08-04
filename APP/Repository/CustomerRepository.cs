using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Customers;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class CustomerRepository(ApplicationDbContext context, IMapper mapper) : ICustomerRepository
{
    public async Task<Result<Guid>> CreateCustomer(CreateCustomerRequest request)
    {
        var customer = await context.Customers
            .FirstOrDefaultAsync(c => c.Name == request.Name || c.Email == request.Email
            || c.Phone == request.Phone);
        
        if (customer != null) return Error.Validation("Customer.Exists", "Customer already exists");
        
        customer = mapper.Map<Customer>(request);
        await context.Customers.AddAsync(customer);
        
        await context.SaveChangesAsync();
        return customer.Id;
        
    }

    public async Task<Result<Paginateable<IEnumerable<CustomerDto>>>> GetCustomers(int page, int pageSize, string searchQuery)
    {
        var query = context.Customers
            .Include(c => c.CreatedBy)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, c=> c.Name, c=> c.Email);
        }
        
        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, mapper.Map<CustomerDto>);
    }

    public async Task<Result<CustomerDto>> GetCustomer(Guid customerId)
    {
        var customer = await context.Customers
            .Include(c => c.CreatedBy)
            .FirstOrDefaultAsync(c => c.Id == customerId);
        return customer is null ? 
            Error.NotFound("Customer.NotFound", "Customer not found") : 
            mapper.Map<CustomerDto>(customer);
    }

    public async Task<Result> UpdateCustomer(Guid customerId, CreateCustomerRequest request)
    {
        var customer  = await context.Customers.FirstOrDefaultAsync(c => c.Id == customerId);
        if (customer == null) return Error.NotFound("Customer.NotFound", "Customer not found");
        
        mapper.Map(request, customer);
        context.Customers.Update(customer);
        
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteCustomer(Guid customerId, Guid id)
    {
        var customer = await context.Customers.FirstOrDefaultAsync(c => c.Id == customerId);
        if (customer == null) return Error.NotFound("Customer.NotFound", "Customer not found");
        
        customer.DeletedAt = DateTime.UtcNow;
        customer.LastDeletedById = id;
        
        context.Customers.Update(customer);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}