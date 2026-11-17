# Issuing packaging materials as they come: overs and shortages

**Status: proposed, revised 17 November 2026 with the user's clarifications.** Not implemented.

## The need

Packaging materials come in containers the store cannot open: rolls, cartons, bundles. A requisition for 18.75 (in the material's unit) may be met with 20 from two sealed packs of 10, or with 18 from two packs of 9. What leaves the store is then **more or less than requested**.

Today Oryx issues and records exactly the requested quantity, so the voucher, the stock and Final Packing's reconciliation all disagree with what was physically handed over.

## Rules confirmed by the user

1. **Packaging materials only.** Raw materials keep exact issuing.
2. **The requirement is never rounded.** The requisition and BOM keep the exact quantity needed.
3. **The store records how much it actually issues** on each packaging line, over or under the requested quantity.
4. **The warehouse supervisor makes that call.** The suggestion is the nearest whole container: 18.75 from packs of 10 suggests 20, and from packs of 9 suggests 18. The supervisor may issue the other whole-container quantity, or any quantity the sealed packs allow.
5. **Leftovers come back through Final Packing's reconciliation.** Final Packing raises a **material return note** with a **partial return** for each packaging material left over. The reconciliation must be checked against those partial returns.
6. **Any difference from the requirement needs a reason** (confirmed 17 November 2026). Issuing **more or less** than required, the supervisor writes why: "sealed packs of 10" for an over, for example. The reason is stored on the line and printed on the voucher beside the over or shortage.

## How it would work

### Issuing (warehouse)

- **Default:** each packaging line's **issued quantity** starts at the requested quantity, split across the batches set aside as today.
- **Changing it:** a supervisor can change a batch line's issued quantity before issuing.
  - **The suggestion:** the nearest whole container, from the batch's `QuantityPerContainer` recorded at its GRN. Batches whose container size is unknown, such as migrated stock recorded as one container, get no suggestion.
  - **The limit:** a batch can give up to its stock on that shelf that is not reserved for anything else.
- **Recorded on the line:** requested, issued, the difference (over or under), who changed it and when, and the reason. The server refuses any over- or under-issue without a reason.
- **The voucher** prints Qty required and Qty issued, and marks each over or under.
- **Stock:** the issued quantity, not the requested one, moves to production's warehouse. The reservation is settled for the issued quantity, and anything extra is taken from free stock on the same batch.

### Final Packing (production)

- The materials reconciliation already works out, per packaging material:

  ```
  received = issued + extra deliveries
  returned = received − (packed + rejected + sampled)
  ```

  On save, it posts the leftovers to `POST /api/v1/production-schedule/return-after-production` (`returnedMaterials` in `modules/activities/lib/final-packing.ts`). `ProductionScheduleRepository.ReturnLeftOverStockAfterProductionEnds` then creates a `MaterialReturnNote` for the batch, with a `MaterialReturnNotePartialReturn` per material, returning to the store it came from.
- **Reconciliation check:** for each packaging material, the partial returns on the batch's return notes must equal the reconciliation's "returned", that is issued + extra deliveries − packed − rejected − sampled. A difference is flagged on the reconciliation and on the return note.
- **Batch attribution:** a partial return is currently booked against the *first* reserved batch of that material. When an over-issue came from a particular batch, the leftover should go back to that batch: the last one issued from. This needs fixing alongside.
- **The one change:** "received" becomes the **issued** quantity, not the requested one. Overs then come back as leftover automatically: issued 20, packed 18.75, returned 1.25.
- **A shortage** (issued 18 of 18.75): production asks for the rest through the existing extra-material request. Extra deliveries are already added to "received". If none is delivered, the shortage shows in the reconciliation's percentage loss with its reason, as any loss does today.

## Permissions

- Issuing keeps `CanIssuePackagingMaterialRequisitions`.
- **Changing a line's issued quantity** gets its own key, `CanAdjustPackagingMaterialIssueQuantity`.
  - It is a separate supervisor task, so it doesn't borrow the issue key.
  - It is granted to nobody until an administrator assigns it to the warehouse supervisor role.
  - The server re-checks it, inside the transaction, before writing.

## Affected code

- **Backend:**
  - the requisition item, which gains the issued quantity, who changed it and when, plus a migration;
  - `StockIssueQuantityValidator`, which allows the issued quantity on packaging lines within stock;
  - `IssueStockRequisition`, which moves the issued quantity;
  - the requisition DTOs.
- **Desktop:**
  - the issue screen, which gets an editable issued quantity for supervisors, the suggestion and the over/under;
  - the voucher;
  - Final Packing's packing materials, where "received" takes the issued quantity, with the partial-return check;
  - `ReturnLeftOverStockAfterProductionEnds`, which books each leftover to the batch it was over-issued from.

## Open points

- Is there a limit on how far over the requirement a supervisor may issue, for example at most one extra container?
