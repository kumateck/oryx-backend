# Billing sheet payment and shipment clearance

Billing sheet charges are recorded through the existing charge payment
endpoint. A charge becomes paid only when its payment is approved; pending
payment approvals do not settle the charge.

When a shipment moves to `Cleared`, the service requires an approved billing
sheet for its invoice and every recorded sheet charge to be paid. If either
condition fails, the shipment status remains unchanged and the endpoint
returns a validation error. Successful clearance marks the billing sheet
`Paid` and records the shipment clearance timestamp. The frontend exposes
the payment dialog from approved billing sheet details as well as the
shipment list.
