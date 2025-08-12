namespace DOMAIN.Entities.AnalyticalTestRequests;

public class CreateAnalyticalTestRequest
{
    public Guid BatchManufacturingRecordId { get; set; }
    public Guid ProductId { get; set; }
    public Guid ProductionScheduleId { get; set; }
    public string SampledQuantity { get; set; }
    public DateTime ManufacturingDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string ReleasedAt { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public TestStage Stage { get; set; }
    public AnalyticalTestStatus Status { get; set; }
    public string Filled { get; set;}
    public Guid StateId { get; set; }
    public Guid? ProductionActivityStepId { get; set; }
    public int NumberOfContainers { get; set; }
    public DateTime? SampledAt { get; set; }
}

public class UpdateAnalyticalTestRequest
{
    public AnalyticalTestStatus Status { get; set; }
    public int NumberOfContainers { get; set; }
    public string SampledQuantity { get; set; }
}