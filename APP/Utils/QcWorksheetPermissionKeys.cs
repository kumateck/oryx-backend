using System.Reflection;
using System.Text.RegularExpressions;
using DOMAIN.Entities.Permissions;

namespace APP.Utils;

/// <summary>
/// Permission keys for the rebuilt QC module. One key per state-machine transition,
/// following the project's convention that every action/view gets its own dedicated key
/// and never shares one.
/// <para>
/// These are additive: they neither replace nor overlap the live Material/Product/
/// Packaging QC keys in <see cref="PermissionKeys"/>.
/// </para>
/// </summary>
public static class QcWorksheetPermissionKeys
{
    // Standard Test Procedures
    public const string CanViewQcStps = "CanViewQcStps";
    public const string CanCreateQcStp = "CanCreateQcStp";
    public const string CanEditQcStp = "CanEditQcStp";
    public const string CanApproveQcStp = "CanApproveQcStp";
    public const string CanSupersedeQcStp = "CanSupersedeQcStp";

    /// <summary>
    /// Deliberately separate from <see cref="CanCreateQcStp"/>: bulk-importing the existing
    /// STP library is a migration-scale activity worth restricting independently of
    /// everyday STP authoring.
    /// </summary>
    public const string CanImportQcStp = "CanImportQcStp";

    // Worksheet Templates
    public const string CanViewWorksheetTemplates = "CanViewWorksheetTemplates";
    public const string CanCreateWorksheetTemplate = "CanCreateWorksheetTemplate";
    public const string CanEditWorksheetTemplate = "CanEditWorksheetTemplate";
    public const string CanApproveWorksheetTemplate = "CanApproveWorksheetTemplate";
    public const string CanSupersedeWorksheetTemplate = "CanSupersedeWorksheetTemplate";

    // Specifications
    public const string CanViewQcSpecifications = "CanViewQcSpecifications";
    public const string CanCreateQcSpecification = "CanCreateQcSpecification";
    public const string CanEditQcSpecification = "CanEditQcSpecification";
    public const string CanApproveQcSpecification = "CanApproveQcSpecification";
    public const string CanSupersedeQcSpecification = "CanSupersedeQcSpecification";

    // Sampling Point Groups

    /// <summary>
    /// Read access to the sampling point group list, separate from
    /// <see cref="CanManageSamplingPointGroups"/>: a role with Specification-authoring rights
    /// but no reference-data management rights still has to list groups to populate the
    /// dropdown a characteristic's Alert/Action tier is grouped by. View is a genuinely
    /// distinct concern from Manage — many roles legitimately need read access to reference
    /// data without deserving admin rights over it.
    /// </summary>
    public const string CanViewSamplingPointGroups = "CanViewSamplingPointGroups";

    /// <summary>
    /// Create/edit/delete, as a single key rather than the create/edit/approve/supersede split
    /// the controlled documents get: a sampling point group is plain reference data maintained
    /// by the QC Manager, with no lifecycle of its own whose transitions could be gated
    /// separately. Only View splits off, and only because it is a different concern, not a
    /// different lifecycle stage.
    /// </summary>
    public const string CanManageSamplingPointGroups = "CanManageSamplingPointGroups";

    // Test requests (the ARD / round)
    public const string CanViewQcTestRequests = "CanViewQcTestRequests";

    /// <summary>
    /// Split from <see cref="CanCreateUnscheduledQcTestRequest"/> because the two are different
    /// authorities: a scheduled round is normally system-raised and this key only covers a
    /// manual override of one, while raising a round outside the schedule always demands a
    /// stated reason.
    /// </summary>
    public const string CanCreateScheduledQcTestRequest = "CanCreateScheduledQcTestRequest";

    public const string CanCreateUnscheduledQcTestRequest = "CanCreateUnscheduledQcTestRequest";
    public const string CanRecordQcSample = "CanRecordQcSample";
    public const string CanAssignQcTestRequest = "CanAssignQcTestRequest";

    // Test room — assignment
    public const string CanAssignWorksheet = "CanAssignWorksheet";

    /// <summary>
    /// Separate from <see cref="CanAssignWorksheet"/>: taking work off an analyst who may
    /// already have entered GxP-relevant data is a different decision from handing out work
    /// that nobody has started.
    /// </summary>
    public const string CanReassignWorksheet = "CanReassignWorksheet";

    // Test room — execution, split by analysis track. Chemical and Microbial analysts are
    // different people in different rooms, so each transition is grantable per track.
    public const string CanStartChemicalWorksheet = "CanStartChemicalWorksheet";
    public const string CanStartMicrobialWorksheet = "CanStartMicrobialWorksheet";
    public const string CanEnterChemicalWorksheetResult = "CanEnterChemicalWorksheetResult";
    public const string CanEnterMicrobialWorksheetResult = "CanEnterMicrobialWorksheetResult";
    public const string CanSubmitChemicalWorksheet = "CanSubmitChemicalWorksheet";
    public const string CanSubmitMicrobialWorksheet = "CanSubmitMicrobialWorksheet";
    public const string CanReviewChemicalWorksheet = "CanReviewChemicalWorksheet";
    public const string CanReviewMicrobialWorksheet = "CanReviewMicrobialWorksheet";
    public const string CanReturnWorksheetForCorrection = "CanReturnWorksheetForCorrection";

    // OOS cases. Three keys for three genuinely different authorities: running the Phase 1
    // lab-error check, deciding a retest is warranted, and signing the QA disposition that
    // rejects or releases a real batch. In a real lab these sit with a QC Officer, a QC
    // Manager and a QA Manager respectively, which is exactly why they are not one key.

    /// <summary>
    /// Read access to the OOS queue and to a single case, separate from
    /// <see cref="CanInvestigateQcOosCase"/> for the same reason
    /// <see cref="CanViewSamplingPointGroups"/> is separate from its Manage key: reading is a
    /// different concern from acting.
    /// <para>
    /// The concrete failure this fixes is a QA Manager holding only
    /// <see cref="CanDispositionQcOosCase"/> — the person who must sign the disposition —
    /// being unable to load the case they are signing. The QC Manager holding only
    /// <see cref="CanAuthorizeQcOosRetest"/> was locked out the same way. Both authorities act
    /// on a case they could not read, which is not a workable split.
    /// </para>
    /// </summary>
    public const string CanViewQcOosCases = "CanViewQcOosCases";

    public const string CanInvestigateQcOosCase = "CanInvestigateQcOosCase";
    public const string CanAuthorizeQcOosRetest = "CanAuthorizeQcOosRetest";
    public const string CanDispositionQcOosCase = "CanDispositionQcOosCase";
}

/// <summary>
/// Registers the QC worksheet keys with the permission catalog, so they can actually be
/// granted to a role. Mirrors <see cref="FullProcedurePermissionCatalog"/>.
/// </summary>
public static class QcWorksheetPermissionCatalog
{
    public const string Module = "Quality Control";
    public const string StandardTestProcedures = "QC Standard Test Procedures";
    public const string WorksheetTemplates = "QC Worksheet Templates";
    public const string Specifications = "QC Specifications";
    public const string SamplingPointGroups = "QC Sampling Point Groups";
    public const string TestRequests = "QC Test Requests";
    public const string TestRoom = "QC Test Room";
    public const string OosCases = "QC OOS Cases";

    private static readonly HashSet<string> StpKeys =
    [
        QcWorksheetPermissionKeys.CanViewQcStps,
        QcWorksheetPermissionKeys.CanCreateQcStp,
        QcWorksheetPermissionKeys.CanEditQcStp,
        QcWorksheetPermissionKeys.CanApproveQcStp,
        QcWorksheetPermissionKeys.CanSupersedeQcStp,
        QcWorksheetPermissionKeys.CanImportQcStp
    ];

    private static readonly HashSet<string> SpecificationKeys =
    [
        QcWorksheetPermissionKeys.CanViewQcSpecifications,
        QcWorksheetPermissionKeys.CanCreateQcSpecification,
        QcWorksheetPermissionKeys.CanEditQcSpecification,
        QcWorksheetPermissionKeys.CanApproveQcSpecification,
        QcWorksheetPermissionKeys.CanSupersedeQcSpecification
    ];

    private static readonly HashSet<string> SamplingPointGroupKeys =
    [
        QcWorksheetPermissionKeys.CanViewSamplingPointGroups,
        QcWorksheetPermissionKeys.CanManageSamplingPointGroups
    ];

    private static readonly HashSet<string> TestRequestKeys =
    [
        QcWorksheetPermissionKeys.CanViewQcTestRequests,
        QcWorksheetPermissionKeys.CanCreateScheduledQcTestRequest,
        QcWorksheetPermissionKeys.CanCreateUnscheduledQcTestRequest,
        QcWorksheetPermissionKeys.CanRecordQcSample,
        QcWorksheetPermissionKeys.CanAssignQcTestRequest
    ];

    private static readonly HashSet<string> TestRoomKeys =
    [
        QcWorksheetPermissionKeys.CanAssignWorksheet,
        QcWorksheetPermissionKeys.CanReassignWorksheet,
        QcWorksheetPermissionKeys.CanStartChemicalWorksheet,
        QcWorksheetPermissionKeys.CanStartMicrobialWorksheet,
        QcWorksheetPermissionKeys.CanEnterChemicalWorksheetResult,
        QcWorksheetPermissionKeys.CanEnterMicrobialWorksheetResult,
        QcWorksheetPermissionKeys.CanSubmitChemicalWorksheet,
        QcWorksheetPermissionKeys.CanSubmitMicrobialWorksheet,
        QcWorksheetPermissionKeys.CanReviewChemicalWorksheet,
        QcWorksheetPermissionKeys.CanReviewMicrobialWorksheet,
        QcWorksheetPermissionKeys.CanReturnWorksheetForCorrection
    ];

    private static readonly HashSet<string> OosCaseKeys =
    [
        QcWorksheetPermissionKeys.CanViewQcOosCases,
        QcWorksheetPermissionKeys.CanInvestigateQcOosCase,
        QcWorksheetPermissionKeys.CanAuthorizeQcOosRetest,
        QcWorksheetPermissionKeys.CanDispositionQcOosCase
    ];

    public static IReadOnlyList<PermissionDto> Generate()
    {
        return typeof(QcWorksheetPermissionKeys)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(field => (string)field.GetRawConstantValue()!)
            .Select(key => CreatePermission(SubmoduleFor(key), key))
            .ToList();
    }

    private static string SubmoduleFor(string key)
    {
        if (StpKeys.Contains(key)) return StandardTestProcedures;
        if (SpecificationKeys.Contains(key)) return Specifications;
        if (TestRequestKeys.Contains(key)) return TestRequests;
        if (TestRoomKeys.Contains(key)) return TestRoom;
        if (OosCaseKeys.Contains(key)) return OosCases;
        if (SamplingPointGroupKeys.Contains(key)) return SamplingPointGroups;
        return WorksheetTemplates;
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
