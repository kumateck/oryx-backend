# QC worksheet template migration CI repair

Date: 2026-09-23.

The backend merge included two `AddQcWorksheetTemplates` migration pairs with different IDs. Their `Up` and `Down` files were byte-identical, so the duplicate class prevented `dotnet publish` from compiling and, if renamed without changing operations, would attempt to create the same tables twice.

The later `20260917202209_AddQcWorksheetTemplates` pair was removed. The earlier `20260917195736_AddQcWorksheetTemplates` migration remains the canonical schema operation. No API, permission, or workflow behavior changed.

Before applying migrations to an environment, check its `__EFMigrationsHistory` for the retained ID. An environment that applied only the removed ID needs a reviewed history reconciliation before applying the retained migration, to avoid recreating existing tables.
