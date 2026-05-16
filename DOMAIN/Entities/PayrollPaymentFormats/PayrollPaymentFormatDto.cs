using DOMAIN.Entities.Base;
using SHARED;

namespace DOMAIN.Entities.PayrollPaymentFormats;

public class PayrollPaymentFormatDto : BaseDto
{
    public string Code { get; set; }
    public string Name { get; set; }
    public CollectionItemDto Country { get; set; }
    public string BankCodeMatch { get; set; }
    public string AdapterKey { get; set; }
    public string SchemaJson { get; set; }
    public bool IsActive { get; set; }
}