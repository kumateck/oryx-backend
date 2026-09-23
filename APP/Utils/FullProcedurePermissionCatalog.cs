using System.Reflection;
using System.Text.RegularExpressions;
using DOMAIN.Entities.Permissions;

namespace APP.Utils;

public static class FullProcedurePermissionCatalog
{
    public const string Module = "Full Procedures";
    public const string QuestionsAndTemplates = "Questions & Templates";
    public const string Procedures = "Procedure Definitions";
    public const string BatchRecords = "BMR/BPR Records";
    public const string ProcedureRuns = "Procedure Runs";
    public const string RndProductBatches = "R&D Product Batches";

    private static readonly HashSet<string> TemplateKeys =
    [
        FullProcedurePermissionKeys.CanViewQuestionTemplates,
        FullProcedurePermissionKeys.CanManageTemplateAreas,
        FullProcedurePermissionKeys.CanManageQuestionRevision,
        FullProcedurePermissionKeys.CanManageFormTemplateRevision,
        FullProcedurePermissionKeys.CanManageActivityTemplateRevision,
        FullProcedurePermissionKeys.CanManageWorkflowTemplateRevision,
        FullProcedurePermissionKeys.CanShareTemplateRevision,
        FullProcedurePermissionKeys.CanReviewTemplateRevision,
        FullProcedurePermissionKeys.CanPublishTemplateRevision,
        FullProcedurePermissionKeys.CanViewHrTemplateResponse,
        FullProcedurePermissionKeys.CanViewQcTemplateResponse,
        FullProcedurePermissionKeys.CanViewMicrobiologyTemplateResponse,
    ];

    private static readonly HashSet<string> ProcedureKeys =
    [
        FullProcedurePermissionKeys.CanViewProcedures,
        FullProcedurePermissionKeys.CanCreateProcedure,
        FullProcedurePermissionKeys.CanEditProcedureDraft,
        FullProcedurePermissionKeys.CanValidateProcedureDraft,
        FullProcedurePermissionKeys.CanSubmitProcedureReview,
        FullProcedurePermissionKeys.CanReviewProcedureRevision,
        FullProcedurePermissionKeys.CanApproveProcedureRevision,
        FullProcedurePermissionKeys.CanAssignProcedureRevision,
    ];

    private static readonly HashSet<string> BatchRecordKeys =
    [
        FullProcedurePermissionKeys.CanViewBatchRecordDefinitions,
        FullProcedurePermissionKeys.CanViewBatchRecordIssues,
        FullProcedurePermissionKeys.CanManageBatchRecordMaster,
        FullProcedurePermissionKeys.CanApproveBatchRecordMaster,
        FullProcedurePermissionKeys.CanApproveProcedureBundle,
        FullProcedurePermissionKeys.CanIssueBmrMaster,
        FullProcedurePermissionKeys.CanIssueBprMaster,
        FullProcedurePermissionKeys.CanReprintBatchRecord,
        FullProcedurePermissionKeys.CanReissueBatchRecord,
        FullProcedurePermissionKeys.CanAmendBatchRecord,
    ];

    private static readonly HashSet<string> RunKeys =
    [
        FullProcedurePermissionKeys.CanViewProcedureRuns,
        FullProcedurePermissionKeys.CanIssueProcedureRun,
        FullProcedurePermissionKeys.CanStartProcedureRun,
        FullProcedurePermissionKeys.CanRecordProcedureEvidence,
        FullProcedurePermissionKeys.CanPerformProcedureProcessAction,
        FullProcedurePermissionKeys.CanPerformProcedureClearance,
        FullProcedurePermissionKeys.CanApproveProcedureClearance,
        FullProcedurePermissionKeys.CanRecordProcedureIpc,
        FullProcedurePermissionKeys.CanReviewProcedureIpc,
        FullProcedurePermissionKeys.CanRequestProcedureStock,
        FullProcedurePermissionKeys.CanCreateProcedureAtr,
        FullProcedurePermissionKeys.CanPerformProcedurePacking,
        FullProcedurePermissionKeys.CanApproveProcedurePackingReconciliation,
        FullProcedurePermissionKeys.CanTransferProcedureFinishedProduct,
        FullProcedurePermissionKeys.CanDispatchProcedureLot,
        FullProcedurePermissionKeys.CanHoldProcedureRun,
        FullProcedurePermissionKeys.CanResumeProcedureRun,
        FullProcedurePermissionKeys.CanAuthorizeProcedureRework,
        FullProcedurePermissionKeys.CanReviewProcedureBatch,
        FullProcedurePermissionKeys.CanReleaseProcedureBatch,
    ];

    public static IReadOnlyList<PermissionDto> Generate()
    {
        return typeof(FullProcedurePermissionKeys)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(field => (string)field.GetRawConstantValue()!)
            .Select(key => CreatePermission(SubmoduleFor(key), key))
            .ToList();
    }

    private static string SubmoduleFor(string key)
    {
        if (TemplateKeys.Contains(key)) return QuestionsAndTemplates;
        if (ProcedureKeys.Contains(key)) return Procedures;
        if (BatchRecordKeys.Contains(key)) return BatchRecords;
        if (RunKeys.Contains(key)) return ProcedureRuns;
        return RndProductBatches;
    }

    private static PermissionDto CreatePermission(string submodule, string key)
    {
        var name = Regex.Replace(key, "(\\B[A-Z])", " $1");
        return new PermissionDto(
            Module,
            submodule,
            key,
            name,
            $"Allows the user to {name[3..].ToLowerInvariant()}."
        );
    }
}
