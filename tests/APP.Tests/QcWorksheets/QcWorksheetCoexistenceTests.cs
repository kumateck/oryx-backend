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

    /// <summary>
    /// The exact key list, by name: Milestone 1's eleven plus Milestone 2's seven. Listing them
    /// explicitly is the point — a key added without a brief calling for it fails here.
    /// </summary>
    [Fact]
    public void Permission_keys_match_the_brief()
    {
        var keys = ConstantsOf(typeof(QcWorksheetPermissionKeys))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();

        // Milestone 2 adds the five-way split a controlled document gets for Specification,
        // plus two keys for SamplingPointGroup, which is plain reference data: View splits off
        // from Manage because read access to reference data is a distinct concern from admin
        // rights over it, while Create/Edit/Delete stay fused under the single Manage key.
        // Milestone 3 adds five operations keys and eleven test room keys — the execution
        // transitions split Chemical from Microbial, since those analysts are staffed
        // separately.
        Assert.Equal(
        [
            "CanApproveQcSpecification",
            "CanApproveQcStp",
            "CanApproveWorksheetTemplate",
            "CanAssignQcTestRequest",
            "CanAssignWorksheet",
            "CanCreateQcSpecification",
            "CanCreateQcStp",
            "CanCreateScheduledQcTestRequest",
            "CanCreateUnscheduledQcTestRequest",
            "CanCreateWorksheetTemplate",
            "CanEditQcSpecification",
            "CanEditQcStp",
            "CanEditWorksheetTemplate",
            "CanEnterChemicalWorksheetResult",
            "CanEnterMicrobialWorksheetResult",
            "CanImportQcStp",
            "CanManageSamplingPointGroups",
            "CanReassignWorksheet",
            "CanRecordQcSample",
            "CanReturnWorksheetForCorrection",
            "CanReviewChemicalWorksheet",
            "CanReviewMicrobialWorksheet",
            "CanStartChemicalWorksheet",
            "CanStartMicrobialWorksheet",
            "CanSubmitChemicalWorksheet",
            "CanSubmitMicrobialWorksheet",
            "CanSupersedeQcSpecification",
            "CanSupersedeQcStp",
            "CanSupersedeWorksheetTemplate",
            "CanViewQcSpecifications",
            "CanViewQcStps",
            "CanViewQcTestRequests",
            "CanViewSamplingPointGroups",
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
        Assert.Equal("QcSpecification", QcWorksheetModelTypes.Specification);
        Assert.Equal("QcWorksheetInstance", QcWorksheetModelTypes.WorksheetInstance);

        Assert.True(QcWorksheetModelTypes.IsQcWorksheetModelType(
            QcWorksheetModelTypes.WorksheetInstance));

        Assert.True(QcWorksheetModelTypes.IsQcWorksheetModelType(
            QcWorksheetModelTypes.StandardTestProcedure));
        Assert.True(QcWorksheetModelTypes.IsQcWorksheetModelType(
            QcWorksheetModelTypes.WorksheetTemplate));
        Assert.True(QcWorksheetModelTypes.IsQcWorksheetModelType(
            QcWorksheetModelTypes.Specification));

        // Existing model types must not be captured by the QC branch. "Specification" bare is
        // listed deliberately: the live Material/Product specification path must not be
        // routed into QC's approval handler by the new model type.
        foreach (var other in new[]
                 {
                     "PurchaseRequisition", "StockRequisition", "PurchaseOrder", "Response",
                     "RndProject", "BillingSheet", "StandardTestProcedure", "WorksheetTemplate",
                     "Specification", "MaterialSpecification", "ProductSpecification",

                     // The live analytical request path must not be captured by the QC branch
                     // either: it keeps running through its own approval dispatch.
                     "AnalyticalTestRequest", "WorksheetInstance", "TestRequest"
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

        Assert.Equal(
            QcApprovalEntityTypes.Specification,
            QcApprovalEntityTypes.FromModelType(QcWorksheetModelTypes.Specification));

        Assert.Equal(
            QcApprovalEntityTypes.WorksheetInstance,
            QcApprovalEntityTypes.FromModelType(QcWorksheetModelTypes.WorksheetInstance));

        Assert.Null(QcApprovalEntityTypes.FromModelType("Response"));

        // The bare EntityType string is not itself a model type: only "QcSpecification" maps.
        Assert.Null(QcApprovalEntityTypes.FromModelType("Specification"));
        Assert.Null(QcApprovalEntityTypes.FromModelType("MaterialSpecification"));
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
        Assert.Equal("DOMAIN.Entities.QcWorksheets", typeof(Specification).Namespace);
        Assert.Equal("DOMAIN.Entities.QcWorksheets", typeof(SpecificationWorksheetLink).Namespace);
        Assert.Equal("DOMAIN.Entities.QcWorksheets", typeof(SpecificationCharacteristic).Namespace);
        Assert.Equal("DOMAIN.Entities.QcWorksheets", typeof(SamplingPointGroup).Namespace);
        Assert.Equal("DOMAIN.Entities.QcWorksheets", typeof(TestRequest).Namespace);
        Assert.Equal("DOMAIN.Entities.QcWorksheets", typeof(TestRequestSubject).Namespace);
        Assert.Equal("DOMAIN.Entities.QcWorksheets", typeof(WorksheetInstance).Namespace);
        Assert.Equal("DOMAIN.Entities.QcWorksheets", typeof(WorksheetFieldValue).Namespace);
        Assert.Equal("DOMAIN.Entities.QcWorksheets", typeof(WorksheetInstanceReassignment).Namespace);
    }

    /// <summary>
    /// Criterion 8 — the new round is a distinct type on a distinct table from the live
    /// <c>AnalyticalTestRequest</c>, which keeps running untouched. A shared table name here
    /// would silently repoint the existing <c>qc/analytical-raw-data</c> pages.
    /// </summary>
    [Fact]
    public void Qc_test_request_is_a_separate_type_and_table_from_the_live_analytical_test_request()
    {
        var live = typeof(DOMAIN.Entities.Base.BaseEntity).Assembly
            .GetTypes()
            .FirstOrDefault(type => type.Name == "AnalyticalTestRequest");

        // The live entity still exists — this milestone did not replace or rename it.
        Assert.NotNull(live);
        Assert.NotEqual(typeof(TestRequest), live);
        Assert.NotEqual("DOMAIN.Entities.QcWorksheets", live.Namespace);

        using var harness = new QcWorksheetTestContext();
        var model = harness.Db.Model;

        // The live entity maps by EF's default naming rather than an explicit ToTable, so its
        // annotation is absent — which is itself the point: the new table is named explicitly
        // and cannot collide with it.
        var liveTable = model.FindEntityType(live)?.FindAnnotation("Relational:TableName")?.Value as string;
        var qcTable = (string)model.FindEntityType(typeof(TestRequest))!
            .FindAnnotation("Relational:TableName")!.Value!;

        Assert.Equal("QcTestRequests", qcTable);
        Assert.NotEqual(qcTable, liveTable);
    }

    /// <summary>
    /// Criterion 8 — the new Specification is a distinct type on a distinct table from the
    /// live <c>MaterialSpecification</c>/<c>ProductSpecification</c>, which keep working
    /// untouched. A shared table name here would silently repoint the existing pages.
    /// </summary>
    [Fact]
    public void Qc_specification_is_a_separate_type_and_table_from_the_live_specifications()
    {
        var liveMaterial = Type.GetType(
            "DOMAIN.Entities.Materials.MaterialSpecification, DOMAIN", throwOnError: false)
            ?? typeof(DOMAIN.Entities.Base.BaseEntity).Assembly
                .GetTypes()
                .FirstOrDefault(type => type.Name == "MaterialSpecification");

        // The live entity still exists — this milestone did not replace or rename it.
        Assert.NotNull(liveMaterial);
        Assert.NotEqual(typeof(Specification), liveMaterial);
        Assert.NotEqual("DOMAIN.Entities.QcWorksheets", liveMaterial.Namespace);

        var liveProduct = typeof(DOMAIN.Entities.Base.BaseEntity).Assembly
            .GetTypes()
            .FirstOrDefault(type => type.Name == "ProductSpecification");

        Assert.NotNull(liveProduct);
        Assert.NotEqual(typeof(Specification), liveProduct);
    }

    /// <summary>
    /// The new tables are namespaced with the module's Qc prefix, so none of them can collide
    /// with an existing table name.
    /// </summary>
    [Fact]
    public void Milestone_two_tables_are_namespaced()
    {
        using var harness = new QcWorksheetTestContext();
        var model = harness.Db.Model;

        // Read the mapped name from the annotation rather than the relational GetTableName
        // extension, which the in-memory test provider does not bring along.
        string TableNameOf(Type type) =>
            (string)model.FindEntityType(type)!.FindAnnotation("Relational:TableName")!.Value!;

        foreach (var type in new[]
                 {
                     typeof(Specification), typeof(SpecificationWorksheetLink),
                     typeof(SpecificationCharacteristic), typeof(SamplingPointGroup),
                     typeof(TestRequest), typeof(TestRequestSubject), typeof(WorksheetInstance),
                     typeof(WorksheetFieldValue), typeof(WorksheetInstanceReassignment)
                 })
        {
            Assert.StartsWith("Qc", TableNameOf(type), StringComparison.Ordinal);
        }

        // And specifically not the live tables.
        Assert.Equal("QcSpecifications", TableNameOf(typeof(Specification)));
        Assert.Equal("QcSamplingPointGroups", TableNameOf(typeof(SamplingPointGroup)));
        Assert.Equal("QcTestRequests", TableNameOf(typeof(TestRequest)));
        Assert.Equal("QcWorksheetInstances", TableNameOf(typeof(WorksheetInstance)));
    }
}
