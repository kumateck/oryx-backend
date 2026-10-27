using API.Controllers;
using APP.Repository;
using APP.Services.Materials;
using APP.Utils;
using DOMAIN.Entities.OosInvestigations;
using DOMAIN.Entities.QcWorksheets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.QcWorksheets;

public class WorkflowCompatibilityPermissionTests
{
    [Fact]
    public async Task Oos_writes_check_current_grants_even_when_controller_is_invoked_without_policy_middleware()
    {
        using var harness = new QcWorksheetTestContext();
        var http = new DefaultHttpContext();
        http.Items["Sub"] = Guid.NewGuid().ToString();
        var controller = new OosInvestigationController(new OosInvestigationRepository(harness.Db, harness.Mapper), harness.Db)
            { ControllerContext = new ControllerContext { HttpContext = http } };
        Assert.IsType<ForbidHttpResult>(await controller.InitiateOosInvestigation(new InitiateOosInvestigationRequest()));
        Assert.IsType<ForbidHttpResult>(await controller.UpdateOosInvestigation(Guid.NewGuid(), new DOMAIN.Entities.OosInvestigations.UpdateOosInvestigationRequest()));
        Assert.IsType<ForbidHttpResult>(await controller.SubmitToQa(Guid.NewGuid()));
        Assert.IsType<ForbidHttpResult>(await controller.ReviewByQa(Guid.NewGuid(), new ReviewOosInvestigationRequest()));
        Assert.Empty(await harness.Db.OosInvestigations.ToListAsync());
    }

    [Fact]
    public async Task Retest_creation_requires_both_current_grants_before_creating_testing()
    {
        using var harness = new QcWorksheetTestContext();
        var service = new MaterialRetestService(harness.Db, harness.ApprovalRepository, harness.SignatureService, harness.TestRequests);
        var controller = new MaterialRetestController(service, harness.Db)
            { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        controller.HttpContext.Items["Sub"] = Guid.NewGuid().ToString();
        Assert.IsType<ForbidHttpResult>(await controller.CreateTesting(Guid.NewGuid(), new CreateTestRequestRequest()));
        Assert.Empty(await harness.Db.QcTestRequests.ToListAsync());
    }

    [Fact]
    public async Task Generic_attachment_routes_cannot_read_or_replace_governed_worksheet_evidence()
    {
        var controller = new FileController(null!, null!);
        Assert.IsType<ForbidHttpResult>(await controller.UploadFile("QcWorksheetInstance", Guid.NewGuid(), "reference", null!));
        Assert.IsType<ForbidHttpResult>(await controller.UploadFile("qcworksheetinstance", Guid.NewGuid(), new List<IFormFile>()));
        Assert.IsType<ForbidHttpResult>(await controller.GetImage("QcWorksheetInstance", Guid.NewGuid(), "reference"));
        Assert.IsType<ForbidHttpResult>(await controller.GetImage("qcworksheetinstance", "id/reference"));
    }

    [Fact]
    public void Legacy_oos_permissions_are_explicit_and_grantable()
    {
        var catalogue = PermissionUtils.GeneratePermissions();
        foreach (var key in new[] { PermissionKeys.CanInitiateOosInvestigation, PermissionKeys.CanViewOosInvestigations,
                     PermissionKeys.CanSubmitOosInvestigation, PermissionKeys.CanReviewOosInvestigation })
            Assert.Single(catalogue, item => item.Key == key);
    }
}
