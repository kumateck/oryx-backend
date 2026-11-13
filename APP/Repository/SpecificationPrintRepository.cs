using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Specifications;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;
using SHARED.Services.Identity;

namespace APP.Repository;

public sealed class SpecificationPrintRepository(ApplicationDbContext context, ICurrentUserService user)
{
    public async Task<Result<SpecificationPrintHistory>> Material(Guid id, MaterialKind kind, CancellationToken cancellationToken)
    {
        var (view, print) = kind == MaterialKind.Raw
            ? (PermissionKeys.CanViewRawMaterialSpecifications, PermissionKeys.CanExportRawMaterialSpecification)
            : (PermissionKeys.CanViewPackagingMaterialSpecifications, PermissionKeys.CanExportPackagingMaterialSpecification);
        if (!await CanPrint(view, print)) return Denied();
        if (!await context.MaterialSpecifications.AnyAsync(specification => specification.Id == id
                && specification.Material.Kind == kind, cancellationToken)) return Missing();

        return await History("MaterialSpecifications", id, cancellationToken);
    }

    public async Task<Result<SpecificationPrintHistory>> Product(Guid id, CancellationToken cancellationToken)
    {
        if (!await CanPrint(PermissionKeys.CanViewProductSpecifications, PermissionKeys.CanExportProductSpecification))
            return Denied();
        if (!await context.ProductSpecifications.AnyAsync(specification => specification.Id == id, cancellationToken))
            return Missing();

        return await History("ProductSpecifications", id, cancellationToken);
    }

    private async Task<bool> CanPrint(string view, string print)
    {
        var permissions = await CurrentPermissionSet.Load(context, user.UserId);
        return permissions.Contains(view) && permissions.Contains(print);
    }

    private static Error Denied() => Error.Forbidden("SpecificationPrint.Permission", "Your permissions do not allow this specification to be printed.");

    private static Error Missing() => Error.NotFound("SpecificationPrint.NotFound", "The specification was not found in the requested category.");

    private async Task<SpecificationPrintHistory> History(string table, Guid id, CancellationToken cancellationToken)
    {
        var availableFrom = await context.Database.SqlQuery<DateTime?>($"""
            SELECT "AvailableFrom" AS "Value" FROM public."ReportHistoryCoverage" WHERE "TableName" = {table}
            """).SingleOrDefaultAsync(cancellationToken);

        // Compare the actual specification fields so approval and other metadata updates do not become revisions.
        var snapshots = await context.Database.SqlQuery<SpecificationSnapshot>($"""
            SELECT v."RecordedAt",
                v."Payload"->>'SpecificationNumber' AS "SpecificationNumber",
                v."Payload"->>'RevisionNumber' AS "RevisionNumber",
                v."Payload"->>'SupersedesNumber' AS "SupersedesNumber",
                (v."Payload"->>'CreatedAt')::timestamptz AS "CreatedAt",
                (v."Payload"->>'UpdatedAt')::timestamptz AS "UpdatedAt",
                NULLIF(concat_ws(' ', creator."FirstName", creator."LastName"), '') AS "CreatedBy",
                NULLIF(concat_ws(' ', editor."FirstName", editor."LastName"), '') AS "UpdatedBy",
                (v."Payload" - ARRAY['Id', 'CreatedAt', 'CreatedById', 'UpdatedAt', 'LastUpdatedById',
                    'DeletedAt', 'LastDeletedById', 'IsVerified', 'VerifiedAt', 'VerifiedById', 'UserId',
                    'ResponseId', 'MaterialId', 'ProductId'])::text AS "Content"
            FROM public."ReportHistoryVersions" v
            LEFT JOIN public.users creator ON creator."Id" = (v."Payload"->>'CreatedById')::uuid
            LEFT JOIN public.users editor ON editor."Id" = (v."Payload"->>'LastUpdatedById')::uuid
            WHERE v."TableName" = {table}
              AND v."EntityKey" = public.oryx_report_key(jsonb_build_object('Id', {id}), ARRAY['Id'])
              AND NOT v."IsDeleted" AND v."Payload"->>'DeletedAt' IS NULL
            ORDER BY v."Sequence"
            """).ToListAsync(cancellationToken);

        var revisions = new List<SpecificationPrintRevision>();
        SpecificationSnapshot previous = null;
        SpecificationSnapshot lastRevision = null;
        foreach (var snapshot in snapshots)
        {
            if (previous is null)
            {
                var baseline = snapshot.RecordedAt == availableFrom;
                revisions.Add(snapshot.ToRevision(baseline ? "History baseline" : "Created",
                    baseline ? snapshot.UpdatedBy ?? snapshot.CreatedBy : snapshot.CreatedBy,
                    baseline ? snapshot.RecordedAt : snapshot.CreatedAt));
                if (baseline && snapshot.UpdatedAt.HasValue) lastRevision = snapshot;
            }
            else if (snapshot.Content != previous.Content)
            {
                revisions.Add(snapshot.ToRevision("Revised", snapshot.UpdatedBy, snapshot.UpdatedAt ?? snapshot.RecordedAt));
                lastRevision = snapshot;
            }
            previous = snapshot;
        }
        revisions.Reverse();
        return new SpecificationPrintHistory(lastRevision?.UpdatedBy,
            lastRevision?.UpdatedAt ?? lastRevision?.RecordedAt, availableFrom, revisions);
    }

    private sealed class SpecificationSnapshot
    {
        public DateTime RecordedAt { get; set; }
        public string SpecificationNumber { get; set; }
        public string RevisionNumber { get; set; }
        public string SupersedesNumber { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
        public string Content { get; set; }

        public SpecificationPrintRevision ToRevision(string action, string actor, DateTime? time) =>
            new(SpecificationNumber, RevisionNumber, SupersedesNumber, actor, time, action);
    }
}
