namespace DOMAIN.Entities.Reports;

public class QaDashboardDto
{
    public int NumberOfBmrRequests { get; set; }
    public int NumberOfPendingBmrRequests { get; set; }
    public int NumberOfApprovedBmrRequests { get; set; }
    public int NumberOfRejectBmrRequests { get; set; }
    public int NumberOfAnalyticalTestRequests { get; set; }
    // public int NumberOfPendingAnalyticalTestRequests { get; set; }
    // public int NumberOfApprovedAnalyticalTestRequests { get; set; }
    public int NumberOfExpiredAnalyticalTestRequests { get; set; }
    
    public int NumberOfApprovals { get; set; }
    public int NumberOfPendingApprovals { get; set; }
    // public int NumberOfExpiredApprovals { get; set; }
    public int NumberOfRejectedApprovals { get; set; }
    
    public int NumberOfManufacturers { get; set; }
    public int NumberOfNewManufacturers { get; set; }
    // public int NumberOfApprovedManufacturers { get; set; }
    public int NumberOfExpiredManufacturers { get; set; }
    
    public int NumberOfProducts { get; set; }
    public int NumberOfRawMaterials { get; set; }
    public int NumberOfPackingMaterials { get; set; }
}