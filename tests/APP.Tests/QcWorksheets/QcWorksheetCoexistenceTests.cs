using System.Reflection;
using APP.Utils;
using DOMAIN.Entities.QcWorksheets;
using Xunit;

namespace APP.Tests.QcWorksheets;

/// <summary>
/// Acceptance criterion 7 — coexistence. The rebuilt QC module must not collide with, or
/// quietly change the meaning of, anything in the live Material/Product/Packaging QC path.
/// </summary>
public class QcWorksheetCoexistenceTests
{
    private static IEnumerable<string> ConstantsOf(Type type) =>
        type.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!);

    /// <summary>No new permission key reuses an existing one.</summary>
    [Fact]
    public void Qc_worksheet_permission_keys_do_not_collide_with_existing_keys()
    {
        var existing = ConstantsOf(typeof(PermissionKeys)).ToHashSet();
        var added = ConstantsOf(typeof(QcWorksheetPermissionKeys)).ToList();

        Assert.NotEmpty(added);
        Assert.Empty(added.Where(existing.Contains));
    }

    /// <summary>
    /// Every new key is registered in the catalog, otherwise no role could ever hold it and
    /// the endpoints would be unreachable.
    /// </summary>
    [Fact]
    public void Every_qc_worksheet_permission_key_is_registered_and_grantable()
    {
        var keys = ConstantsOf(typeof(QcWorksheetPermissionKeys)).ToList();
        var generated = PermissionUtils.GeneratePermissions().ToList();
        var generatedKeys = generated.Select(permission => permission.Key).ToHashSet();

        foreach (var key in keys)
            Assert.Contains(key, generatedKeys);

        // Each QC worksheet key is registered exactly once. This is deliberately scoped to
        // the keys this module adds: the catalog as a whole already contains pre-existing
        // duplicates from other modules, which are out of scope here.
        foreach (var key in keys)
            Assert.Single(generated.Where(permission => permission.Key == key));
    }

    /// <summary>The brief's eleven keys, by name.</summary>
    [Fact]
    public void Permission_keys_match_the_brief()
    {
        var keys = ConstantsOf(typeof(QcWorksheetPermissionKeys)).OrderBy(key => key).ToList();

        Assert.Equal(
        [
            "CanApproveQcStp",
            "CanApproveWorksheetTemplate",
            "CanCreateQcStp",
            "CanCreateWorksheetTemplate",
            "CanEditQcStp",
            "CanEditWorksheetTemplate",
            "CanImportQcStp",
            "CanSupersedeQcStp",
            "CanSupersedeWorksheetTemplate",
            "CanViewQcStps",
            "CanViewWorksheetTemplates"
        ], keys);
    }

    /// <summary>
    /// The QC model types are distinct strings, so registering them with the approval engine
    /// cannot shadow an existing model type's dispatch.
    /// </summary>
    [Fact]
    public void Qc_model_types_are_namespaced_and_recognised()
    {
        Assert.Equal("QcStandardTestProcedure", QcWorksheetModelTypes.StandardTestProcedure);
        Assert.Equal("QcWorksheetTemplate", QcWorksheetModelTypes.WorksheetTemplate);

        Assert.True(QcWorksheetModelTypes.IsQcWorksheetModelType(
            QcWorksheetModelTypes.StandardTestProcedure));
        Assert.True(QcWorksheetModelTypes.IsQcWorksheetModelType(
            QcWorksheetModelTypes.WorksheetTemplate));

        // Existing model types must not be captured by the QC branch.
        foreach (var other in new[]
                 {
                     "PurchaseRequisition", "StockRequisition", "PurchaseOrder", "Response",
                     "RndProject", "BillingSheet", "StandardTestProcedure", "WorksheetTemplate"
                 })
        {
            Assert.False(QcWorksheetModelTypes.IsQcWorksheetModelType(other), other);
        }
    }

    /// <summary>
    /// EntityType values are the plain names the brief specifies, and map from the model
    /// types; anything else maps to null rather than being silently accepted.
    /// </summary>
    [Fact]
    public void Entity_type_mapping_is_explicit()
    {
        Assert.Equal(
            QcApprovalEntityTypes.StandardTestProcedure,
            QcApprovalEntityTypes.FromModelType(QcWorksheetModelTypes.StandardTestProcedure));

        Assert.Equal(
            QcApprovalEntityTypes.WorksheetTemplate,
            QcApprovalEntityTypes.FromModelType(QcWorksheetModelTypes.WorksheetTemplate));

        Assert.Null(QcApprovalEntityTypes.FromModelType("Response"));
        Assert.Null(QcApprovalEntityTypes.FromModelType("Specification"));
    }

    /// <summary>
    /// The new entities are their own types in their own namespace — they are not the live
    /// Material/Product STP entities under another name.
    /// </summary>
    [Fact]
    public void New_entities_live_in_the_qc_worksheets_namespace()
    {
        Assert.Equal("DOMAIN.Entities.QcWorksheets", typeof(StandardTestProcedure).Namespace);
        Assert.Equal("DOMAIN.Entities.QcWorksheets", typeof(WorksheetTemplate).Namespace);
        Assert.Equal("DOMAIN.Entities.QcWorksheets", typeof(QcApproval).Namespace);
    }
}
