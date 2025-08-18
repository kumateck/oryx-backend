namespace DOMAIN.Entities.Reports.HumanResource;

public class QcDashboardDto
{
    public int NumberOfStpRawMaterials  { get; set; }
    public int NumberOfStpProducts {get; set; }
    public int NumberOfAnalyticalRawData { get; set; }
    public int NumberOfBatchTestCountRawMaterials { get; set; }
    
    // public int NumberOfBatchTestRawMaterials { get; set; }
    public int NumberOfBatchTestPendingRawMaterials { get; set; }
    public int NumberOfBatchTestApprovedRawMaterials { get; set; }
    public int NumberOfBatchTestRejectedRawMaterials{ get; set; }
    
    // public int NumberOfBatchTestProducts { get; set; }
    // public int NumberOfBatchTestPendingProducts { get; set; }
    // public int NumberOfBatchTestApprovedProducts { get; set; }
    // public int NumberOfBatchTestExpiredProducts { get; set; }
    
    public int NumberOfApprovals {get; set; }
    public int NumberOfPendingApprovals { get; set; }
    // public int NumberOfCompletedApprovals { get; set; }
    public int NumberOfRejectedApprovals { get; set; }
}