using System.Reflection;
using API.Authorization;
using API.Controllers;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace APP.Tests.Authorization;

/// <summary>QC and QA reads: each needs a key of a page that shows it (audit phase 4).</summary>
public sealed class QualityReadPermissionTests
{
    private static IReadOnlyList<string> Keys(Type controller, string name, string? template = null) =>
        Assert.Single(Assert.Single(controller.GetMethods(), method => method.Name == name
                && (template is null || method.GetCustomAttributes<HttpMethodAttribute>().Any(http => http.Template == template)))
            .GetCustomAttributes<AnyPermissionAttribute>()).Keys;

    [Theory]
    [InlineData(typeof(MaterialSpecificationController), "GetMaterialSpecifications", "CanViewRawMaterialSpecifications")]
    [InlineData(typeof(MaterialSpecificationController), "GetMaterialSpecifications", "CanViewPackagingMaterialSpecifications")]
    [InlineData(typeof(ProductStandardTestProcedureController), "GetStandardTestProcedure", "CanViewProductStps")]
    [InlineData(typeof(AnalyticalTestRequestController), "GetAnalyticalTestRequests", "CanViewAnalyticalTestRequests")]
    [InlineData(typeof(RoutineQcController), "List", "CanViewRoutineTests")]
    [InlineData(typeof(QualityAuditController), "GetAudits", "CanViewQualityAudits")]
    [InlineData(typeof(QcWorksheetFilesController), "Read", "CanViewQcTestRequests")]
    [InlineData(typeof(WaterQualityController), "ListPeriods", "CanViewWaterQuality")]
    public void A_quality_read_takes_the_key_of_a_page_that_shows_it(Type controller, string name, string key) =>
        Assert.Contains(key, Keys(controller, name));

    [Fact]
    public void Stp_documents_keep_their_owner_check_in_the_action() =>
        Assert.All(typeof(StpDocumentController).GetMethods().Where(method => method.Name is "GetDocument" or "GetVersionFile"),
            method => Assert.NotNull(method.GetCustomAttribute<PermissionCheckedInActionAttribute>()));
}
