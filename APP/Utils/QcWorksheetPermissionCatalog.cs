using System.Reflection;
using System.Text.RegularExpressions;
using DOMAIN.Entities.Permissions;

namespace APP.Utils;

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
    public const string SamplingPoints = "QC Sampling Points";
    public const string TestRequests = "QC Test Requests";
    public const string TestRoom = "QC Test Room";
    public const string OosCases = "QC OOS Cases";
    public const string Certificates = "QC Certificates";
    public const string MonitoringPrograms = "QC Monitoring Programs";
    public const string WaterQuality = "QC Water Quality";
    public const string ApprovalHistory = "QC Approval History";

    private static readonly HashSet<string> StpKeys =
    [
        QcWorksheetPermissionKeys.CanViewQcStps,
        QcWorksheetPermissionKeys.CanCreateQcStp,
        QcWorksheetPermissionKeys.CanEditQcStp,
        QcWorksheetPermissionKeys.CanApproveQcStp,
        QcWorksheetPermissionKeys.CanSupersedeQcStp,
        QcWorksheetPermissionKeys.CanImportQcStp
    ];

    private static readonly HashSet<string> WorksheetTemplateKeys =
    [
        QcWorksheetPermissionKeys.CanViewWorksheetTemplates,
        QcWorksheetPermissionKeys.CanCreateWorksheetTemplate,
        QcWorksheetPermissionKeys.CanEditWorksheetTemplate,
        QcWorksheetPermissionKeys.CanApproveWorksheetTemplate,
        QcWorksheetPermissionKeys.CanSupersedeWorksheetTemplate,
        QcWorksheetPermissionKeys.CanImportQcWorksheetTemplates
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

    private static readonly HashSet<string> SamplingPointKeys =
    [
        QcWorksheetPermissionKeys.CanViewSamplingPoints,
        QcWorksheetPermissionKeys.CanManageSamplingPoints
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

    private static readonly HashSet<string> CertificateKeys =
    [
        QcWorksheetPermissionKeys.CanViewQcCertificate,
        QcWorksheetPermissionKeys.CanIssueQcCertificate,
        QcWorksheetPermissionKeys.CanReviseQcCertificate
    ];

    private static readonly HashSet<string> MonitoringProgramKeys =
    [
        QcWorksheetPermissionKeys.CanViewMonitoringPrograms,
        QcWorksheetPermissionKeys.CanCreateMonitoringProgram,
        QcWorksheetPermissionKeys.CanEditMonitoringProgram,
        QcWorksheetPermissionKeys.CanPauseMonitoringProgram
    ];

    private static readonly HashSet<string> WaterQualityKeys =
    [
        QcWorksheetPermissionKeys.CanViewQcWaterQualityPeriods,
        QcWorksheetPermissionKeys.CanActivateQcWaterQualityPeriod,
        QcWorksheetPermissionKeys.CanHoldQcWaterQualityPeriod,
        QcWorksheetPermissionKeys.CanRecordQcWaterUse
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
        if (WorksheetTemplateKeys.Contains(key)) return WorksheetTemplates;
        if (SpecificationKeys.Contains(key)) return Specifications;
        if (TestRequestKeys.Contains(key)) return TestRequests;
        if (TestRoomKeys.Contains(key)) return TestRoom;
        if (OosCaseKeys.Contains(key)) return OosCases;
        if (CertificateKeys.Contains(key)) return Certificates;
        if (MonitoringProgramKeys.Contains(key)) return MonitoringPrograms;
        if (WaterQualityKeys.Contains(key)) return WaterQuality;
        if (SamplingPointGroupKeys.Contains(key)) return SamplingPointGroups;
        if (SamplingPointKeys.Contains(key)) return SamplingPoints;
        if (key == QcWorksheetPermissionKeys.CanViewQcApprovalHistory) return ApprovalHistory;
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
