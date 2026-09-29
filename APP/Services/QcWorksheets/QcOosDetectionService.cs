using APP.IRepository;
using DOMAIN.Entities.QcWorksheets;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace APP.Services.QcWorksheets;

/// <summary>
/// Automatic OOS detection, and the one workflow hook a completed retest needs.
/// <para>
/// Kept out of <c>WorksheetInstanceRepository</c> deliberately: submitting a worksheet and
/// judging its values against a Specification are separate concerns, and the judging half is
/// the half that has to be independently testable.
/// </para>
/// </summary>
public interface IQcOosDetectionService
{
    /// <summary>
    /// Evaluates every field on a just-submitted worksheet that a Characteristic on the round's
    /// pinned Specification binds, whatever the field's type (see <see cref="QcBoundFieldJudge"/>),
    /// opening an <see cref="OosCase"/> for each Action-limit breach. Returns the cases opened,
    /// newest first.
    /// </summary>
    Task<List<OosCase>> DetectOnSubmitAsync(Guid worksheetInstanceId, Guid userId);

    /// <summary>
    /// Called when a worksheet reaches Reviewed. If it is a retest an OOS case is waiting on,
    /// that case advances to PendingQaDisposition and its QA approval round is opened.
    /// </summary>
    Task AdvanceOnRetestReviewedAsync(Guid worksheetInstanceId, Guid userId);
}

/// <inheritdoc />
public class QcOosDetectionService(
    ApplicationDbContext context,
    IApprovalRepository approvalRepository,
    ILogger<QcOosDetectionService> logger) : IQcOosDetectionService
{
    public async Task<List<OosCase>> DetectOnSubmitAsync(Guid worksheetInstanceId, Guid userId)
    {
        var instance = await context.QcWorksheetInstances
            .AsNoTracking()
            .Include(item => item.TestRequestSubject)
                .ThenInclude(subject => subject.TestRequest)
            .SingleOrDefaultAsync(item => item.Id == worksheetInstanceId);

        if (instance?.TestRequestSubject?.TestRequest is null)
            return [];

        var subject = instance.TestRequestSubject;
        var round = subject.TestRequest;

        // The round's own pinned Specification row, by id. Nothing here walks SupersedesId, so
        // a Specification revised after this round was raised cannot change what its results
        // are judged against.
        var characteristics = await context.QcSpecificationCharacteristics
            .AsNoTracking()
            .Where(item => item.SpecificationId == round.SpecificationId
                && item.SourceWorksheetTemplateId == instance.WorksheetTemplateId)
            .ToListAsync();

        if (characteristics.Count == 0)
            return [];

        // The pinned template version's own fields — resolved by id, never forward. Judged =
        // bound by a characteristic: every candidate field (a Result field, or any scalar Entry
        // or Calculated field) is judged if a Characteristic names it, and skipped if none does.
        var templateFields = await context.QcWorksheetFields
            .AsNoTracking()
            .Where(field => context.QcWorksheetSections.Any(section =>
                section.Id == field.WorksheetSectionId
                && section.WorksheetTemplateId == instance.WorksheetTemplateId))
            .Select(field => new { field.FieldKey, field.Type, field.Mode })
            .ToListAsync();

        var judgedFields = templateFields
            .Where(field => QcBoundFieldJudge.IsCandidate(field.Type, field.Mode))
            .ToList();

        var resultFieldKeys = judgedFields.Select(field => field.FieldKey).ToList();

        if (resultFieldKeys.Count == 0)
            return [];

        var fieldTypes = judgedFields
            .GroupBy(field => field.FieldKey, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().Type, StringComparer.Ordinal);

        var values = await context.QcWorksheetFieldValues
            .AsNoTracking()
            .Where(value => value.WorksheetInstanceId == worksheetInstanceId
                && resultFieldKeys.Contains(value.FieldKey))
            .ToListAsync();

        // Cases already open against this worksheet, so a resubmission after a correction cycle
        // re-judges the values without opening a second case for the same field.
        var alreadyOpen = await context.QcOosCases
            .AsNoTracking()
            .Where(item => item.WorksheetInstanceId == worksheetInstanceId
                && item.Status != OosCaseStatus.Closed)
            .Select(item => item.FieldKey)
            .ToListAsync();

        var opened = new List<OosCase>();

        foreach (var fieldKey in resultFieldKeys.Distinct())
        {
            if (alreadyOpen.Contains(fieldKey))
                continue;

            var characteristic = QcCharacteristicResolver.Resolve(
                characteristics, fieldKey, subject.SamplingPointGroupId);
            if (characteristic is null)
                continue;

            // A judged field is one scalar row. A value that was never entered still gets
            // judged, because "nothing was entered against a hard limit" is not a pass.
            var submitted = values
                .Where(value => value.FieldKey == fieldKey)
                .Select(value => value.Value)
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

            var evaluation = QcBoundFieldJudge.Evaluate(characteristic, fieldTypes[fieldKey], submitted);

            switch (evaluation.Outcome)
            {
                case LimitOutcome.Compliant:
                    continue;

                // Alert breaches flag for trend review and block nothing — no case, no
                // quarantine, no hold on release (lifecycle-and-governance.md).
                case LimitOutcome.Alert:
                    logger.LogInformation(
                        "QC alert limit breached for trend review. Worksheet instance {InstanceId}, "
                        + "field {FieldKey}, characteristic {CharacteristicId}: {Reason}",
                        worksheetInstanceId, fieldKey, characteristic.Id, evaluation.Reason);
                    continue;

                // An unparseable limit is a Specification data-quality defect, not a result
                // defect. It is still never a silent pass: a case is opened so a human decides,
                // and the defect is logged against the characteristic that carries it.
                case LimitOutcome.ManualReview:
                    logger.LogWarning(
                        "QC limit text could not be evaluated and was referred for manual review. "
                        + "Specification characteristic {CharacteristicId} ('{TestName}') on "
                        + "specification {SpecificationId}, worksheet instance {InstanceId}, field "
                        + "{FieldKey}: {Reason}",
                        characteristic.Id, characteristic.TestName, round.SpecificationId,
                        worksheetInstanceId, fieldKey, evaluation.Reason);
                    break;
            }

            opened.Add(BuildCase(worksheetInstanceId, fieldKey, characteristic, submitted, evaluation, userId));
        }

        if (opened.Count == 0)
            return [];

        context.QcOosCases.AddRange(opened);
        await context.SaveChangesAsync();

        foreach (var oosCase in opened)
            logger.LogWarning(
                "OOS case {CaseId} opened automatically for worksheet instance {InstanceId}, "
                + "field {FieldKey}. Observed '{Observed}' against '{Limit}'.",
                oosCase.Id, worksheetInstanceId, oosCase.FieldKey, oosCase.ObservedValue,
                oosCase.BreachedLimit);

        return opened;
    }

    public async Task AdvanceOnRetestReviewedAsync(Guid worksheetInstanceId, Guid userId)
    {
        var waiting = await context.QcOosCases
            .Where(item => item.RetestWorksheetInstanceId == worksheetInstanceId
                && item.Status == OosCaseStatus.RetestRequested)
            .ToListAsync();

        if (waiting.Count == 0)
            return;

        var retestStatus = await context.QcWorksheetInstances
            .AsNoTracking()
            .Where(item => item.Id == worksheetInstanceId)
            .Select(item => item.Status)
            .SingleOrDefaultAsync();

        // Only a reviewed retest closes Phase 1. A submitted-but-unreviewed result is not yet
        // something QA can dispose against.
        if (retestStatus is not (WorksheetInstanceStatus.Reviewed or WorksheetInstanceStatus.Locked))
            return;

        foreach (var oosCase in waiting)
        {
            oosCase.Status = OosCaseStatus.PendingQaDisposition;
            oosCase.UpdatedAt = DateTime.UtcNow;
            oosCase.LastUpdatedById = userId;
        }

        await context.SaveChangesAsync();

        // With no stages, the case stays pending until a QA actor selects an outcome.
        // The disposition endpoint then records the system approval and applies it.
        if (!await HasConfiguredDispositionChain())
            return;

        // Opens the QA disposition round through the same approval engine every other QC
        // approval point uses. No signature is taken here — that happens at /disposition.
        foreach (var oosCase in waiting)
            await approvalRepository.CreateInitialApprovalsAsync(QcWorksheetModelTypes.OosCase, oosCase.Id);
    }

    private async Task<bool> HasConfiguredDispositionChain()
    {
        var approval = await context.Approvals
            .FirstOrDefaultAsync(item => item.ItemType == QcWorksheetModelTypes.OosCase);

        if (approval is null)
            return false;

        return await context.ApprovalStages.AnyAsync(stage => stage.ApprovalId == approval.Id);
    }

    private static OosCase BuildCase(
        Guid worksheetInstanceId,
        string fieldKey,
        SpecificationCharacteristic characteristic,
        string submitted,
        LimitEvaluation evaluation,
        Guid userId) => new()
    {
        Id = Guid.NewGuid(),
        WorksheetInstanceId = worksheetInstanceId,
        FieldKey = fieldKey,
        SpecificationCharacteristicId = characteristic.Id,
        Status = OosCaseStatus.Open,

        // System-set, and deliberately without an "opened by": a limit breach opened this, not
        // the analyst who happened to submit the worksheet.
        OpenedAt = DateTime.UtcNow,
        ObservedValue = submitted,
        BreachedLimit = evaluation.LimitText,

        // InvestigationDetails is left empty on purpose: it belongs to the investigator, and
        // seeding it with the detector's own wording would blur an automatic finding into a
        // human one. The breach itself is fully described by ObservedValue and BreachedLimit.
        Approved = false,
        CreatedAt = DateTime.UtcNow,
        CreatedById = userId
    };
}
