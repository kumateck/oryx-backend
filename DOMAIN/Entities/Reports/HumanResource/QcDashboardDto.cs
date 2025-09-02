namespace DOMAIN.Entities.Reports.HumanResource;

public class QcDashboardDto
{
    public int NumberOfStpRawMaterials  { get; set; }
    public int NumberOfStpPackingMaterials  { get; set; }
    public int TotalMaterialStp => NumberOfStpRawMaterials + NumberOfStpPackingMaterials;
    public int NumberOfStpProducts {get; set; }
    public int NumberOfMaterialAnalyticalRawData { get; set; }
    public int NumberOfMaterialAnalyticalPackingData { get; set; }
    public int NumberOfAnalyticalRawData => NumberOfMaterialAnalyticalRawData + NumberOfMaterialAnalyticalPackingData;
    
    public int NumberOfIntermediateProductAnalyticalRawData { get; set; }
    public int NumberOfBulkProductAnalyticalRawData { get; set; }
    public int NumberOfFinishedProductAnalyticalRawData { get; set; }
    public int TotalProductAnalyticalData => NumberOfIntermediateProductAnalyticalRawData 
                                             + NumberOfBulkProductAnalyticalRawData
                                             + NumberOfFinishedProductAnalyticalRawData;
    public int NumberOfBatchTestCountRawMaterials { get; set; }
    
    // public int NumberOfBatchTestRawMaterials { get; set; }
    public int NumberOfBatchTestPendingRawMaterials { get; set; }
    public int NumberOfBatchTestApprovedRawMaterials { get; set; }
    public int NumberOfBatchTestRejectedRawMaterials{ get; set; }
    
    // public int NumberOfBatchTestProducts { get; set; }
    // public int NumberOfBatchTestPendingProducts { get; set; }
    // public int NumberOfBatchTestApprovedProducts { get; set; }
    // public int NumberOfBatchTestExpiredProducts { get; set; }
    
    // public int NumberOfApprovals {get; set; }
    // public int NumberOfPendingApprovals { get; set; }
    // public int NumberOfCompletedApprovals { get; set; }
    // public int NumberOfRejectedApprovals { get; set; }
    
    public int NumberOfRawMaterialSpecifications { get; set; }
    public int NumberOfPackingMaterialSpecifications { get; set; }
    public int TotalMaterialSpecifications => NumberOfRawMaterialSpecifications + NumberOfPackingMaterialSpecifications;
    public int NumberOfIntermediateProductSpecifications { get; set; }
    public int NumberOfBulkProductSpecifications { get; set; }
    public int NumberOfFinishedProductSpecifications { get; set; }
    
    public int TotalProductSpecifications => NumberOfIntermediateProductSpecifications 
                                             + NumberOfBulkProductSpecifications
                                             + NumberOfFinishedProductSpecifications;
}