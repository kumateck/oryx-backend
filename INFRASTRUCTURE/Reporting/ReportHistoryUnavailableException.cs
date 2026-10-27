namespace INFRASTRUCTURE.Reporting;
public sealed class ReportHistoryUnavailableException(string message, DateTime? availableFrom = null) : Exception(message)
{
    public DateTime? AvailableFrom { get; } = availableFrom;
}
