using DOMAIN.Entities.Base;
using DOMAIN.Entities.Users;
using SHARED;

namespace DOMAIN.Entities.PayrollPostingEvents;

public class PayrollPostingEventDto : BaseDto
{
    public CollectionItemDto PayrollRun { get; set; }
    public PayrollPostingEventType EventType { get; set; } 
    public PayrollPostingEventStatus Status { get; set; } 
    public Guid? JournalEntryId { get; set; }
    public string RequestPayloadJson { get; set; }
    public string ResponsePayloadJson { get; set; }
    public DateTime? PostedAt { get; set; }
    public UserDto PostedBy { get; set; }
    public string RejectionReason { get; set; }
    public Guid? ParentEventId { get; set; }
}