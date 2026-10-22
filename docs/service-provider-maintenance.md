# Contractor maintenance repair — 2026-10-21

Part of the requested Oryx Live general stores/services implementation. These are local source changes; no deployment or production record mutation was performed.

## Contracts and behavior

Existing service-provider endpoints and request/response DTOs are unchanged. `PUT /api/v1/service-providers/{id}` now loads the existing service association collection and explicitly replaces it with the verified requested services. Previously the request's ServiceIds did not map to the Services navigation, so the API accepted edits without persisting association changes. Missing service IDs return before mapping or changing existing associations.

`DELETE /api/v1/service-providers/{id}` now returns NotFound for a missing contractor. The old inverted null check rejected every existing contractor and would dereference null for a missing one. An unreferenced contractor is soft-deleted with DeletedAt/LastDeletedById, preserving the established audit path.

Deletion refuses links in JobOrderServiceProviders, JobOrderExecutions, ServiceQuotations, ServiceProformaInvoices and ServiceMemos. These checks explicitly include soft-deleted historical rows to preserve historical job, quotation and financial reconciliation. Merely being associated with service definitions does not count as an operational job/financial reference. No linked record is modified or deleted.

## Verification

Nine isolated EF Core in-memory tests pass. They verify association replacement across fresh contexts, missing-service failure without association loss, missing contractor handling, audited soft deletion, and all five historical-reference deletion guards. The test fixture uses the repository's installed AutoMapper version; no dependency was added or upgraded. SQL concurrency and deployed-server acceptance remain unverified.

The existing controller still lacks granular per-operation permission attributes; Live uses the existing contractor keys but client guards do not repair backend authorization. This repair does not change approval workflows, introduce role bypasses or alter deployment/session policy.

Documentation updated: this note, docs/services.md and docs/workflows.md. Live's migration docs record the matching UI checks and the need to run the corrected backend before expecting association updates/deletion to work.
