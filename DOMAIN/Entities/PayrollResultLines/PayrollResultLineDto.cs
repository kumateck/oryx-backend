using DOMAIN.Entities.Base;
using SHARED;

namespace DOMAIN.Entities.PayrollResultLines;

public class PayrollResultLineDto : BaseDto
{
    public CollectionItemDto PayrollRunEmployee { get; set; }
    public CollectionItemDto PayrollElement { get; set; }
    public CollectionItemDto PayrollElementVersion { get; set; }

    public int Sequence { get; set; }
    public string InputsJson { get; set; }
    public string RuleVersionRef { get; set; }

    public decimal Amount { get; set; }
    public CollectionItemDto Currency { get; set; }

    public bool IsRetroAdjustment { get; set; }
    public Guid? RetroSourceLineId { get; set; }
    public string Hash { get; set; }
}