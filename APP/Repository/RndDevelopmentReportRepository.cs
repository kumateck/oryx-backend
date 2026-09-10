using APP.IRepository;
using APP.Services.Pdf;
using AutoMapper;
using DOMAIN.Entities.RndAnalyticalMethods;
using DOMAIN.Entities.RndFormulations;
using DOMAIN.Entities.RndProjects;
using DOMAIN.Entities.RndStabilityStudies;
using DOMAIN.Entities.RndTechnologyTransfers;
using DOMAIN.Entities.RndTrialBatches;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class RndDevelopmentReportRepository(
    ApplicationDbContext context,
    IMapper mapper,
    IPdfService pdfService
) : IRndDevelopmentReportRepository
{
    public async Task<Result<byte[]>> GenerateDevelopmentReportPdf(Guid rndProjectId)
    {
        var project = await context
            .RndProjects.AsSplitQuery()
            .Include(p => p.Product)
            .Include(p => p.Department)
            .Include(p => p.RequestedBy)
            .FirstOrDefaultAsync(p => p.Id == rndProjectId);
        if (project is null)
            return Error.NotFound("RndDevelopmentReport.ProjectNotFound", "R&D project not found.");

        var formulations = await context
            .RndFormulations.AsSplitQuery()
            .Include(f => f.Items)
                .ThenInclude(i => i.Material)
            .Where(f => f.RndProjectId == rndProjectId)
            .OrderBy(f => f.Version)
            .ToListAsync();

        var trialBatches = await context
            .RndTrialBatches.Where(b => b.RndProjectId == rndProjectId)
            .OrderBy(b => b.BatchCode)
            .ToListAsync();

        var analyticalMethods = await context
            .RndAnalyticalMethods.AsSplitQuery()
            .Include(m => m.Material)
            .Include(m => m.Product)
            .Where(m => m.RndProjectId == rndProjectId)
            .OrderBy(m => m.MethodName)
            .ToListAsync();

        var stabilityStudies = await context
            .RndStabilityStudies.AsSplitQuery()
            .Include(s => s.RndStabilityChamber)
            .Include(s => s.PullPoints)
            .Where(s => s.RndProjectId == rndProjectId)
            .OrderBy(s => s.StartDate)
            .ToListAsync();

        var technologyTransfers = await context
            .RndTechnologyTransfers.Where(t => t.RndProjectId == rndProjectId)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync();

        var projectDto = mapper.Map<RndProjectDto>(project);
        var formulationDtos = formulations
            .Select(f => new RndFormulationDto
            {
                Id = f.Id,
                CreatedAt = f.CreatedAt,
                RndProjectId = f.RndProjectId,
                Version = f.Version,
                Status = f.Status,
                Items = f
                    .Items.Select(item => new RndFormulationItemDto
                    {
                        Id = item.Id,
                        Material = mapper.Map<CollectionItemDto>(item.Material),
                        Order = item.Order,
                        BaseQuantity = item.BaseQuantity,
                        PrescribedQuantity = item.PrescribedQuantity,
                        Percentage = item.Percentage,
                    })
                    .ToList(),
            })
            .ToList();

        var html = PdfTemplate.RndDevelopmentReportTemplate(
            projectDto,
            formulationDtos,
            trialBatches,
            analyticalMethods,
            stabilityStudies,
            technologyTransfers
        );

        return pdfService.GeneratePdfFromHtml(html);
    }
}
