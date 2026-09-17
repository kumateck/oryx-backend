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

    /// <summary>
    /// A single key rather than the view/create/edit/approve/supersede split the controlled
    /// documents get: a sampling point group is plain reference data maintained by the QC
    /// Manager, with no lifecycle of its own whose transitions could be gated separately.
    /// </summary>
    public const string CanManageSamplingPointGroups = "CanManageSamplingPointGroups";
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
        if (key == QcWorksheetPermissionKeys.CanManageSamplingPointGroups) return SamplingPointGroups;
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
