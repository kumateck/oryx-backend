using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Forms;

public enum CertificateReturnKind
{
    Review = 1,
    Correction = 2
}

public sealed class PendingCertificateChange
{
    public Guid Id { get; set; }
    public Guid ResponseId { get; set; }
    public int Version { get; set; }
    public Guid ActorId { get; set; }
    public DateTime ChangedAt { get; set; }
    public CertificateReturnKind? RequestedReturn { get; set; }
    [MaxLength(2000)] public string Reason { get; set; }
    public string PreviousStateJson { get; set; }
    public string NewStateJson { get; set; }
}

public sealed record RequestCertificateChange(
    [Range(0, int.MaxValue)] int Version,
    [Required, StringLength(2000)] string Reason);

public sealed record SavePendingCertificate(
    [Range(0, int.MaxValue)] int Version,
    [Required, StringLength(2000)] string Reason,
    [StringLength(2000)] string Notes,
    [Required] List<CertificateOfAnalysisComplies> Compliance);
