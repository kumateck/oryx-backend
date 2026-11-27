using APP.Repository;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.ProductionSchedules;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

/// <summary>QA's routing of test results: material and finished products to the QA GM, bulk by Beta or non-Beta division.</summary>
public sealed class ResponseApprovalRouteTests
{
    private sealed class Actor : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => "NonProduction";
    }
    private static ApplicationDbContext Memory() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new Actor());

    // A response is read through its worksheet form (deleted forms hide their responses).
    private static Form Worksheet() => new() { Id = Guid.NewGuid(), Name = "Worksheet" };

    private static readonly string[] Routes =
    [
        ResponseApprovalRoute.General, ResponseApprovalRoute.Material, ResponseApprovalRoute.ProductFinished,
        ResponseApprovalRoute.IntermediateGeneral, ResponseApprovalRoute.IntermediateBeta
    ];

    private static void Configure(ApplicationDbContext db, params string[] itemTypes)
    {
        foreach (var itemType in itemTypes)
            db.Approvals.Add(new Approval { Id = Guid.NewGuid(), ItemType = itemType,
                ApprovalStages = [new ApprovalStage { Id = Guid.NewGuid(), Order = 0, Required = true, RoleId = Guid.NewGuid() }] });
    }

    private static Guid ProductResponse(ApplicationDbContext db, TestStage stage, Division division)
    {
        var bmr = Guid.NewGuid(); var step = Guid.NewGuid();
        var product = new Product { Id = Guid.NewGuid(), Code = "P", Name = "Product", Division = division };
        db.AnalyticalTestRequests.Add(new AnalyticalTestRequest { Id = Guid.NewGuid(), BatchManufacturingRecordId = bmr, ProductionActivityStepId = step,
            Stage = stage, ProductionScheduleProduct = new ProductionScheduleProduct { Id = Guid.NewGuid(), Product = product } });
        var response = new Response { Id = Guid.NewGuid(), Form = Worksheet(), BatchManufacturingRecordId = bmr, ProductionActivityStepId = step };
        db.Responses.Add(response);
        return response.Id;
    }

    [Theory]
    [InlineData(TestStage.Finished, Division.NonBetaLactam, ResponseApprovalRoute.ProductFinished)]
    [InlineData(TestStage.Finished, Division.BetaLactam, ResponseApprovalRoute.ProductFinished)]
    [InlineData(TestStage.Bulk, Division.NonBetaLactam, ResponseApprovalRoute.IntermediateGeneral)]
    [InlineData(TestStage.Intermediate, Division.NonBetaLactam, ResponseApprovalRoute.IntermediateGeneral)]
    [InlineData(TestStage.Bulk, Division.BetaLactam, ResponseApprovalRoute.IntermediateBeta)]
    [InlineData(TestStage.Intermediate, Division.BetaLactam, ResponseApprovalRoute.IntermediateBeta)]
    public async Task Product_results_go_to_the_workflow_for_their_stage_and_division(TestStage stage, Division division, string expected)
    {
        await using var db = Memory();
        Configure(db, Routes);
        var response = ProductResponse(db, stage, division);
        await db.SaveChangesAsync();
        Assert.Equal(expected, await ResponseApprovalRoute.ConfigurationTypeAsync(db, response));
    }

    [Fact]
    public async Task Material_results_go_to_the_material_workflow()
    {
        await using var db = Memory();
        Configure(db, Routes);
        var response = new Response { Id = Guid.NewGuid(), Form = Worksheet(), MaterialBatchId = Guid.NewGuid() };
        db.Responses.Add(response);
        await db.SaveChangesAsync();
        Assert.Equal(ResponseApprovalRoute.Material, await ResponseApprovalRoute.ConfigurationTypeAsync(db, response.Id));
    }

    [Fact]
    public async Task A_route_without_a_configured_stage_uses_the_general_workflow()
    {
        await using var db = Memory();
        Configure(db, ResponseApprovalRoute.General);
        db.Approvals.Add(new Approval { Id = Guid.NewGuid(), ItemType = ResponseApprovalRoute.IntermediateBeta, ApprovalStages = [] });
        var response = ProductResponse(db, TestStage.Bulk, Division.BetaLactam);
        await db.SaveChangesAsync();
        Assert.Equal(ResponseApprovalRoute.IntermediateBeta, await ResponseApprovalRoute.RouteAsync(db, response));
        Assert.Equal(ResponseApprovalRoute.General, await ResponseApprovalRoute.ConfigurationTypeAsync(db, response));
    }

    [Fact]
    public async Task Routine_results_keep_the_general_workflow()
    {
        await using var db = Memory();
        Configure(db, Routes);
        var response = new Response { Id = Guid.NewGuid(), Form = Worksheet(), RoutineTrackId = Guid.NewGuid() };
        db.Responses.Add(response);
        await db.SaveChangesAsync();
        Assert.Equal(ResponseApprovalRoute.General, await ResponseApprovalRoute.ConfigurationTypeAsync(db, response.Id));
    }
}
