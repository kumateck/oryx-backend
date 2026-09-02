# Oryx ERP

## Recent Updates

- Material stock pipeline: `GET /api/v1/material/{materialId}/stock/pipeline` exposes active inbound material quantities from purchase requisition through sourcing, quotation, purchase order, shipment milestones, receiving, QC, and GRN. The read model reconciles each quantity into one stage, uses actual shipment received quantity, groups duplicate lines before subtraction, and excludes final shelf-distributed stock. Focused builder and EF-backed query tests cover partial deliveries, duplicate lines, and completed distribution.
- Warehouse shelf-list performance: `GET /api/v1/warehouse/shelf` is now a no-tracking, auto-include-free list query that loads only shelf/rack/location/warehouse metadata. List rows intentionally omit material batches; `GET /api/v1/warehouse/shelf/{shelfId}` remains the full detail contract. Search now covers shelf code/name/description, rack, location, and warehouse. Repository coverage verifies the lean payload and code search.
- Analytics/report integrity: implemented the staff gender-ratio report, corrected checklist user identity and logistics OpenAPI metadata, and removed anonymous access from supplier/vendor summary reports.
- Warehouse expiry risk now excludes the intentionally unlimited water sentinel and labels shelf quantities with shelf UOM when available.
- Warehouse KPI reports now apply warehouse type/division filters inside authenticated department scope. `GET /api/v1/report/warehouse-kpi/freshness` exposes persisted source watermarks and row counts without mutating operational data.
- Partial shelf movements preserve total stock, retain source UOM, and reject empty, non-positive, or cross-UOM requests. Regression coverage is included in `tests/APP.Tests`.
- Supplier quotation request lookup now remains readable after it is sent. For a supplier with a new request, the lookup selects that unsent request first; otherwise it returns the latest sent request. Only a supplier with no quotation request at all receives `404 Supplier.QuotationRequest.NotFound`, never `Error.NullValue`. Added repository coverage for completed and next-request selection.
- Supplier quotation detail and receipt endpoints now return `404 Supplier.Quotation.NotFound` for a genuinely missing quotation instead of producing `Error.NullValue` or a null-reference response. The controller contract and repository regression coverage include this behavior.

## Introduction
Brief introduction about your project.

## Features
List of features.

## Installation
Instructions for installing and running the project.

## Usage
Instructions for using the project.

## Docker Setup
For instructions on how to set up and run the application using Docker, please refer to the [Docker Setup Instructions](DOCKER.md).

## Contributing
Guidelines for contributing to the project.

## License
Details about the project's license.
