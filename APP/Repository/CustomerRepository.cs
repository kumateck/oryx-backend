using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Customers;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class CustomerRepository(
    ApplicationDbContext context,
    IMapper mapper,
    IApprovalRepository approvalRepository
) : ICustomerRepository
{
    public async Task<Result<Guid>> CreateCustomer(CreateCustomerRequest request)
    {
        var existingCustomer = await context.Customers
            .AnyAsync(c => c.Name == request.Name || c.Email == request.Email
            || c.Phone == request.Phone);

        if (existingCustomer) return Error.Validation("Customer.Exists", "Customer already exists");

        var references = await ValidateCustomerReferences(request);
        if (!references.IsSuccess) return references.Error;

        var customer = mapper.Map<Customer>(request);
        customer.BillingAddress ??= request.Address;
        customer.ShippingAddress ??= request.Address;
        await context.Customers.AddAsync(customer);

        await context.SaveChangesAsync();
        return customer.Id;

    }

    public async Task<Result<Paginateable<IEnumerable<CustomerDto>>>> GetCustomers(int page, int pageSize, string searchQuery)
    {
        var query = context.Customers
            .Include(c => c.CreatedBy)
            .Include(c => c.TermsOfPayment)
            .Include(c => c.Currency)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, c => c.Name, c => c.Email);
        }

        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, mapper.Map<CustomerDto>);
    }

    public async Task<Result<CustomerDto>> GetCustomer(Guid customerId)
    {
        var customer = await context.Customers
            .Include(c => c.CreatedBy)
            .Include(c => c.TermsOfPayment)
            .Include(c => c.Currency)
            .FirstOrDefaultAsync(c => c.Id == customerId);
        return customer is null ?
            Error.NotFound("Customer.NotFound", "Customer not found") :
            mapper.Map<CustomerDto>(customer);
    }

    public async Task<Result> UpdateCustomer(Guid customerId, CreateCustomerRequest request)
    {
        var customer = await context.Customers.FirstOrDefaultAsync(c => c.Id == customerId);
        if (customer == null) return Error.NotFound("Customer.NotFound", "Customer not found");

        var references = await ValidateCustomerReferences(request, customer.CurrencyId, customer.CreditLimit);
        if (!references.IsSuccess) return references.Error;

        var existingCreditLimit = customer.CreditLimit;
        var existingTerms = customer.TermsOfPaymentId;
        var existingType = customer.Type;
        var existingCurrency = customer.CurrencyId;
        var existingBillingAddress = customer.BillingAddress;
        var existingShippingAddress = customer.ShippingAddress;
        mapper.Map(request, customer);
        customer.CreditLimit = request.CreditLimit ?? existingCreditLimit;
        customer.TermsOfPaymentId = request.TermsOfPaymentId ?? existingTerms;
        customer.Type = request.Type ?? existingType;
        customer.CurrencyId = request.CurrencyId ?? existingCurrency;
        customer.BillingAddress = request.BillingAddress ?? existingBillingAddress;
        customer.ShippingAddress = request.ShippingAddress ?? existingShippingAddress;
        customer.BillingAddress ??= request.Address;
        customer.ShippingAddress ??= request.Address;
        context.Customers.Update(customer);

        await context.SaveChangesAsync();
        return Result.Success();
    }

    private async Task<Result> ValidateCustomerReferences(
        CreateCustomerRequest request, Guid? existingCurrencyId = null, decimal? existingCreditLimit = null)
    {
        if (request.CreditLimit < 0)
            return Error.Validation("Customer.CreditLimit", "Credit limit cannot be negative.");
        if (request.TermsOfPaymentId.HasValue && !await context.TermsOfPayments
                .AnyAsync(item => item.Id == request.TermsOfPaymentId.Value))
            return Error.NotFound("TermsOfPayment.NotFound", "Terms of payment not found.");
        if (request.CurrencyId.HasValue && !await context.Currencies
                .AnyAsync(item => item.Id == request.CurrencyId.Value))
            return Error.NotFound("Currency.NotFound", "Currency not found.");
        if ((request.CreditLimit ?? existingCreditLimit).HasValue
            && !(request.CurrencyId ?? existingCurrencyId).HasValue)
            return Error.Validation("Customer.CurrencyRequired", "A preferred currency is required when setting a credit limit.");
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
