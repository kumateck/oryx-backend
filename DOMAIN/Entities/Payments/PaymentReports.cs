namespace DOMAIN.Entities.Payments;

public class AgingReportDto
{
    public Guid BaseCurrencyId { get; set; }
    public string BaseCurrencyName { get; set; }
    public decimal TotalOutstandingBase { get; set; }
    public List<AgingPartyDto> Parties { get; set; } = [];
    public List<string> DataQualityWarnings { get; set; } = [];
}

public class AgingPartyDto
{
    public Guid PartyId { get; set; }
    public string PartyName { get; set; }
    public decimal TotalOutstandingBase { get; set; }
    public AgingBucketsDto BucketsBase { get; set; } = new();
    public List<AgingLineDto> Lines { get; set; } = [];
}

public class AgingLineDto
{
    public PayableType PayableType { get; set; }
    public Guid PayableId { get; set; }
    public string DocumentCode { get; set; }
    public DateTime? DueDate { get; set; }
    public Guid CurrencyId { get; set; }
    public string CurrencyName { get; set; }
    public decimal OriginalOutstanding { get; set; }
    public decimal RateToBase { get; set; }
    public decimal OutstandingBase { get; set; }
    public AgingBucket Bucket { get; set; }
}

public class AgingBucketsDto
{
    public decimal Current { get; set; }
    public decimal Days0To30 { get; set; }
    public decimal Days31To60 { get; set; }
    public decimal Days61To90 { get; set; }
    public decimal DaysOver90 { get; set; }
    public decimal DueDateUnknown { get; set; }
}

public enum AgingBucket
{
    Current = 0,
    Days0To30 = 1,
    Days31To60 = 2,
    Days61To90 = 3,
    DaysOver90 = 4,
    DueDateUnknown = 5,
}

public class CashflowSummaryDto
{
    public Guid BaseCurrencyId { get; set; }
    public string BaseCurrencyName { get; set; }
    public CashflowDirectionDto ProjectedOutflows { get; set; } = new();
    public CashflowDirectionDto ProjectedInflows { get; set; } = new();
    public List<string> DataQualityWarnings { get; set; } = [];
}

public class CashflowDirectionDto
{
    public decimal TotalOutstandingBase { get; set; }
    public decimal Overdue { get; set; }
    public decimal Next7Days { get; set; }
    public decimal Days8To30 { get; set; }
    public decimal Days31To60 { get; set; }
    public decimal Days61To90 { get; set; }
    public decimal Beyond90Days { get; set; }
    public decimal DueDateUnknown { get; set; }
}
