# Packing-material substitute save failure

At deployed commit `ab23684044b94a42e6d7a24dcc220ae2acdc21cb`, POST product/{productId}/packages fails when any request row contains a nonempty quantified `substitutes` list. AutoMapper conventionally maps that same-named collection before AfterMap. There is no nested request-to-entity map, so it throws before the explicit material-id/quantity mapping runs. Legacy substituteMaterialIds-only requests and empty substitute lists do not trigger it. The issue was also present on the checked main and desmond-latest branches.

Ignore the destination Substitutes member during conventional mapping, then retain the existing AfterMap implementation. This is the safeguard already used by BOM items. Quantified substitutes keep their own quantities and take precedence over legacy IDs. Legacy substitutes keep a null quantity, preserving downstream behavior.

Other AfterMap collection assignments were inspected: BOM substitutes already ignore conventional mapping; SourceRequisition grouping uses a registered SourceRequisitionItem-to-DTO map; allocation pricing adjusts already-mapped items. No additional missing nested map of this pattern was found.

On PostgreSQL, CreateProductPackage owns a ProductionMaterialTransaction around old-list deletion and new-list save. A mapping exception disposes the uncommitted transaction and rolls back deletion. No automatic customer data rewrite is needed for this exception; after deploying the fix, retry the intended packing list save. The regression suite includes a real PostgreSQL replacement save and an injected mapping failure that checks active and historical rows are retained. No customer data or schema migration is included.

No endpoint, permission key, grant, request shape or frontend behavior changes. Existing variant-specific ProductPermission checks and repository grant reloads remain authoritative.
