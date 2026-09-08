using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Payments;
using SHARED;

namespace APP.IRepository;

public interface IPaymentRepository
{
    Task<Result<Guid>> RecordPayment(RecordPaymentRequest request, Guid userId);
    Task<Result<PaymentDto>> GetPayment(Guid paymentId);
    Task<Result<PaymentListDto>> GetPayments(PaymentListRequest request);
    Task<Result<CashflowCurrencyDto>> GetCurrencyConfiguration(DateTime? asOf = null);
    Task<Result> ReviewPayment(
        Guid paymentId,
        ReviewPaymentRequest request,
        Guid userId,
        List<Guid> roleIds
    );
    Task<Result<AgingReportDto>> GetApAging(DateTime? asOf = null);
    Task<Result<AgingReportDto>> GetArAging(DateTime? asOf = null);
    Task<Result<CashflowSummaryDto>> GetCashflowSummary(DateTime? asOf = null);
    Task<Result> SetBaseCurrency(Guid currencyId);
    Task<Result<Guid>> AddExchangeRate(CreateExchangeRateRequest request, Guid userId);
    Task<Result<ExchangeRateDto>> GetExchangeRate(Guid currencyId, DateTime asOf);
}
