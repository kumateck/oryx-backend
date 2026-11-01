namespace DOMAIN.Entities.Products.Production;

/// <summary>What repairing a batch's procedure changed.</summary>
public class ProductionProcedureRepairResult
{
    public int StepsUpdated { get; set; }
    public int StepsAdded { get; set; }
    public int StepsRemoved { get; set; }
    /// <summary>Steps already started that the procedure no longer has: they are kept as they are.</summary>
    public List<string> StepsKept { get; set; } = [];
}
