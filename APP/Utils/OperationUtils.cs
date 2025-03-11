namespace APP.Utils;

public static class OperationUtils
{
    public static IEnumerable<(string Name, string Description, int Order)> All()
    {
        return new List<(string Name, string Description, int Order)>
        {
            ("Requisition & Issuing of BMR", "Managing requisitions and issuing Batch Manufacturing Records.", 1),
            ("Requisition & Issue of Stock Materials", "Requesting and managing stock or purchases.", 2),
            ("Line Clearance: Dispensing of Stock Materials", "Ensuring the line is clear for dispensing stock materials.", 3),
            ("Equipment Clearance", "Ensuring equipment is ready and cleared for use.", 4),
            ("Production Preparation", "Preparation for the production process.", 5),
            ("Sampling & Release by QA: Testing by QC", "Sampling and releasing products after QA approval and QC testing.", 6),
            ("Line Clearance For Liquid Filling & Capping Activity", "Clearing the line for liquid filling and capping operations.", 7),
            ("Liquid Filling & Capping", "Filling liquids and capping bottles or containers.", 8),
            ("In Process Checking: Fill Volume & Seal Integrity Checking", "Checking fill volume and seal integrity during processing.", 9),
            ("Finished Goods Transfer To Storage Room", "Transferring finished goods to the storage area.", 10),
            ("Inspection & Release", "Inspecting and releasing items for the next phase.", 11),
            ("Quarantine Finished Goods & QC Testing", "Quarantining finished goods and conducting QC tests.", 12),
            ("Dispatch", "Dispatching finished goods to customers or destinations.", 13)
        };
    }
}
