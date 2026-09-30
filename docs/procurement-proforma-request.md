# Procurement proforma request

The awarded quotation screen sends a supplier proforma request through
`POST /api/v1/procurement/purchase-order/proforma-invoice/{purchaseOrderId}`.
This request precedes purchase order review, so it can be sent while the
purchase order is unapproved. The endpoint still requires an existing purchase
order and retains its email and status behavior.

Final purchase order dispatch through
`POST /api/v1/procurement/purchase-order/{purchaseOrderId}` continues to
require purchase order approval. This preserves the approval gate for the
commitment sent to the supplier.
