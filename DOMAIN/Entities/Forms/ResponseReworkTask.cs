using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Forms;

public enum ResponseRejectionOutcome { Disposed = 0, Rework = 1 }
public enum ResponseReworkStatus { AwaitingQc = 0, Testing = 1, AwaitingQa = 2, Completed = 3, Rejected = 4 }

/// <summary>A new test cycle prompted by QA rejection of a Response.</summary>
public class ResponseReworkTask : BaseEntity
{
    public Guid SourceResponseId { get; set; }
    public Response SourceResponse { get; set; }
    public Guid RootResponseId { get; set; }
    public Response RootResponse { get; set; }
    public Guid? NewResponseId { get; set; }
    public Response NewResponse { get; set; }
    public Guid? MaterialBatchId { get; set; }
    public Guid? BatchManufacturingRecordId { get; set; }
    [Required, StringLength(2000)] public string Reason { get; set; }
    public Guid RequestedById { get; set; }
    public DateTime RequestedAt { get; set; }
    public ResponseReworkStatus Status { get; set; } = ResponseReworkStatus.AwaitingQc;
    public DateTime? StartedAt { get; set; }
    public Guid? StartedById { get; set; }
    public DateTime? CompletedAt { get; set; }
}
