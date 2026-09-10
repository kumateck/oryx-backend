using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.BillOfMaterials.Request;
using DOMAIN.Entities.RndFormulations;
using DOMAIN.Entities.RndTechnologyTransfers;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class RndTechnologyTransferRepository(
    ApplicationDbContext context,
    IMapper mapper,
    IBoMRepository boMRepository
) : IRndTechnologyTransferRepository
{
    public async Task<Result<Guid>> CreateTransfer(
        Guid rndProjectId,
        CreateRndTechnologyTransferRequest request,
        Guid userId
    )
    {
        var project = await context.RndProjects.FirstOrDefaultAsync(p => p.Id == rndProjectId);
        if (project is null)
            return Error.NotFound("RndTechnologyTransfer.ProjectNotFound", "R&D project not found.");

        var approvalGate = project.EnsureApprovedForProgression("R&D project");
        if (approvalGate.IsFailure)
            return approvalGate.Error;

        var formulation = await context.RndFormulations.FirstOrDefaultAsync(f =>
            f.Id == request.RndFormulationId
        );
        if (formulation is null)
            return Error.NotFound("RndTechnologyTransfer.FormulationNotFound", "Formulation not found.");

        if (formulation.RndProjectId != rndProjectId)
            return Error.Validation(
                "RndTechnologyTransfer.FormulationMismatch",
                "This formulation does not belong to the specified R&D project."
            );

        if (formulation.Status != RndFormulationStatus.Approved)
            return Error.Validation(
                "RndTechnologyTransfer.FormulationNotApproved",
                "Only an Approved formulation can be transferred to production."
            );

        if (
            request.GapAnalysisFormId.HasValue
            && !await context.Forms.AnyAsync(f => f.Id == request.GapAnalysisFormId.Value)
        )
            return Error.NotFound("RndTechnologyTransfer.FormNotFound", "Gap analysis form not found.");

        var transfer = new RndTechnologyTransfer
        {
            RndProjectId = rndProjectId,
            RndFormulationId = request.RndFormulationId,
            GapAnalysisFormId = request.GapAnalysisFormId,
            Status = RndTechnologyTransferStatus.DueDiligence,
            CreatedById = userId,
        };

        await context.RndTechnologyTransfers.AddAsync(transfer);
        await context.SaveChangesAsync();
        return transfer.Id;
    }

    public async Task<Result> UpdateStatus(Guid id, UpdateRndTechnologyTransferStatusRequest request, Guid userId)
    {
        var transfer = await context.RndTechnologyTransfers.FirstOrDefaultAsync(t => t.Id == id);
        if (transfer is null)
            return Error.NotFound("RndTechnologyTransfer.NotFound", "Technology transfer not found.");

        var validTransition = (transfer.Status, request.Status) switch
        {
            (RndTechnologyTransferStatus.DueDiligence, RndTechnologyTransferStatus.GapAnalysis) => true,
            (RndTechnologyTransferStatus.GapAnalysis, RndTechnologyTransferStatus.ProtocolApproved) => true,
            (RndTechnologyTransferStatus.GapAnalysis, RndTechnologyTransferStatus.DueDiligence) => true,
            _ => false,
        };

        if (!validTransition)
            return Error.Validation(
                "RndTechnologyTransfer.InvalidStatus",
                $"Cannot move a technology transfer from {transfer.Status} to {request.Status}. "
                    + "Completed is only reached via PromoteToProduction."
            );

        transfer.Status = request.Status;
        transfer.LastUpdatedById = userId;

        context.RndTechnologyTransfers.Update(transfer);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<Guid>> PromoteToProduction(Guid id, Guid userId)
    {
        var transfer = await context
            .RndTechnologyTransfers.Include(t => t.RndProject)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (transfer is null)
            return Error.NotFound("RndTechnologyTransfer.NotFound", "Technology transfer not found.");

        if (transfer.Status != RndTechnologyTransferStatus.ProtocolApproved)
            return Error.Validation(
                "RndTechnologyTransfer.InvalidStatus",
                "The transfer protocol must be Approved before promoting to production."
            );

        if (!transfer.RndProject.ProductId.HasValue)
            return Error.Validation(
                "RndTechnologyTransfer.ProductRequired",
                "The R&D project must be linked to a Product before promoting to production."
            );

        var formulation = await context
            .RndFormulations.AsSplitQuery()
            .Include(f => f.Items)
                .ThenInclude(i => i.Substitutes)
            .FirstOrDefaultAsync(f => f.Id == transfer.RndFormulationId);
        if (formulation is null)
            return Error.NotFound("RndTechnologyTransfer.FormulationNotFound", "Formulation not found.");

        var productId = transfer.RndProject.ProductId.Value;

        var bomRequest = new CreateBillOfMaterialRequest
        {
            ProductId = productId,
            Items = formulation
                .Items.Select(item => new CreateBoMItemsRequest
                {
                    MaterialId = item.MaterialId,
                    IsSubstitutable = item.IsSubstitutable,
                    Grade = item.Grade,
                    CasNumber = item.CasNumber,
                    BaseQuantity = item.BaseQuantity,
                    BaseUoMId = item.BaseUoMId,
                    Order = item.Order,
                    PrescribedQuantity = item.PrescribedQuantity,
                    SubstituteMaterialIds = item.Substitutes.Select(s => s.SubstituteMaterialId).ToList(),
                })
                .ToList(),
        };

        var bomResult = await boMRepository.CreateBillOfMaterial(bomRequest, userId);
        if (bomResult.IsFailure)
            return Result.Failure<Guid>(bomResult.Errors);

        var product = await context.Products.FirstOrDefaultAsync(p => p.Id == productId);
        if (product is not null)
        {
            product.MasterFormulaNumber = $"MF-{transfer.RndProject.Code}-V{formulation.Version}";
            product.RevisionNumber = formulation.Version;
            product.LastUpdatedById = userId;
            context.Products.Update(product);
        }

        transfer.Status = RndTechnologyTransferStatus.Completed;
        transfer.BillOfMaterialId = bomResult.Value;
        transfer.CompletedAt = DateTime.UtcNow;
        transfer.CompletedById = userId;
        context.RndTechnologyTransfers.Update(transfer);

        await context.SaveChangesAsync();
        return bomResult.Value;
    }

    private IQueryable<RndTechnologyTransfer> TransferDetailQuery() =>
        context.RndTechnologyTransfers.Include(t => t.CompletedBy);

    public async Task<Result<RndTechnologyTransferDto>> GetTransfer(Guid id)
    {
        var transfer = await TransferDetailQuery().FirstOrDefaultAsync(t => t.Id == id);
        return transfer is null
            ? Error.NotFound("RndTechnologyTransfer.NotFound", "Technology transfer not found.")
            : mapper.Map<RndTechnologyTransferDto>(transfer);
    }

    public async Task<Result<Paginateable<IEnumerable<RndTechnologyTransferDto>>>> GetTransfersForProject(
        Guid rndProjectId,
        int page,
        int pageSize
    )
    {
        var query = TransferDetailQuery()
            .Where(t => t.RndProjectId == rndProjectId)
            .OrderByDescending(t => t.CreatedAt)
            .AsQueryable();

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<RndTechnologyTransferDto>
        );
    }
}
