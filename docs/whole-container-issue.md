# Issuing materials in whole containers

**Status: proposed (17 November 2026).** Not implemented. To be confirmed with the user before work starts.

## The need

Stores cannot open a container: only production may. When a requisition asks for 18.75 kg of a material held in 10 kg boxes, the store issues two whole boxes, **20 kg**. Production uses 18.75 kg and returns the 1.25 kg left over.

Today Oryx reserves and issues the exact requested quantity, so the voucher says 18.75 kg while 20 kg physically leaves the store.

## Rules given by the user

1. **Applies to raw and packaging materials.**
2. **Nothing is rounded in what is needed.** The requisition and the BOM keep the exact quantity, 18.75 kg. Only the quantity issued is counted in whole containers.
5. **The store issues the nearest whole number of containers** (confirmed 17 November 2026):
   - 18.75 kg from 10 kg boxes is 1.875 boxes, so **2 boxes, 20 kg**: production returns 1.25 kg.
   - 18.75 kg from 9 kg containers is 2.08 containers, so **2 containers, 18 kg**: 0.75 kg less than required.
3. **Only whole, sealed containers are issued.** For now the store never issues part of a container.
4. **Production returns the leftover** through the material return note's existing partial return.

## How it would work

### Container size

Every received batch already records `MaterialBatch.QuantityPerContainer` and `NumberOfContainers` from its GRN. That is the container size used here, in the batch's unit.

A batch whose container size is **unknown** is issued exactly as today. That covers:
- a size of zero or less;
- stock created by **migration or a stock top-up**, which records the whole quantity as one container: `QuantityPerContainer = total`, `NumberOfContainers = 1`, in `StockMigrationRepository` and `MaterialRepository`. Treating that as a real container would force the store to issue the entire batch.

This needs an explicit marker on the batch, not a guess. Migration rows can be recognised by their source.

### Reserving (setting aside)

`ProductionMaterialAllocation` reserves batches soonest-expiry first, as now. For each batch it reserves whole containers instead of an exact amount:

```
containers from a batch = round(still needed ÷ container size) to the nearest whole container, limited to the sealed containers available
reserved from the batch = containers × container size
```

Examples:
- 18.75 kg from 10 kg boxes: two boxes, 20 kg, are reserved, and **1.25 kg is expected back**.
- 18.75 kg from 9 kg containers: two containers, 18 kg, are reserved, and the line is **0.75 kg under** the requirement.

- **Sealed containers available** on a shelf are `floor(shelf quantity ÷ container size)`. Oryx does not track individual containers, so a remainder smaller than a container is treated as an already-opened container and is not issued.
- When the sealed containers run out on one batch, the next batch is used. On the last batch only enough whole containers are taken to cover the need.
- When the sealed containers cannot cover the need at all, the requisition shows the shortfall, as it does today. It never falls back to opening a container.

### Issuing

- `StockIssueQuantityValidator` changes from "reserved equals requested" to:
  - each batch's reserved quantity is a whole number of its containers (or exact, for an unknown container size);
  - the total is the nearest whole number of containers to the request, so it differs from the request by at most half a container of the last batch used.
- The full reserved quantity, 20 kg, moves to production's warehouse, as stock moves today.
- The requisition line records **required 18.75 kg**, **issued 20 kg (2 × 10 kg)** and **expected back 1.25 kg**, or, when it is rounded down, **issued 18 kg (2 × 9 kg)** and **under by 0.75 kg**.

### The voucher and issue screen

They show the containers next to each batch, for example `2 × 10 kg = 20 kg`, and the expected leftover under the material: "Production returns 1.25 kg".

### Returning the leftover

When production raises the batch's material return note, the expected leftover is offered as a **partial return** of that material, to the store it came from (`MaterialReturnNotePartialReturn`).

The BMR material reconciliation reads: issued − used − returned = 0.

## Open points to confirm

- **Rounding down leaves the batch short** (18 kg issued for 18.75 kg). How production makes up the 0.75 kg is not yet decided:
  - an accepted variance recorded against the batch;
  - a top-up issued from an already-opened container;
  - a separate requisition.
- **Exactly half a container**, for example 15 kg from 10 kg boxes, rounds up to the next container unless decided otherwise.
- Should production be able to keep a leftover for the next batch of the same product, or must it always be returned?
- Container size per batch comes from its GRN. Should materials also carry a default container size for batches received without one?

## Affected code

- `ProductionMaterialAllocation` (reserving)
- `StockIssueQuantityValidator`
- `RequisitionRepository.IssueStockRequisition`
- material return notes
- stock requisition DTOs, which need the issued and expected-back quantities
- the desktop issue screen and voucher (`modules/stock-issue`)
- the production return-materials page

## Permissions

No new keys are expected. Issuing keeps the existing raw and packaging issue keys, and returns keep theirs.
