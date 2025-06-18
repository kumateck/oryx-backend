using APP.IRepository;
using AutoMapper;
using DOMAIN.Entities.MaterialSampling;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class MaterialSamplingRepository(ApplicationDbContext context, IMapper mapper) : IMaterialSamplingRepository
{
    public async Task<Result<Guid>> CreateMaterialSampling(CreateMaterialSamplingRequest materialSamplingRequest)
    {
        var grn = await context.Grns.FirstOrDefaultAsync(gr => gr.Id == materialSamplingRequest.GrnId 
                                                    && gr.LastDeletedById == null);

        if (grn == null)
        {
            return Error.Validation("GRN.Invalid", "Invalid GRN");
        }
        
        var request = mapper.Map<MaterialSampling>(materialSamplingRequest);
        
        await context.MaterialSamplings.AddAsync(request);
        await context.SaveChangesAsync();
        
        return request.Id;
    }

    public async Task<Result<MaterialSamplingDto>> GetMaterialSamplingByMaterialId(Guid id)
    {
        var materialSampling =  await context.MaterialSamplings
            .Include(m => m.Grn)
            .FirstOrDefaultAsync(ps => ps.Id == id);

        return materialSampling == null ? 
            Error.Validation("MaterialSampling.NotFound", "Material Sampling not found") 
            : Result.Success(mapper.Map<MaterialSamplingDto>(materialSampling));
    }
}