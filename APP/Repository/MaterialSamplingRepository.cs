using APP.IRepository;
using AutoMapper;
using DOMAIN.Entities.Checklists;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.MaterialSampling;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class MaterialSamplingRepository(ApplicationDbContext context, IMapper mapper) : IMaterialSamplingRepository
{
    public async Task<Result<Guid>> CreateMaterialSampling(CreateMaterialSamplingRequest materialSamplingRequest)
    {
        var grn = await context.Grns.IgnoreQueryFilters().FirstOrDefaultAsync(gr => gr.Id == materialSamplingRequest.GrnId);

        if (grn == null)
        {
            return Error.Validation("GRN.Invalid", "Invalid GRN");
        }

        var batch = await context.MaterialBatches.FirstOrDefaultAsync(b => b.Id == materialSamplingRequest.MaterialBatchId);
        if (batch is null) return Error.NotFound("MaterialBatchId.NotFound", "MaterialBatch not found");

        var request = mapper.Map<MaterialSampling>(materialSamplingRequest);

        await context.MaterialSamplings.AddAsync(request);
        batch.Status = BatchStatus.Sampled;
        batch.SampledQuantity += materialSamplingRequest.SampleQuantity;
        context.MaterialBatches.Update(batch);
        await context.SaveChangesAsync();

        return request.Id;
    }

    public async Task<Result> AddIssueNumberToMaterialSample(Guid materialSampleId, string issueNumber, Guid userId)
    {
        var materialSample = await context.MaterialSamplings
            .FirstOrDefaultAsync(m => m.Id == materialSampleId);

        if (materialSample == null)
            return Error.NotFound("MaterialSamplingId.NotFound", "MaterialSampling not found");

        materialSample.IssueNumber = issueNumber;
        materialSample.IssuedById = userId;
        materialSample.IssuedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<MaterialSamplingDto>> GetMaterialSamplingByGrnAndBatch(Guid grnId, Guid batchId)
    {
        var materialSampling = await context.MaterialSamplings
            .AsSplitQuery()
            .IgnoreQueryFilters()
            .Include(m => m.IssuedBy)
            .Include(m => m.Grn)
            .Include(m => m.MaterialBatch)
            .FirstOrDefaultAsync(ps => ps.GrnId == grnId && ps.MaterialBatchId == batchId);

        return materialSampling == null ?
            Error.Validation("MaterialSampling.NotFound", "Material Sampling not found")
            : Result.Success(mapper.Map<MaterialSamplingDto>(materialSampling));
    }

    // ---------------------------------------------------------------------
    // ✅ PRE-SAMPLE CHECKLIST METHODS
    // ---------------------------------------------------------------------

    public async Task<Result<Guid>> CreatePreSampleChecklist(CreatePreSampleChecklistRequest request)
    {
        var grn = await context.Grns.IgnoreQueryFilters().FirstOrDefaultAsync(g => g.Id == request.GrnId);
        if (grn is null)
            return Error.Validation("GRN.Invalid", "Invalid GRN");

        var batch = await context.MaterialBatches.FirstOrDefaultAsync(b => b.Id == request.MaterialBatchId);
        if (batch is null)
            return Error.NotFound("MaterialBatch.NotFound", "Material batch not found");

        var checklist = mapper.Map<PreSampleChecklist>(request);

        await context.PreSampleChecklists.AddAsync(checklist);
        await context.SaveChangesAsync();

        return checklist.Id;
    }

    public async Task<Result<PreSampleChecklistDto>> GetPreSampleChecklistByGrnAndBatch(Guid grnId, Guid batchId)
    {
        var checklist = await context.PreSampleChecklists
            .AsSplitQuery()
            .Include(x => x.Grn)
            .Include(x => x.MaterialBatch)
            .FirstOrDefaultAsync(x => x.GrnId == grnId && x.MaterialBatchId == batchId);

        if (checklist is null)
            return Error.NotFound("PreSampleChecklist.NotFound", "Pre-sample checklist not found");

        var dto = mapper.Map<PreSampleChecklistDto>(checklist);
        return Result.Success(dto);
    }
}
