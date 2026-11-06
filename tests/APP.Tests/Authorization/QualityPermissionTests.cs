using System.Reflection;
using API.Authorization;
using API.Controllers;
using APP.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace APP.Tests.Authorization;

/// <summary>QC and QA writes: each task needs its own key on the server (audit phase 4).</summary>
public sealed class QualityPermissionTests
{
    private static string Policy(Type controller, string action, string? template = null) =>
        Assert.Single(controller.GetMethods().Where(method => method.Name == action
                && (template is null || method.GetCustomAttributes<HttpMethodAttribute>().Any(http => http.Template == template)))
            .SelectMany(method => method.GetCustomAttributes<AuthorizeAttribute>())).Policy!;

    [Theory]
    [InlineData(typeof(QualityAuditController), "CreateAudit", PermissionKeys.CanCreateQualityAudit)]
    [InlineData(typeof(QualityAuditController), "RaiseFinding", PermissionKeys.CanConductQualityAudit)]
    [InlineData(typeof(QualityAuditController), "VerifyCorrectiveActionEffectiveness", PermissionKeys.CanReviewCorrectiveAction)]
    [InlineData(typeof(QualityAuditController), "CloseAudit", PermissionKeys.CanCloseQualityAudit)]
    [InlineData(typeof(RoutineQcController), "AddSample", PermissionKeys.CanCreateRoutineTest)]
    [InlineData(typeof(RoutineQcController), "GenerateCertificate", PermissionKeys.CanPerformRoutineTest)]
    [InlineData(typeof(RoutineQcController), "CreateArd", PermissionKeys.CanManageRoutineSetup)]
    [InlineData(typeof(WaterQualityController), "HoldPeriod", PermissionKeys.CanManageWaterQuality)]
    [InlineData(typeof(AnalyticalTestRequestController), "CreateAnalyticalTestRequest", PermissionKeys.CanUpdateProductionStep)]
    [InlineData(typeof(AnalyticalTestRequestController), "AssignAnalyticalTestRequest", PermissionKeys.CanAssignProductTest)]
    [InlineData(typeof(AnalyticalTestRequestController), "DeleteAnalyticalTestRequest", PermissionKeys.CanDeleteAnalyticalTestRequest)]
    [InlineData(typeof(MaterialAnalyticalRawDataController), "SubmitResponse", PermissionKeys.CanSubmitUniformityOfWeight)]
    public void A_quality_write_needs_its_task_key(Type controller, string action, string key) => Assert.Equal(key, Policy(controller, action));

    [Fact]
    public void Each_test_request_update_route_has_its_own_key()
    {
        Assert.Equal(PermissionKeys.CanChangeAnalyticalTestRequestStatus, Policy(typeof(AnalyticalTestRequestController), "UpdateAnalyticalTestRequest", "status/{id:guid}"));
        Assert.Equal(PermissionKeys.CanEditAnalyticalTestRequest, Policy(typeof(AnalyticalTestRequestController), "UpdateAnalyticalTestRequest", "{id:guid}"));
    }

    [Fact]
    public void Starting_a_material_test_uses_the_key_for_the_batch_kind()
    {
        var attribute = Assert.Single(typeof(MaterialAnalyticalRawDataController).GetMethod("StartTestForMaterialBatch")!.GetCustomAttributes<MaterialKindPermissionAttribute>());
        Assert.Equal((PermissionKeys.CanStartRawMaterialTest, PermissionKeys.CanStartPackagingMaterialTest), (attribute.RawKey, attribute.PackagingKey));
    }
}
