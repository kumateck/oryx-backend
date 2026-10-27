# Price-unit guards (2026-10-23)

## Why

EPRC-69 showed an order value of $780,000,000 for 650 kg at $1.20. Its line had price 1.2 and **no price
unit**. Without a unit the server costs a line as price x stored quantity, and a mass quantity is stored in mg:
1.2 x 650,000,000.

How the unit was lost: the quotation response was entered on 2026-07-29 with an empty price unit. Responses
only had to carry a unit from 2026-08-06 (9f123ccf), and awards only copied it onto the order line from
2026-09-02 (c56865ae). The read-time fallback (`PriceUoMExtensions`) found the awarded quotation line, but that
line itself was blank.

## Data repair

`backfill_blank_price_uom_kg.sql` (run by hand; preview first, logs every change to `_PriceUoMKgBackfill`,
with an undo section):

- mass lines (mg, g, kg) with a blank unit get `kg`, including unpriced (losing/unused) quotes;
- counted lines (pcs, pack, ...) with a blank unit get their own unit. A count is stored as entered, so
  price x quantity was already right, and no amount changes;
- left blank and listed for a person: volume/length lines (per ml or per L cannot be told apart) and
  prices above 0 but under 0.01 (almost certainly per g or mg).

Saved order totals (`TotalFobValue`, `TotalCifValue`, `AmountInFigures`) are not recalculated by the repair.

## Guards

One rule, `UomConverter.CanPrice(lineUnit, priceUnit)`: the price unit is given **and** converts to the line's
unit. Every refusal uses `PriceUoMErrors.Unusable` (code `Price.PriceUoM`) with a readable message.

| Path | Change |
|---|---|
| Quotation response (`ReceiveQuotationFromSupplier`) | every submitted line checked before anything is saved; unit trimmed; no longer marks untouched lines as updated |
| Award (`ProcessQuotationAndCreatePurchaseOrder`) | the awarded quotation's unit must be usable, not just non-blank; a failed order creation is returned instead of ignored |
| Direct order creation (`CreatePurchaseOrder`) | every line checked |
| Revision (`RevisePurchaseOrder`) | added lines and updated lines (new unit, or the kept one) checked before any revision is applied |
| Check (`CheckPurchaseOrder`) | refuses an order with a line that cannot be costed; a blank line takes the awarded quotation's unit (as the order pages show it) and keeps it |
| Save guard (`ApplicationDbContext.ValidatePriceUoM`) | **on by default** (`Procurement:EnforcePriceUoM` true in code and appsettings); now judges only writes of a price or its unit, so a status change on a legacy blank line does not fail an unrelated action |

Desktop app (oryx-live-desktop): a purchase order line with no usable price unit shows "Cannot be costed" and the
order has no total. The server's raw cost is used only for counted units, where it is correct.

Not covered: general-stores vendor quotations (`InventoryProcurementRepository.Memos`) are a separate module.

## Tests

- `tests/APP.Tests/Repository/PriceUoMGuardTests.cs` (18 cases): the rule, quotation response refusal and legacy
  neighbour, direct creation, revision update/add, check refusal and quotation fallback, save guard on writes
  and not on unrelated changes.
- Live: `orders.test.ts` "never shows the server's raw cost for a line it cannot cost".

## Deploy order

1. Run the backfill (preview, then the transaction), and settle the lines it lists.
2. Deploy the backend. No migration. Orders with a line that still cannot be costed will be refused at check
   with a message naming the material.
