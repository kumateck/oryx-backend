# Material stock trace, full return and pending certificates

Local implementation; no customer stock has been changed and no release has been deployed by this work.

## Stock accounting and reports

`GET /api/v1/material-stock-trace/{raw|packaging}/{materialId}` reads one repeatable-read snapshot in the caller's department. The `/print` suffix requires the corresponding print permission and returns the complete snapshot, including all bin-card entries. The desktop uses the existing report components for five separate reports (stock by batch, bin card, reserved, production floor, consumed) and one combined material-details document. Material identity, department, units, period and snapshot time appear first. Printing never posts inventory.

- Opening store balance + recorded receipts − outward movements = closing bin-card balance.
- Reservations are a subset of shelf stock, not an additional stock bucket.
- Production-floor stock is supported by movements less confirmed consumption and received returns. An activated full return stays on the floor, marked as awaiting warehouse receipt, until the store shelves it.
- Consumed stock comes from consumption events, never from issue alone or an archived reservation alone. New production consumption records the exact allocation and production batch.
- Incoming transfers awaiting shelf receipt are separate from shelf stock.
- Ledger/shelf differences, inconsistent floor allocations and unresolved units remain visible. A report never invents quantities to hide a historical gap.

Transfer issue now posts its source bin-card outward movement atomically with the stock deduction. Receipt posts its receiving bin-card inward movement atomically with shelf allocation. Search accepts material name/code as well as batch and document references.

## Full return

The existing `POST /api/v1/production-schedule/return-before-production` now verifies current issue evidence for each stock kind. It does not assume that raw and packaging were issued together, or that packaging was issued at a particular earlier step.

Before consumption, an authorized operator in the owning production department can cancel the batch and activate a full-return note containing only issued, unconsumed allocations. Unissued holds are released and their unissued requisitions retired; no shelf quantity is added for them. Released old repair allocations are excluded. Original issue records stay recorded. The BMR/BPR are retired and the activated return note releases their batch number under the existing batch-number rules. The run becomes cancelled rather than being reset to a new production run.

The service refuses a run which has entered its configured consumption step (`Product Preparation`, or `Final Packing` if earlier in that procedure), consumed/deleted allocations, consumption events, downstream quality/packing/finished-goods records, existing returns, ambiguous stock evidence or a mismatched floor balance. Physical consumption is an irreversible boundary for full return. The check uses operation identity/name as the current consumption code does, rather than assuming every procedure numbers that step as 5.

The warehouse receives each returned lot through the existing supply/shelf-allocation action. It checks the note, issued quantity, material kind, department, destination warehouse, floor balance and shelf unit. The entire outstanding lot quantity must be assigned. One transaction adds the shelf stock and bin-card receipt, moves stock off the floor and marks the return lines received. Total and consumed quantities do not change; assigned quantity follows the restored shelf stock. Repeat receipt is refused. A return note cannot be manually completed before all its stock is received.

The desktop exposes Full return on the batch board before consumption. The quantity-correction repair remains separate: if issued and current-master quantities agree, its adjustment is zero and Apply stock repair stays disabled with that explanation.

## L014K029 evidence

Read-only customer check on 25 November 2026: Metronidazole 125mg, NL-014; packaging issue PM/N/26/041 was issued on 24 November at 15:43 UTC. Raw requisition RM/N/26/097 remained pending. The 14 raw allocations are holds, not stock to receive as returns.

| Packaging code | Issued quantity (piece) |
| --- | ---: |
| PMB001 | 30,600 |
| PMC137 | 30,700 |
| PMF108 | 4 |
| PMI038 | 30,600 (23,160 + 7,440 across two lots) |
| PML030 | 32,000 |
| PML272 | 530 |
| PMR004 | 30,600 |
| PMR006 | 30,600 |
| PMS013 | 505 |
| PMT001 | 15 |

PMB005's old 30,300-piece allocation was already released and must not be returned again. The screenshot's quantity repair is disabled because every current line has zero adjustment. The intended cancellation uses Full return and subsequent warehouse receipt; no live cancellation was performed during this investigation.

## Glycerine RMG006

Read-only reproduction found two Syrup lots, GB26060008 and GB25040002, physically on S/RM/G/09/B. Lot units are ml; Syrup's reporting unit is mg, with verified density 1.261 g/ml. At the diagnostic snapshot, current source code returned 21,995.982 kg on shelves, 2,833.725 kg reserved and 19,162.257 kg free. The two batch rows loaded and their quantities added to the approved total.

The existing reserved query raised `OverflowException` because PostgreSQL's full-precision sums/subtraction of density fractions could exceed the scale accepted by the decimal reader. The replacement reads individual quantities, converts shelves and holds into one department unit and sums with the same decimal stock rule as approved stock. The read-only retest returned all 22 live reservations, totalling 2,833.725 kg. Source records and their exact saved readings remain unchanged. These diagnostics do not prove that the local desktop's running backend matches this source version; the captured initial server log contained notification retry warnings, not the failing stock request.

## COA results and editing

The shared result renderers show only the final average value and unit when an average exists, including worksheets, certificate previews, approvals and issued-document rendering. They omit sample result lines and the Average label. The specification shows the matching range once. Saved inputs/results and approved snapshots remain intact.

The effective reported verdict is shared by reads, certificate generation, resubmission and approval. A failed or unresolved conversion cannot be overridden as complying. Worksheet remarks now use those decisions rather than searching only for the words “does not comply”.

Edit COA appears directly on the results page and approval detail for a pending certificate's original preparer with the matching edit key. Other preparers receive an ownership explanation; assigned approvers can request correction. Approved certificates remain immutable.

## Permissions and deployment

New keys, each registered once under Warehouse / ApprovedMaterials:

- CanViewRawMaterialStockTrace and CanViewPackagingMaterialStockTrace protect the view endpoints.
- CanPrintRawMaterialStockTrace and CanPrintPackagingMaterialStockTrace protect complete print snapshots.

`GrantMaterialStockTraceViews` copies the matching approved-material view grants and access types to these new view/print tasks. Transfer receipt uses the existing CanAssignRawMaterialStockTransfers / CanAssignPackagingMaterialStockTransfers keys; `GrantStockTransferReceiptAssignments` copies existing matching issue grants and access types. Full return keeps CanReturnProductionMaterials; receipt keeps the stored kind's shelf-assignment key; completion keeps CanCompleteMaterialReturnNote. No role-name bypass is added.

Apply the new consumption-provenance schema migration and both permission migrations before running this backend/desktop pair. The existing pending-certificate migrations and grants must already be present. No applied migration was edited. Regenerate the desktop contract through the reviewed importer; the stock-trace addition preserves the previous typed contract after stripping only its four operations and owned schemas.

Historical transfer bin-card gaps identified by the read-only investigation still require a separately reviewed correction of missing ledger entries. New posting prevents further gaps; deploying it alone does not backfill historical rows or establish that every customer ledger is already reconciled.

## Department board ordering

GET and PUT `/api/v1/production-board-order/{departmentId}` use the specific Settings / Procedures permissions `CanViewDepartmentProductionBoardOrder` and `CanReorderDepartmentProductionBoard`. Migration `20261125210000_GrantProductionBoardOrdering` copies existing `CanViewOperations` and `CanEditOperation` grants, preserving access types and existing target grants. No roles gain an unrelated task.

The configuration saves all currently linked operation IDs in one transaction with fresh grants, a complete-set check and a version of names, links and ordering. Stale requests fail for review. Only `Operation.Order` changes; recorded batch step order is preserved. The existing board query already uses operation order. The desktop exposes the existing draggable table and an optional Tablet preset covering the supplied 21 operation names, leaving unmatched operations visible. This is local implementation; no live department order was changed.
