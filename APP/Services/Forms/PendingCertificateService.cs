using System.Text.Json;
using APP.Repository;
using APP.Services.Authorization;
using APP.Services.Formulas;
using APP.Services.NotificationService;
using APP.Services.Transactions;
using APP.Utils;
using DOMAIN.Entities.Alerts;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Notifications;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.Forms;

public sealed class PendingCertificateService(
    ApplicationDbContext db,
    INotificationService notifications,
    IFormulaCalculationClient calculationClient = null)
{
    public static string EditKey(TemplateRepairTestKind kind) => kind switch
    {
        TemplateRepairTestKind.Product => PermissionKeys.CanEditProductCertificateOfAnalysis,
        TemplateRepairTestKind.RawMaterial => PermissionKeys.CanEditRawMaterialCertificateOfAnalysis,
        _ => PermissionKeys.CanEditPackagingMaterialCertificateOfAnalysis
    };

    public static string ReturnKey(TemplateRepairTestKind kind, CertificateReturnKind request) => (kind, request) switch
    {
        (TemplateRepairTestKind.Product, CertificateReturnKind.Review) => PermissionKeys.CanRequestProductCertificateReview,
        (TemplateRepairTestKind.Product, _) => PermissionKeys.CanRequestProductCertificateCorrection,
        (TemplateRepairTestKind.RawMaterial, CertificateReturnKind.Review) => PermissionKeys.CanRequestRawMaterialCertificateReview,
        (TemplateRepairTestKind.RawMaterial, _) => PermissionKeys.CanRequestRawMaterialCertificateCorrection,
        (_, CertificateReturnKind.Review) => PermissionKeys.CanRequestPackagingMaterialCertificateReview,
        _ => PermissionKeys.CanRequestPackagingMaterialCertificateCorrection
    };

    public Task<Result> Request(Guid id, Guid actor, TemplateRepairTestKind kind, CertificateReturnKind requestKind, RequestCertificateChange request) =>
        ReferenceMaintenanceTransaction.Run(db, async () =>
        {
            var key = ReturnKey(kind, requestKind);
            if (!await Allowed(actor, key)) return Denied();
            var response = await Load(id);
            var error = await Validate(response, kind, request.Version);
            if (error is not null) return error;
            if (response.CoaReturnKind.HasValue) return Error.Conflict("COA.ChangesRequested", "The preparer already has an open request.");
            var roles = await db.UserRoles.Where(role => role.UserId == actor && db.Roles.Any(active => active.Id == role.RoleId))
                .Select(role => role.RoleId).ToListAsync();
            if (!await (await PendingApprovalQueries.For(db, actor, roles)).Responses.AnyAsync(item => item.Id == id))
                return Denied();
            var reason = request.Reason?.Trim();
            if (!ValidReason(reason)) return ReasonError();
            var previous = Snapshot(response, []);
            response.CoaReturnKind = requestKind;
            response.CoaReturnReason = reason;
            Record(response, actor, requestKind, reason, previous, Snapshot(response, []));
            if (!await Allowed(actor, key)) return Denied();
            var batchNumber = response.MaterialBatchId.HasValue
                ? await db.MaterialBatches.Where(item => item.Id == response.MaterialBatchId).Select(item => item.BatchNumber).SingleOrDefaultAsync()
                : await db.BatchManufacturingRecords.Where(item => item.Id == response.BatchManufacturingRecordId).Select(item => item.BatchNumber).SingleOrDefaultAsync();
            var label = requestKind == CertificateReturnKind.Review ? "review" : "correction";
            await notifications.SendNotification(new NotificationDto
            {
                Message = $"QA requested {label} of the certificate you prepared for batch {batchNumber ?? "–"}. Open its test results to view the request and resubmit.",
                Type = NotificationType.CertificateChangeRequested,
                Recipients = [new() { Id = response.CheckedById.Value }]
            }, [AlertType.InApp]);
            // Some callers use a notification adapter that does not save the tracked certificate.
            if (!await Allowed(actor, key)) return Denied();
            await db.SaveChangesAsync();
            return Result.Success();
        });

    public Task<Result> Save(Guid id, Guid actor, TemplateRepairTestKind kind, SavePendingCertificate request) =>
        ReferenceMaintenanceTransaction.Run(db, async () =>
        {
            var key = EditKey(kind);
            if (!await Allowed(actor, key)) return Denied();
            var response = await Load(id);
            var error = await Validate(response, kind, request.Version);
            if (error is not null) return error;
            if (response.CheckedById != actor) return Denied();
            var reason = request.Reason?.Trim();
            if (!ValidReason(reason)) return ReasonError();
            var notes = request.Notes?.Trim();
            if (notes?.Length > 2000) return Error.Validation("COA.Notes", "Keep certificate notes within 2000 characters.");
            if (request.Compliance is null || request.Compliance.Any(item => item is null)
                || request.Compliance.Select(item => item.FormSectionId).Distinct().Count() != request.Compliance.Count)
                return Error.Validation("COA.Compliance", "Send each test's compliance at most once.");

            var answers = await db.FormResponses.Where(item => item.ResponseId == id && item.FormField.FormSection.FormId == response.FormId)
                .Include(item => item.FormField).ToListAsync();
            var sections = answers.Select(item => item.FormField.FormSectionId).ToHashSet();
            if (request.Compliance.Any(item => !sections.Contains(item.FormSectionId)))
                return Error.Validation("COA.Test", "One of the test sections does not belong to this certificate.");

            var reporting = (await ProductAssayReportingRuntime.Build(db, response, calculationClient))
                .ToDictionary(field => field.FormFieldId);
            var overrides = request.Compliance.ToDictionary(item => item.FormSectionId, item => item.Complies);
            var editedAnswers = answers.Where(answer => overrides.ContainsKey(answer.FormField.FormSectionId)).ToList();
            if (editedAnswers.Any(answer => reporting.TryGetValue(answer.FormFieldId, out var field)
                && (field.Error is not null || field.Results.Any(result => result.Converted && result.Complies != true))))
                return Error.Validation("COA.CalculatedCompliance", "A test with unresolved or failed assay reporting cannot be overridden.");

            var previous = Snapshot(response, answers);
            foreach (var answer in editedAnswers)
            {
                answer.Complies = reporting.TryGetValue(answer.FormFieldId, out var field)
                    ? ProductAssayReportingRuntime.EffectiveCompliance(field, overrides[answer.FormField.FormSectionId])
                    : overrides[answer.FormField.FormSectionId];
            }
            response.CertificateNotes = string.IsNullOrWhiteSpace(notes) ? null : notes;
            response.CoaReturnKind = null;
            response.CoaReturnReason = null;
            var stages = ResponseApprovalRoundManager.Current(response.Approvals);
            var now = DateTime.UtcNow;
            var newRound = stages.Select(stage => new ResponseApproval
            {
                ResponseId = id, ApprovalId = stage.ApprovalId, ApprovalRound = checked(stage.ApprovalRound + 1),
                Required = stage.Required, Order = stage.Order, UserId = stage.UserId, RoleId = stage.RoleId,
                FormulaSubmissionSetId = stage.FormulaSubmissionSetId, CreatedAt = now
            }).ToList();
            ResponseApprovalRoundManager.ActivateEligible(newRound, now);
            db.ResponseApprovals.AddRange(newRound);
            Record(response, actor, null, reason, previous, Snapshot(response, answers));
            if (!await Allowed(actor, key)) return Denied();
            await db.SaveChangesAsync();
            return Result.Success();
        });

    internal static Error? DecisionError(Response response, int? contentRevision)
    {
        if (response.CoaReturnKind.HasValue)
            return Error.Conflict("COA.AwaitingResubmission", "The preparer must resubmit this certificate before a decision.");
        if ((response.CoaVersion > 0 || contentRevision.HasValue) && response.CoaVersion != contentRevision)
            return Error.Conflict("COA.Changed", "The certificate changed. Reload it before making a decision.");
        return null;
    }

    private Task<Response> Load(Guid id) => db.Responses.Include(item => item.Approvals).SingleOrDefaultAsync(item => item.Id == id);

    private async Task<Error?> Validate(Response response, TemplateRepairTestKind kind, int version)
    {
        if (response is null) return Error.NotFound("COA.NotFound", "The certificate could not be found.");
        var actualKind = await SubjectKind(db, response);
        if (actualKind != kind) return Denied();
        if (response.Approved || response.Rejected || !response.CheckedAt.HasValue || !response.CheckedById.HasValue
            || !ResponseApprovalRoundManager.Current(response.Approvals).Any(item => item.Required && item.Status == ApprovalStatus.Pending))
            return Error.Conflict("COA.Closed", "Only a certificate awaiting approval can be changed.");
        if (response.CoaVersion != version)
            return Error.Conflict("COA.Changed", "The certificate changed. Reload it before continuing.");
        return null;
    }

    internal static async Task<TemplateRepairTestKind?> SubjectKind(ApplicationDbContext db, Response response)
    {
        if (response.RoutineTrackId.HasValue) return null;
        if (response.BatchManufacturingRecordId.HasValue && response.ProductionActivityStepId.HasValue && !response.MaterialBatchId.HasValue)
            return TemplateRepairTestKind.Product;
        if (!response.MaterialBatchId.HasValue || response.BatchManufacturingRecordId.HasValue) return null;
        var kind = await db.MaterialBatches.Where(item => item.Id == response.MaterialBatchId)
            .Select(item => (MaterialKind?)item.Material.Kind).SingleOrDefaultAsync();
        return kind switch
        {
            MaterialKind.Raw => TemplateRepairTestKind.RawMaterial,
            MaterialKind.Package => TemplateRepairTestKind.PackagingMaterial,
            _ => null
        };
    }

    private void Record(Response response, Guid actor, CertificateReturnKind? requested, string reason, string previous, string next)
    {
        response.CoaVersion++;
        response.LastUpdatedById = actor;
        db.Set<PendingCertificateChange>().Add(new PendingCertificateChange
        {
            ResponseId = response.Id, Version = response.CoaVersion, ActorId = actor, ChangedAt = DateTime.UtcNow,
            RequestedReturn = requested, Reason = reason, PreviousStateJson = previous, NewStateJson = next
        });
    }

    private static string Snapshot(Response response, IReadOnlyCollection<FormResponse> answers) => JsonSerializer.Serialize(new
    {
        response.CertificateNotes, response.CoaReturnKind, response.CoaReturnReason,
        Compliance = answers.OrderBy(item => item.Id).Select(item => new { item.Id, item.FormFieldId, item.Complies })
    });

    private async Task<bool> Allowed(Guid actor, string key) => (await CurrentPermissionSet.Load(db, actor)).Contains(key);
    private static bool ValidReason(string reason) => reason?.Length is >= 1 and <= 2000;
    private static Error ReasonError() => Error.Validation("COA.Reason", "Enter a reason of 1–2000 characters.");
    private static Error Denied() => Error.Forbidden("COA.Permission", "You cannot change this certificate.");
}
