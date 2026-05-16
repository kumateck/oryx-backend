using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.PayrollElements;
using DOMAIN.Entities.PayrollRunEmployees;

namespace DOMAIN.Entities.PayrollResultLines;


public class PayrollResultLine : BaseEntity
{
    public Guid PayrollRunEmployeeId { get; set; }
    public PayrollRunEmployee PayrollRunEmployee { get; set; }

    public Guid PayrollElementId { get; set; }
    public PayrollElement PayrollElement { get; set; }

    public Guid PayrollElementVersionId { get; set; }
    public PayrollElementVersion PayrollElementVersion { get; set; }

    public int Sequence { get; set; }

    [StringLength(int.MaxValue)] public string InputsJson { get; set; }
    [StringLength(100)] public string RuleVersionRef { get; set; }

    public decimal Amount { get; set; }

    public Guid CurrencyId { get; set; }
    public Currency Currency { get; set; }

    public bool IsRetroAdjustment { get; set; }

    public Guid? RetroSourceLineId { get; set; }
    public PayrollResultLine RetroSourceLine { get; set; }

    [StringLength(128)] public string Hash { get; set; }

}