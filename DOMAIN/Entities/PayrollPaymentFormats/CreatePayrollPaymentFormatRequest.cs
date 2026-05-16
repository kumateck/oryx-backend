namespace DOMAIN.Entities.PayrollPaymentFormats;

public class CreatePayrollPaymentFormatRequest
{
    public string Code { get; set; }
    public string Name { get; set; }
    public Guid? CountryId { get; set; }
    public string BankCodeMatch { get; set; }
    public string AdapterKey { get; set; }
    public string SchemaJson { get; set; }
}