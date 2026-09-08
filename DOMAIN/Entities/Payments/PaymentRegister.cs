using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Currencies;

namespace DOMAIN.Entities.Payments;

public class PaymentListRequest
{
    [Range(1, 1000000)] public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 25;
    [StringLength(255)] public string? SearchQuery { get; set; }
    [EnumDataType(typeof(PaymentStatus))] public PaymentStatus? Status { get; set; }
    public bool? IsReceipt { get; set; }
}

public class PaymentListDto
{
    public List<PaymentListItemDto> Data { get; set; } = [];
    public int PageIndex { get; set; }
    public int PageCount { get; set; }
    public int TotalRecordCount { get; set; }
}

public class PaymentListItemDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public CurrencyDto Currency { get; set; }
    public DateTime PaymentDate { get; set; }
    public PaymentMethod Method { get; set; }
    public string Reference { get; set; }
    public PayableType PayableType { get; set; }
    public Guid PayableId { get; set; }
    public PaymentStatus Status { get; set; }
}

public class CashflowCurrencyDto
{
    public CurrencyDto? BaseCurrency { get; set; }
    public DateTime AsOf { get; set; }
    public List<CashflowRateDto> Rates { get; set; } = [];
}

public class CashflowRateDto
{
    public CurrencyDto Currency { get; set; }
    public decimal? RateToBase { get; set; }
    public DateTime? EffectiveDate { get; set; }
}
