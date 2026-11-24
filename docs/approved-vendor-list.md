# Approved vendor list

Date: 2026-11-19. QA's Approved Manufacturer List for Raw Materials (Annexure QAD/SOP/007/A03-06), as a document with revisions.

## Model

- `ApprovedVendorLists`: Annexure No., Revision No., Supersedes, the revision it was revised from, and a status. Status is Pending (in review or awaiting approval), Approved (the current list), Superseded or Rejected. A filtered unique index allows at most one Pending and one Approved list.
- `ApprovedVendorListLines`: material (raw only), manufacturer, supplier, Qty/Pack (kg), approval month and validity month. Months are stored as their first day. A line is valid through the last day of its validity month.
- `ApprovedVendorListApprovals`: its approval stages.

Lines reuse the material ↔ manufacturer (`ManufacturerMaterials`) and supplier ↔ manufacturer ↔ material (`SupplierManufacturers`) mappings. A new manufacturer or supplier name creates the record (`CanCreateManufacturer` / `CanCreateVendor`). A missing mapping is added (`CanUpdateManufacturerDetails` / `CanUpdateVendorDetails`, or the create key for a new record). Grants are reloaded before the write.

## Flow

1. **Prepare** (`POST approved-vendor-list`, first list only) or **revise** (`POST approved-vendor-list/revise`, from the current approved list). A revision's Supersedes is the current revision number, and its revision number must be higher.
2. Saving calls `CreateInitialApprovalsAsync("ApprovedVendorList")`. The type is registered in `ReviewDocumentTypes` as always reviewed, so the document review opens (Checked by).
3. **Correct** (`PUT approved-vendor-list/{id}`) while the review is awaiting review or returned. The edit is recorded on the review. A returned list is sent back for review with the edit key.
4. Confirming the review starts the approval engine (`ApprovedVendorListApprovalHandler`). Without a configured workflow the list is approved automatically. The final approval makes it Approved and the revision it came from Superseded. If the current list changed in the meantime, the approval is refused.
5. A rejected list stays rejected. Prepare a new revision instead.
6. **Delete** (`DELETE approved-vendor-list/{id}`) is available only for a Superseded revision and requires `CanDeleteApprovedVendorList`. It removes that revision from normal list, detail and print reads, recording the deletion time and actor. Its lines, review and approval history, and links from later revisions are retained. Current, Pending and Rejected lists cannot be deleted. The current revision continues to govern procurement.

## Procurement blocking (`ApprovedVendorRules`)

Owner's decisions (2026-11-19):

- A material with no line on the current approved list is unrestricted.
- A listed material may use only a supplier, or manufacturer, on one of its lines that has not lapsed.
- A lapsed or missing line blocks that supplier or manufacturer for that material only.

| Where | Checks |
| --- | --- |
| Sourcing create and update (`RequisitionRepository`) | supplier per material |
| Recording quotation prices (`SupplierQuotationPriceRecorder`) | the quotation's supplier for each priced material |
| Awarding quotations (`ProcessQuotationAndCreatePurchaseOrder`), all checked before any order is created | supplier per material |
| Purchase order create, and revision AddItem | the order's supplier per material |
| PO invoice create and update | each manufacturer against the order's supplier and materials |
| Shipment invoice create and update | manufacturer per material through the order's supplier |

Errors are validation errors that name the supplier or manufacturer and the material. They say whether the line is missing or lapsed, and when it lapsed.

## Permissions

| Key | Endpoints | Granted by migration to |
| --- | --- | --- |
| `CanViewApprovedVendorList` | list, read, options, mappings; review read | nobody |
| `CanCreateApprovedVendorList` | create | nobody |
| `CanEditApprovedVendorList` | correct; review resubmit | nobody |
| `CanDeleteApprovedVendorList` | delete a Superseded revision | nobody; assign explicitly (Quality Assurance → Approved Vendor List) |
| `CanReviseApprovedVendorList` | revise | nobody |
| `CanReviewApprovedVendorList` / `CanReturnApprovedVendorList` | review confirm / return | nobody |
| `CanApproveApprovedVendorList` / `CanRejectApprovedVendorList` | approval decide (`DecisionKey`) | nobody |
| `CanPrintApprovedVendorList` | print | nobody |
| `CanViewApprovedVendorEligibility` (Procurement) | eligibility | holders of the sourcing, quotation response, vendor pricing selection, PO create/revise, PO invoice and shipment invoice create/edit keys (`GrantApprovedVendorEligibility`) |
