namespace APP.Utils;

public static class OperationUtils
{
    public static Dictionary<string, List<(string Name, string Description, int Order)>> All()
    {
        return new Dictionary<string, List<(string, string, int)>>
        {
            {"OINTMENT", new List<(string, string, int)>
                {
                    ("Requisition of BMR to Quality Assurance (QA). Issue of BMR by QA.", "Managing requisitions and issuing Batch Manufacturing Records.", 1),
                    ("Indent of R.M. & P.M. Requisitions by Production as Per BMR.", "Requesting Raw Materials and Packing Materials.", 2),
                    ("Line Clearance. Dispensing of Raw Material & issue of Packing Materials in presence of Production, QA & warehouse person.", "Ensuring the line is clear for dispensing stock materials and issuing packing materials.", 3),
                    ("Equipment & Area Clearance by QA.", "Ensuring equipment and areas are ready and cleared for use.", 4),
                    ("Cream Preparation", "Preparation for the production process.", 5),
                    ("Sampling done by QA,Testing By QC and Released by QA", "Sampling and releasing products after QA approval and QC testing.", 6),
                    ("Line Clearance by QA for Ointment Manufacturing and Filling", "Clearing the line for manufacturing and filling operations.", 7),
                    ("Ointment Filling", "Filling ointments into containers.", 8),
                    ("In process Checking: Filled Weight, integrity checking.", "Checking filled weight and checking integrity during processing.", 9),
                    ("Overprinting & approval of Secondary Packing Materials by QA", "Ensuring secondary packing materials are properly overprinted and approved.", 10),
                    ("Line Clearance by QA Hand Packing Activity.", "QA clearance before initiating manual packing operations.", 11),
                    ("Final Packing.", "Completing the final packaging of the product.", 12),
                    ("F.P. Quarantine, Finished product Sampling and Release by QA. Testing by QC. Collection of Retain Samples by QA.", "Quarantining finished goods and conducting QC tests.", 13),
                    ("Inspection and release by QA", "Inspecting and releasing items for the next phase.", 14),
                    ("Finished Product Transfer to Finished Goods Store (FGS).", "Transferring finished goods to the storage area.", 15),
                    ("Dispatch by QA", "Dispatching finished goods to customers or destinations.", 16)
                }
            },
            {"SYRUP", new List<(string, string, int)>
                {
                    ("Requisition of BMR to Quality Assurance (QA). Issue of BMR by QA.", "Managing requisitions and issuing Batch Manufacturing Records.", 1),
                    ("Indent of R.M. & P.M. Requisitions by Production as Per BMR.", "Requesting Raw Materials and Packing Materials.", 2),
                    ("Line Clearance. Dispensing of Raw Material & issue of Packing Materials in presence of Production, QA & warehouse person.", "Ensuring the line is clear for dispensing stock materials and issuing packing materials.", 3),
                    ("Equipment & Area Clearance by QA.", "Ensuring equipment and areas are ready and cleared for use.", 4),
                    ("Product Preparation", "Preparation for the production process.", 5),
                    ("Sampling done by QA,Testing By QC and Released by QA", "Sampling and releasing products after QA approval and QC testing.", 6),
                    ("Line Clearance by QA for Liquid filling and Capping Activity.", "Clearing the line for liquid filling and capping operations.", 7),
                    ("Liquid Filling and Capping.", "Filling liquids and capping bottles or containers.", 8),
                    ("In process Checking: Filled Volume, Seal integrity checking.", "Checking filled volume and seal integrity during processing.", 9),
                    ("Overprinting & approval of Secondary Packing Materials by QA", "Ensuring secondary packing materials are properly overprinted and approved.", 10),
                    ("Line Clearance by QA for Hand Packing Activity.", "QA clearance before initiating manual packing operations.", 11),
                    ("Final Packing.", "Completing the final packaging of the product.", 12),
                    ("F.P. Quarantine, Finished product Sampling and Release by QA. Testing by QA. Collection of Retain Samples by QA.", "Quarantining finished goods and conducting QC tests.", 13),
                    ("F.P. Inspection and release by QA", "Inspecting and releasing finished products for the next phase.", 14),
                    ("Finished Product Transfer slip issued by approved production person to Finished Goods Quarantine Store (FGQS)", "Transferring finished goods to the storage area.", 15),
                    ("Dispatch of Finished Product", "Dispatching finished goods to customers or destinations.", 16)
                }
            },
            {"BETA", new List<(string, string, int)>
                {
                    ("Requisition of BMR (Batch Manufacturing Record) to Quality Assurance (QA). ii. Issue of BMR by QA.", "Managing requisitions and issuing Batch Manufacturing Records.", 1),
                    ("Indent of Raw Material (R.M.) and Packing Material (P.M.) Requisitions by Production as per BMR.", "Requesting Raw Materials and Packing Materials.", 2),
                    ("Dispensing of Raw Material and issue of Packing Materials in the presence of Production, QA, and a warehouse person.", "Dispensing materials for production.", 3),
                    ("Equipment & Area Clearance by QA.", "Ensuring equipment and areas are ready and cleared for use.", 4),
                    ("Product Preparation", "Preparation for the production process.", 5),
                    ("", "Empty stage.", 6),
                    ("Intermediate granules Sampling and Release by QA. Testing is done by QC.", "Sampling and releasing intermediate granules after QA approval and QC testing.", 7),
                    ("Line Clearance by QA for filling and Capping Activity.", "Clearing the line for filling and capping operations.", 8),
                    ("Filling and Capping", "Filling and capping the product.", 9),
                    ("In process Checking: • Filled Weight variation •Seal integrity checking.", "Checking filled weight and seal integrity during processing.", 10),
                    ("Overprinting & approval of Secondary Packing Materials by QA.", "Ensuring secondary packing materials are properly overprinted and approved.", 11),
                    ("Line Clearance by QA for Blister Packing Activity.", "QA clearance for blister packing.", 12),
                    ("Primary Packing activity.", "Performing the primary packaging.", 13),
                    ("Leak test checking.", "Checking for leaks in the primary packaging.", 14),
                    ("Secondary packing activity.", "Performing the secondary packaging.", 15),
                    ("Line Clearance by QA for Secondary Packing Activity.", "QA clearance before secondary packing.", 16),
                    ("F. P. Quarantine, Finished product Sampling, Testing of packed products by QC. Collection of Retain Samples by Q. A", "Quarantining finished goods and conducting QC tests.", 17),
                    ("Finished Product Transfer slip issued by approved personnel to the Finished Goods Quarantine Store (FGQS).", "Transferring finished goods to the storage area.", 18),
                    ("Inspection and release by QA.", "Inspecting and releasing items for the next phase.", 19),
                    ("Dispatch.", "Dispatching finished goods to customers or destinations.", 20)
                }
            }
        };
    }
}