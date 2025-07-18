using APP.Utils;
using DOMAIN.Entities.Customers;
using SHARED;

namespace APP.IRepository;

public interface ICustomerRepository
{
    Task<Result<Guid>> CreateCustomer(CreateCustomerRequest customer);

    Task<Result<Paginateable<IEnumerable<CustomerDto>>>> GetCustomers(int page, int pageSize, string searchQuery);
    
    Task<Result<CustomerDto>> GetCustomer(Guid customerId);
    
    Task<Result> UpdateCustomer(Guid customerId, CreateCustomerRequest request);
    
    Task<Result> DeleteCustomer(Guid customerId, Guid id);
}