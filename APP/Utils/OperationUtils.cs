namespace APP.Utils;

public static class OperationUtils
{
    public static IEnumerable<(string Name, string Description)> All()
    {
        return new List<(string Name, string Description)>
        {
            ("Requisition & Issuing of BMR", "Managing requisitions and issuing Batch Manufacturing Records."),
            ("Purchase/Stock Requisition", "Requesting and managing stock or purchases."),
            ("Line Clearance: Dispensing of Stock Materials", "Ensuring the line is clear for dispensing stock materials."),
            ("Equipment Clearance", "Ensuring equipment is ready and cleared for use."),
            ("Production Preparation", "Preparation for the production process."),
            ("Sampling & Release by QA: Testing by QC", "Sampling and releasing products after QA approval and QC testing."),
            ("Line Clearance For Liquid Filling & Capping Activity", "Clearing the line for liquid filling and capping operations."),
            ("Liquid Filling & Capping", "Filling liquids and capping bottles or containers."),
            ("In Process Checking: Fill Volume & Seal Integrity Checking", "Checking fill volume and seal integrity during processing."),
            ("Finished Goods Transfer To Storage Room", "Transferring finished goods to the storage area."),
            ("Inspection & Release", "Inspecting and releasing items for the next phase."),
            ("Quarantine Finished Goods & QC Testing", "Quarantining finished goods and conducting QC tests."),
            ("Dispatch", "Dispatching finished goods to customers or destinations.")
        };
    }
}
