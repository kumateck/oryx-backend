using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Countries;

namespace DOMAIN.Entities.PayrollPaymentFormats;

public class PayrollPaymentFormat : BaseEntity
{
    [StringLength(50)] public string Code { get; set; }
    [StringLength(255)] public string Name { get; set; }
    public Guid? CountryId { get; set; }
    public Country Country { get; set; }
    
    [StringLength(50)] public string BankCodeMatch { get; set; }
    [StringLength(100)] public string AdapterKey { get; set; }
    [StringLength(int.MaxValue)] public string SchemaJson { get; set; }
    public bool IsActive { get; set; } = true;

}