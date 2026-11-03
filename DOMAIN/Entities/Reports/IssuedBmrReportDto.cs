#nullable enable
using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Reports;

public sealed class IssuedBmrReportFilter
{
    /// <summary>Inclusive UTC calendar dates, applied to the recorded BMR issue time.</summary>
    [Required] public DateOnly? StartDate { get; set; }
    [Required] public DateOnly? EndDate { get; set; }
    public Guid? DepartmentId { get; set; }
}

public sealed class IssuedBmrReportRowDto
{
    public Guid Id { get; set; }
    public Guid ScheduleProductId { get; set; }
    public Guid ProductId { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? ScheduleCode { get; set; }
    public string? BatchNumber { get; set; }
    public decimal BatchQuantity { get; set; }
    public string? BatchUnit { get; set; }
    public string? PackingStyle { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime IssuedAt { get; set; }
    public Guid? IssuedById { get; set; }
    public string? IssuedByName { get; set; }
    public bool IsArchived { get; set; }
    public bool IsCancelled { get; set; }
}

public sealed class IssuedBmrReportDto
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public DateTime GeneratedAt { get; set; }
    public int TotalIssued { get; set; }
    public List<IssuedBmrReportRowDto> Rows { get; set; } = [];
}
