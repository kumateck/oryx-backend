namespace DOMAIN.Entities.Reports;
public sealed class ReportHistoryCapabilitiesDto
{
    public int ProtocolVersion { get; set; } = 1;
    public DateTime ServerTime { get; set; }
    public bool Available { get; set; }
    public DateTime? AvailableFrom { get; set; }
    public string Basis { get; set; } = "RecordedState";
}
