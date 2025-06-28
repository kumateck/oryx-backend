using APP.IRepository;
using AutoMapper;
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
        var grn = await context.Grns.FirstOrDefaultAsync(gr => gr.Id == materialSamplingRequest.GrnId);

        if (grn == null)
        {
            return Error.Validation("GRN.Invalid", "Invalid GRN");
        }
        
        var batch = await context.MaterialBatches.FirstOrDefaultAsync(b => b.Id == materialSamplingRequest.MaterialBatchId);
        if(batch is null) return Error.NotFound("MaterialBatchId.NotFound", "MaterialBatch not found");
        
        var request = mapper.Map<MaterialSampling>(materialSamplingRequest);
        
        await context.MaterialSamplings.AddAsync(request);
        batch.Status = BatchStatus.Testing;
        context.MaterialBatches.Update(batch);
        await context.SaveChangesAsync();
        
        return request.Id;
    }

    public async Task<Result<MaterialSamplingDto>> GetMaterialSamplingByGrnAndBatch(Guid grnId, Guid batchId)
    {
        var materialSampling =  await context.MaterialSamplings
            .AsSplitQuery()
            .Include(m => m.Grn)
            .Include(m => m.MaterialBatch)
            .FirstOrDefaultAsync(ps => ps.GrnId == grnId && ps.MaterialBatchId == batchId);

        return materialSampling == null ? 
            Error.Validation("MaterialSampling.NotFound", "Material Sampling not found") 
            : Result.Success(mapper.Map<MaterialSamplingDto>(materialSampling));
    }
}