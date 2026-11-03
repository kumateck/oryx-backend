# QA product department scope

The QA product UI selects a production department and reuses the existing product
list/detail/edit operations and permission keys. It does not switch the session
or grant roles. Production product routes retain their active-department behavior.

`GET /api/v1/material/department` and `GET /api/v1/material/{materialId}/uom`
accept optional `productId`. This requires live `CanViewPlannedProducts` and
`CanEditProductionPlan` grants. The server derives the department from an active
product in an active production department; disabled/deleted actors, archived
products, nonproduction departments and revoked grants cannot supply the context.
The client cannot supply an arbitrary department ID. Warehouse quantities and
pending transfer calculations use that derived department too. Calls without
product context retain the existing user-department linkage.

No schema migration is required. Deploy this API before the matching QA product
editors; otherwise their material queries would fall back to the QA user's
own department. Existing product validation, verification, approval and audit
workflows are reused. No customer data or role assignment is changed by this work.
