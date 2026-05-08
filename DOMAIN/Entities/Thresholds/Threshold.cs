namespace DOMAIN.Entities.Thresholds;

public class CreateThreshold
{
    public decimal StoreThreshold { get; set; }
    public decimal MemoThreshold { get; set; }
}

public class Threshold
{
    public Guid Id { get; set; }
    public decimal StoreThreshold { get; set; }
    public decimal MemoThreshold { get; set; }
}

public class ThresholdDto
{
    public Guid Id { get; set; }
    public decimal StoreThreshold { get; set; }
    public decimal MemoThreshold { get; set; }
}