using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.AnalyticalTestRequests;

public class CreateAnalyticalTestRequest
{
    public Guid BatchManufacturingRecordId { get; set; }
    public Guid ProductId { get; set; }
    public Guid ProductionScheduleId { get; set; }
    [Required] public string SampledQuantity { get; set; }
    [Required] public DateTime ManufacturingDate { get; set; }
    [Required] public DateTime ExpiryDate { get; set; }
    [Required] public string ReleasedAt { get; set; }
    [Required] public DateTime ReleaseDate { get; set; }
    [Required] public TestStage Stage { get; set; }
    [Required] public Status Status { get; set; }
    public string Filled { get; set;}
    public Guid StateId { get; set; }
}