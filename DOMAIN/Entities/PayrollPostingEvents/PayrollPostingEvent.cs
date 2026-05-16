using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.PayrollRuns;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.PayrollPostingEvents;


public class PayrollPostingEvent : BaseEntity
{
    public Guid PayrollRunId { get; set; }
    public PayrollRun PayrollRun { get; set; }

    public PayrollPostingEventType EventType { get; set; } = PayrollPostingEventType.RunAccrual;
    public PayrollPostingEventStatus Status { get; set; } = PayrollPostingEventStatus.Pending;

    public Guid? JournalEntryId { get; set; }

    [StringLength(int.MaxValue)] public string RequestPayloadJson { get; set; }
    [StringLength(int.MaxValue)] public string ResponsePayloadJson { get; set; }

    public DateTime? PostedAt { get; set; }
    public Guid? PostedById { get; set; }
    public User PostedBy { get; set; }

    [StringLength(2000)] public string RejectionReason { get; set; }

    public Guid? ParentEventId { get; set; }
    public PayrollPostingEvent ParentEvent { get; set; }
}

public enum PayrollPostingEventType
{
    RunAccrual = 0,
    PaymentSettlement = 1,
    Reversal = 2
}

public enum PayrollPostingEventStatus
{
    Pending = 0,
    Posted = 1,
    SkippedModuleDisabled = 2,
    RejectedValidation = 3,
    DuplicateEventIgnored = 4,
    Reversed = 5
}