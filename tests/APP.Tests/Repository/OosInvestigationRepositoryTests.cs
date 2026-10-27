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

file class NoCurrentUserService(Guid? actorId = null) : ICurrentUserService
{
    public Guid? UserId => actorId;
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
    private static ApplicationDbContext CreateContext(Guid? actorId = null) =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            new NoCurrentUserService(actorId)
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
        Assert.False((await repository.ReviewByQa(investigation.Id,
            new ReviewOosInvestigationRequest { Approve = true, Comments = "Incomplete" }, Guid.NewGuid())).IsSuccess);
        investigation.RootCauseAnalysis = "Calibration drift";
        investigation.CorrectiveActions = "Recalibrate and repeat testing";
        investigation.PreventiveActions = "Daily instrument checks";
        investigation.InvestigationDetails = "Bench records and current results reviewed";
        await context.SaveChangesAsync();
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

    [Fact]
    public async Task Initiation_requires_exact_subject_rejected_results_and_refuses_duplicates()
    {
        var actor = Guid.NewGuid();
        await using var context = CreateContext(actor);
        var repository = CreateRepository(context);
        var atr = SeedAtr(context);
        await context.SaveChangesAsync();
        var response = await context.Responses.SingleAsync();
        response.Approved = false;
        response.Rejected = true;
        response.RejectionOutcome = ResponseRejectionOutcome.Rework;
        response.CreatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        var request = new InitiateOosInvestigationRequest
        {
            AnalyticalTestRequestId = atr.Id, CoaNumber = "COA-OOS-1", RejectionReason = "Failed assay",
        };
        var result = await repository.InitiateOosInvestigation(request, actor);
        Assert.True(result.IsSuccess, result.Error?.Description);
        var duplicate = await repository.InitiateOosInvestigation(request, actor);
        Assert.False(duplicate.IsSuccess);
        Assert.Single(await context.OosInvestigations.ToListAsync());
        Assert.Equal(actor, (await context.OosInvestigations.SingleAsync()).CreatedById);
        var missing = await repository.InitiateOosInvestigation(new InitiateOosInvestigationRequest
        { CoaNumber = "COA", RejectionReason = "Failure" }, actor);
        Assert.False(missing.IsSuccess);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Initiation_cannot_reopen_approved_or_disposed_results(bool disposed)
    {
        await using var context = CreateContext();
        var atr = SeedAtr(context);
        await context.SaveChangesAsync();
        var response = await context.Responses.SingleAsync();
        response.Approved = !disposed;
        response.Rejected = disposed;
        response.RejectionOutcome = disposed ? ResponseRejectionOutcome.Disposed : null;
        await context.SaveChangesAsync();
        var result = await CreateRepository(context).InitiateOosInvestigation(new InitiateOosInvestigationRequest
        { AnalyticalTestRequestId = atr.Id, CoaNumber = "COA", RejectionReason = "Failure" }, Guid.NewGuid());
        Assert.False(result.IsSuccess);
        Assert.Empty(await context.OosInvestigations.ToListAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Initiation_checks_the_selected_tests_cycle_without_confusing_another_analysis(bool sameForm)
    {
        await using var context = CreateContext();
        var atr = SeedAtr(context);
        await context.SaveChangesAsync();
        var rejected = await context.Responses.SingleAsync();
        rejected.Approved = false;
        rejected.Rejected = true;
        rejected.RejectionOutcome = ResponseRejectionOutcome.Rework;
        var otherForm = new Form { Id = Guid.NewGuid(), Name = "Other analysis" };
        context.Forms.Add(otherForm);
        context.Responses.Add(new Response { Id = Guid.NewGuid(), FormId = sameForm ? rejected.FormId : otherForm.Id,
            BatchManufacturingRecordId = atr.BatchManufacturingRecordId, ProductionActivityStepId = atr.ProductionActivityStepId,
            ReworkSequence = 1, CreatedAt = DateTime.UtcNow, Approved = true });
        await context.SaveChangesAsync();
        var result = await CreateRepository(context).InitiateOosInvestigation(new InitiateOosInvestigationRequest
            { AnalyticalTestRequestId = atr.Id, ResponseId = rejected.Id, CoaNumber = "COA-EXACT", RejectionReason = "Assay failed" }, Guid.NewGuid());
        Assert.Equal(!sameForm, result.IsSuccess);
    }

    [Fact]
    public async Task Submission_requires_complete_findings_and_review_requires_stage_and_separation()
    {
        await using var context = CreateContext();
        var actor = Guid.NewGuid();
        var investigation = new OosInvestigation { Id = Guid.NewGuid(), CoaNumber = "COA", RejectionReason = "Failed", CreatedById = actor };
        context.OosInvestigations.Add(investigation);
        await context.SaveChangesAsync();
        var repository = CreateRepository(context);
        Assert.False((await repository.SubmitToQa(investigation.Id, actor)).IsSuccess);
        Assert.False((await repository.ReviewByQa(investigation.Id,
            new ReviewOosInvestigationRequest { Approve = false, Comments = "Confirmed" }, Guid.NewGuid())).IsSuccess);
        investigation.RootCauseAnalysis = "Calibration drift";
        investigation.CorrectiveActions = "Recalibrate";
        investigation.PreventiveActions = "Daily verification";
        investigation.InvestigationDetails = "Bench records inspected";
        investigation.RootCauseAnalysis = "<p>&nbsp;</p>";
        await context.SaveChangesAsync();
        Assert.False((await repository.SubmitToQa(investigation.Id, actor)).IsSuccess);
        investigation.RootCauseAnalysis = "Calibration drift";
        await context.SaveChangesAsync();
        Assert.True((await repository.SubmitToQa(investigation.Id, actor)).IsSuccess);
        Assert.False((await repository.SubmitToQa(investigation.Id, actor)).IsSuccess);
        Assert.False((await repository.ReviewByQa(investigation.Id,
            new ReviewOosInvestigationRequest { Approve = false, Comments = "Confirmed" }, actor)).IsSuccess);
    }
}
