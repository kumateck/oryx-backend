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

    Task<Result<List<CustomerContactDto>>> GetContacts(Guid customerId);
    Task<Result<Guid>> CreateContact(Guid customerId, CustomerContactRequest request, Guid userId);
    Task<Result> UpdateContact(Guid customerId, Guid id, CustomerContactRequest request, Guid userId);
    Task<Result> DeleteContact(Guid customerId, Guid id, Guid userId);

    Task<Result<List<CustomerPricingAgreementDto>>> GetPricingAgreements(Guid customerId);
    Task<Result<Guid>> CreatePricingAgreement(Guid customerId, CustomerPricingAgreementRequest request, Guid userId);
    Task<Result> UpdatePricingAgreement(Guid customerId, Guid id, CustomerPricingAgreementRequest request, Guid userId);
    Task<Result> DeletePricingAgreement(Guid customerId, Guid id, Guid userId);
    Task<Result<CustomerPricingAgreementDto>> GetActivePricingAgreement(
        Guid customerId, Guid productId, Guid uomId, DateTime asOf);

    Task<Result<CustomerCreditStatusDto>> GetAvailableCredit(Guid customerId, decimal additionalOrderValue = 0);
    Task<Result<bool>> IsWithinCreditLimit(Guid customerId, decimal additionalOrderValue);

    Task<Result<Guid>> CreateQuotation(Guid customerId, CreateCustomerQuotationRequest request, Guid userId);
    Task<Result<CustomerQuotationDto>> GetQuotation(Guid quotationId, DateTime? asOf = null);
    Task<Result<Paginateable<IEnumerable<CustomerQuotationDto>>>> GetActiveQuotations(
        Guid customerId, int page, int pageSize, DateTime? asOf = null);
    Task<Result> SendQuotation(Guid quotationId, Guid userId);
    Task<Result> ApproveQuotation(Guid quotationId, CustomerQuotationApprovalRequest request, Guid userId);
    Task<Result<Guid>> ConvertQuotationToProductionOrder(Guid quotationId, Guid userId);

    Task<Result<Paginateable<IEnumerable<CustomerOrderHistoryDto>>>> GetOrderHistory(
        Guid customerId, int page, int pageSize);
    Task<Result<CustomerSummaryDto>> GetSummary(Guid customerId);
}
