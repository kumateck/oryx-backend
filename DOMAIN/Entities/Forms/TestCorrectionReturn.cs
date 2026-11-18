using System.ComponentModel.DataAnnotations;
namespace DOMAIN.Entities.Forms;

/// <summary>Append-only evidence linking the submitted worksheet to its populated correction draft.</summary>
public sealed class TestCorrectionReturn
{
    public Guid Id { get; set; }
    public Guid SourceResponseId { get; set; }
    public Guid CorrectionResponseId { get; set; }
    public Guid ReturnedById { get; set; }
    public DateTime ReturnedAt { get; set; }
    [MaxLength(2000)] public string Reason { get; set; }
    public string PreviousState { get; set; }
}
