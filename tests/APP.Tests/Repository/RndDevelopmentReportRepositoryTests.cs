using System.Text;
using APP.Mapper;
using APP.Repository;
using APP.Services.Pdf;
using AutoMapper;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.RndFormulations;
using DOMAIN.Entities.RndProjects;
using DOMAIN.Entities.RndTrialBatches;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SHARED;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class NoCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

// Real PDF rendering (DinkToPdf/wkhtmltopdf) needs native libraries not
// available in a unit test run - a fake that echoes the generated HTML back
// as bytes lets these tests verify the data-aggregation logic (the actual
// thing this repository is responsible for) without needing real rendering.
file class FakePdfService : IPdfService
{
    public string LastHtml { get; private set; }

    public byte[] GeneratePdfFromHtml(string htmlContent)
    {
        LastHtml = htmlContent;
        return Encoding.UTF8.GetBytes(htmlContent);
    }
}

public class RndDevelopmentReportRepositoryTests
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
        services.AddHttpContextAccessor();
        services.AddAutoMapper(cfg => { }, typeof(OryxMapper));
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    [Fact]
    public async Task GenerateDevelopmentReportPdf_returns_not_found_for_missing_project()
    {
        await using var context = CreateContext();
        var repository = new RndDevelopmentReportRepository(context, CreateMapper(), new FakePdfService());

        var result = await repository.GenerateDevelopmentReportPdf(Guid.NewGuid());

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task GenerateDevelopmentReportPdf_includes_formulation_and_trial_batch_data()
    {
        await using var context = CreateContext();
        var pdfService = new FakePdfService();
        var repository = new RndDevelopmentReportRepository(context, CreateMapper(), pdfService);

        // RndProject.RequestedBy and Department are both required FKs, and
        // both User and Department carry a global soft-delete query filter -
        // the detail query's Include(RequestedBy)/Include(Department)
        // silently excludes the whole project row if either FK doesn't
        // resolve to a real row.
        var requestedBy = new User { Id = Guid.NewGuid() };
        context.Users.Add(requestedBy);
        var department = new Department { Id = Guid.NewGuid(), Name = "R&D", Type = DepartmentType.RnD };
        context.Departments.Add(department);
        var project = new RndProject
        {
            Id = Guid.NewGuid(),
            Code = "RND-2026-0007",
            Title = "Report test project",
            DepartmentId = department.Id,
            RequestedById = requestedBy.Id,
            Status = RndProjectStatus.InDevelopment,
            Approved = true,
        };
        var formulation = new RndFormulation
        {
            Id = Guid.NewGuid(),
            RndProjectId = project.Id,
            Version = 3,
            Status = RndFormulationStatus.Approved,
        };
        var trialBatch = new RndTrialBatch
        {
            Id = Guid.NewGuid(),
            RndProjectId = project.Id,
            RndFormulationId = formulation.Id,
            BatchCode = "TRB-2026-0099",
            ScaleType = RndTrialBatchScaleType.PilotScale,
            BatchSize = 5,
            Status = RndTrialBatchStatus.Completed,
        };
        context.AddRange(project, formulation, trialBatch);
        await context.SaveChangesAsync();

        var result = await repository.GenerateDevelopmentReportPdf(project.Id);

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Description : "");
        Assert.Contains(project.Code, pdfService.LastHtml);
        Assert.Contains("TRB-2026-0099", pdfService.LastHtml);
        Assert.Contains("PilotScale", pdfService.LastHtml);
    }
}
