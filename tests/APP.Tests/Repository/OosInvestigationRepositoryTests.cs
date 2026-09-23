using APP.Mapper;
using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.OosInvestigations;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.ProductAnalyticalRawData;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.ProductStandardTestProcedures;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.QualityRoutines;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class NoCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

/// <summary>
/// ReviewByQa's reject branch only updated MaterialBatch.Status - an ATR-based OOS
/// investigation that was permanently rejected left the AnalyticalTestRequest's status
/// untouched, so it stayed stuck on Testing forever. See AnalyticalTestStatus.Rejected.
/// </summary>
public class OosInvestigationRepositoryTests
{
    private static ApplicationDbContext CreateContext() =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            new NoCurrentUserService()
        );

    private static IMapper CreateMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => { }, typeof(OryxMapper));
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    private static OosInvestigationRepository CreateRepository(ApplicationDbContext context) =>
        new(context, CreateMapper());

    private static AnalyticalTestRequest SeedAtr(ApplicationDbContext context)
    {
        var product = new Product { Id = Guid.NewGuid(), Name = "Test product" };
        var scheduleProduct = new ProductionScheduleProduct
        {
            Id = Guid.NewGuid(), ProductId = product.Id, Product = product
        };
        var form = new Form { Id = Guid.NewGuid(), Name = "Chemical worksheet" };
        var stp = new ProductStandardTestProcedure
        {
            Id = Guid.NewGuid(), ProductId = product.Id, Product = product,
            StpNumber = "STP-TEST"
        };
        var ard = new ProductAnalyticalRawData
        {
            Id = Guid.NewGuid(), StpId = stp.Id,
            ProductStandardTestProcedure = stp, FormId = form.Id, Form = form,
            Stage = TestStage.Intermediate, AnalysisType = AnalysisType.Chemical,
            IsVerified = true
        };
        var atr = new AnalyticalTestRequest
        {
            Id = Guid.NewGuid(), Status = AnalyticalTestStatus.Testing,
            ExpiryDate = DateTime.UtcNow.AddYears(1),
            ManufacturingDate = DateTime.UtcNow,
            ProductionScheduleProductId = scheduleProduct.Id,
            BatchManufacturingRecordId = Guid.NewGuid(),
            ProductionActivityStepId = Guid.NewGuid(),
            ChemicalArdId = ard.Id, Stage = TestStage.Intermediate
        };
        var response = new Response
        {
            Id = Guid.NewGuid(), FormId = form.Id, Form = form,
            BatchManufacturingRecordId = atr.BatchManufacturingRecordId,
            ProductionActivityStepId = atr.ProductionActivityStepId,
            Approved = true
        };
        context.AddRange(product, scheduleProduct, form, stp, ard, atr, response);
        return atr;
    }

    [Fact]
    public async Task ReviewByQa_reject_sets_atr_status_to_rejected()
    {
        await using var context = CreateContext();
        var atr = SeedAtr(context);
        var investigation = new OosInvestigation
        {
            Id = Guid.NewGuid(),
            AnalyticalTestRequestId = atr.Id,
            CoaNumber = "COA-1",
            RejectionReason = "Out of specification",
            Status = OosInvestigationStatus.SubmittedToQa,
        };
        context.OosInvestigations.Add(investigation);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);
        var result = await repository.ReviewByQa(
            investigation.Id,
            new ReviewOosInvestigationRequest { Approve = false, Comments = "Confirmed failure" },
            Guid.NewGuid()
        );

        Assert.True(result.IsSuccess);
        var updatedAtr = await context.AnalyticalTestRequests.FirstAsync(a => a.Id == atr.Id);
        Assert.Equal(AnalyticalTestStatus.Rejected, updatedAtr.Status);
        var updatedInvestigation = await context.OosInvestigations.FirstAsync(o => o.Id == investigation.Id);
        Assert.Equal(OosInvestigationStatus.PermanentlyRejected, updatedInvestigation.Status);
    }

    [Fact]
    public async Task ReviewByQa_approve_releases_atr()
    {
        await using var context = CreateContext();
        var atr = SeedAtr(context);
        var investigation = new OosInvestigation
        {
            Id = Guid.NewGuid(),
            AnalyticalTestRequestId = atr.Id,
            CoaNumber = "COA-2",
            RejectionReason = "Out of specification",
            Status = OosInvestigationStatus.SubmittedToQa,
        };
        context.OosInvestigations.Add(investigation);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);
        var result = await repository.ReviewByQa(
            investigation.Id,
            new ReviewOosInvestigationRequest { Approve = true, Comments = "Root cause resolved" },
            Guid.NewGuid()
        );

        Assert.True(result.IsSuccess);
        var updatedAtr = await context.AnalyticalTestRequests.FirstAsync(a => a.Id == atr.Id);
        Assert.Equal(AnalyticalTestStatus.Released, updatedAtr.Status);
    }

    [Fact]
    public async Task UpdateOosInvestigation_succeeds_while_initiated()
    {
        await using var context = CreateContext();
        var investigation = new OosInvestigation
        {
            Id = Guid.NewGuid(),
            MaterialBatchId = Guid.NewGuid(),
            CoaNumber = "COA-3",
            RejectionReason = "Out of specification",
            Status = OosInvestigationStatus.Initiated,
        };
        context.OosInvestigations.Add(investigation);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);
        var result = await repository.UpdateOosInvestigation(
            investigation.Id,
            new UpdateOosInvestigationRequest { RootCauseAnalysis = "Instrument calibration drift" },
            Guid.NewGuid()
        );

        Assert.True(result.IsSuccess);
        var updated = await context.OosInvestigations.FirstAsync(o => o.Id == investigation.Id);
        Assert.Equal("Instrument calibration drift", updated.RootCauseAnalysis);
    }

    [Fact]
    public async Task UpdateOosInvestigation_fails_once_submitted_to_qa()
    {
        await using var context = CreateContext();
        var investigation = new OosInvestigation
        {
            Id = Guid.NewGuid(),
            MaterialBatchId = Guid.NewGuid(),
            CoaNumber = "COA-4",
            RejectionReason = "Out of specification",
            Status = OosInvestigationStatus.SubmittedToQa,
        };
        context.OosInvestigations.Add(investigation);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);
        var result = await repository.UpdateOosInvestigation(
            investigation.Id,
            new UpdateOosInvestigationRequest { RootCauseAnalysis = "Should not apply" },
            Guid.NewGuid()
        );

        Assert.False(result.IsSuccess);
    }
}
