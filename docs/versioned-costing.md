# Versioned costing (2026-11-21)

Costing price lists follow the QA Approved Vendor List revision pattern. A revision is a division-wide document containing independent product/packing-style price lines. The desktop uses the existing FormWizard, shared tables, review panel and print sheet; the root route is Finance → Costing.

## State, approval and effective date

A document starts Draft, then Submit opens the existing review workflow. Reviewers may return it for correction and authors with the submit permission may resubmit. Confirmed review opens the configured human approval stages. At least one required approval stage must exist before submission. No absent workflow automatically approves a price list.

Draft/pending editable versions use an integer concurrency token. Approved and rejected revisions are immutable. A new revision copies the latest approved snapshot, including a scheduled revision. Revision numbers and effective dates increase. PostgreSQL permits one open (Draft/Pending) revision per division and one line per document/product/packing-style tuple. Serializable write transactions and fresh actor grants protect each save; stale/conflicting edits return 409.

Effective dates are calendar dates stored as midnight UTC. An approved revision is eligible only when both `EffectiveFrom <= asOf` and `ApprovedAt <= asOf`. The latest eligible effective date/revision is Current; later approved documents are Scheduled, earlier ones Superseded. Late approval never changes pricing before the actual approval time. Query-based activation needs no worker and does not rewrite product master data.

## Packing styles and currency

A price is per **one complete pack of the selected packing style**. No automatic conversion between styles or currencies occurs. Each row records product ID, packing ID, amount or unpriced, required currency for priced lines, and snapshots of product code/name, packing style, currency name/symbol. Initial preparation includes active packing styles in the division. A legacy price seeds only products having exactly one active style; multi-style products remain unpriced until explicitly set. Later revisions clone all predecessor rows.

The active customer agreement for the exact customer/product/style still wins. Otherwise the effective division document's exact style applies. An absent or unpriced style on an effective list is unpriced; it cannot borrow another style's or a legacy price. Before the first list becomes effective, legacy product prices remain the fallback. New quotation and order lines record the chosen revision ID. Order lines also retain currency, and proforma lines retain style/amount/currency/revision when their source order has recorded pricing. Existing invoice amounts are not recalculated. Reports batch-load only relevant snapshot prices and apply the same as-of rule.

Legacy direct price writes are refused with `ListPrice.Versioned` once the division has revision documents. Existing customer agreement behavior is retained.

## Narrow reads

| Method | Path beneath `/api/v1/costing/price-lists` | Permission |
| --- | --- | --- |
| GET | `?division&page&pageSize&searchQuery` | CanViewCostingPriceList |
| GET | `/products?division&page&pageSize&searchQuery` | CanViewCostingPriceList |
| GET | `/{id}` | CanViewCostingPriceList |
| GET | `/{id}/lines?page&pageSize&searchQuery` | CanViewCostingPriceList |
| GET | `/{id}/print` | CanPrintCostingPriceList |
| POST | root | CanCreateCostingPriceList |
| POST | `/revise` | CanReviseCostingPriceList |
| PUT | `/{id}` | CanEditCostingPriceList |
| PUT | `/{id}/lines` | CanEditCostingPriceList |
| POST | `/{id}/submit` | CanSubmitCostingPriceList |

List and choice pages clamp page size to 100, use stable ordering and scalar projections, and avoid product warehouse/equipment/BOM/stock graphs. Line searches include product name/code/packing style. Only the explicit print request loads all snapshot lines from the selected revision. The list response provides latest-approved/open revision/next revision metadata independently of pagination and search. The legacy `/costing/list-prices` query is also a narrow scalar projection and adds optional division filtering; history and customer agreement contracts remain intact.

## Permissions and deployment

Ten keys are registered once under Finance → Costing. In addition to the six preparation/read/print keys above, `CanReviewCostingPriceList` and `CanReturnCostingPriceList` guard existing shared review actions; `CanApproveCostingPriceList` and `CanRejectCostingPriceList` guard the assigned approval decision. Assignment, document state, confirmed review and fresh grants remain required.

Apply `20261121124140_AddVersionedCostingPriceLists` and `20261121124141_GrantCostingPriceListPreparation` before the matching desktop build. These add tables and nullable transaction provenance; historical rows are not backfilled or repriced. The grant migration preserves view/create/edit/revise/submit/print tasks for all existing `ManageCosting` roles with matching access types. New review/return/approve/reject tasks are **granted to nobody** until an administrator assigns them. Configure a `CostingPriceList` approval workflow with a required stage through the existing settings. Customer agreements continue using `ManageCosting`.

The desktop contract merge is additive: seven new paths/fifteen schemas, optional division on legacy lookup, optional invoice style/price provenance. It does not import unrelated backend contract drift. Its compatibility regression restores the prior reviewed fingerprint when only those additions are removed.

## Verification

Focused tests cover pack/currency isolation, initial seeding, agreement precedence, missing styles, date boundaries/late approval, immutable approved documents, permission revocation, review before approval, print snapshots, search-independent revision metadata, endpoint policies and catalog registration. The PostgreSQL test creates/drops its own database using `ORYX_COSTING_TEST_CONNECTION` and executes the generated migration, uniqueness constraints, optimistic concurrency and actual scalar price queries. No demo-server database changes are made by these tests.
