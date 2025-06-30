using DOMAIN.Entities.Base;
using DOMAIN.Entities.Routes;

namespace APP.Utils;

public static class OperationUtils
{
    public static IEnumerable<(string Name, string Description, int Order, OperationAction? Action)> All()
    {
        return new List<(string, string, int, OperationAction?)>
        {
            ("Requisition & Issuing of BMR", "Managing requisitions and issuing Batch Manufacturing Records.", 1, OperationAction.BmrAndBprRequisition),
            ("Requisition & Issue of Stock Materials", "Requesting and managing stock or purchases.", 2, OperationAction.StockRequisition),
            ("Line Clearance: Dispensing of Stock Materials", "Ensuring the line is clear for dispensing stock materials.", 3, OperationAction.FullReturn),
            ("Equipment Clearance", "Ensuring equipment is ready and cleared for use.", 4, OperationAction.FullReturn),
            ("Production Preparation", "Preparation for the production process.", 5, null),
            ("Sampling & Release by QA: Testing by QC", "Sampling and releasing products after QA approval and QC testing.", 6, null),
            ("Line Clearance For Liquid Filling & Capping Activity", "Clearing the line for liquid filling and capping operations.", 7, OperationAction.AdditionalStockRequest),
            ("Liquid Filling & Capping", "Filling liquids and capping bottles or containers.", 8, OperationAction.AdditionalStockRequest),
            ("In Process Checking: Fill Volume & Seal Integrity Checking", "Checking fill volume and seal integrity during processing.", 9, OperationAction.AdditionalStockRequest),
            ("Overprinting & Approval of Secondary Packing Materials", "Ensuring secondary packing materials are properly overprinted and approved.", 10, OperationAction.AdditionalStockRequest),
            ("Line Clearance by QA For Hand Packing Activity", "QA clearance before initiating manual packing operations.", 11, OperationAction.AdditionalStockRequest),
            ("Final Packing", "Completing the final packaging of the product.", 12, OperationAction.FinalPackingOrPartialReturn),
            ("Finished Goods Transfer To Storage Room", "Transferring finished goods to the storage area.", 13, OperationAction.FinishedGoodsTransferNote),
            ("Inspection & Release", "Inspecting and releasing items for the next phase.", 14, null),
            ("Quarantine Finished Goods & QC Testing", "Quarantining finished goods and conducting QC tests.", 15, OperationAction.FinishedGoodsTransferNote),
            ("Dispatch", "Dispatching finished goods to customers or destinations.", 16, OperationAction.Dispatch)
        };
    }
}

