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
        var grn = context.Grns.FirstOrDefault(gr =>
            gr.Id == materialSamplingRequest.GrnId);

        if (grn == null)
        {
            return Error.Validation("GRN.Invalid", "Invalid GRN");
        }
        var materialSample =  context.MaterialSamplings
            .FirstOrDefault(ps => ps.GrnId == materialSamplingRequest.GrnId);

        if (materialSample != null)
        {
            return Error.Validation("MaterialSampling", "Material Sampling already exists");
        }
        
        var request = mapper.Map<MaterialSampling>(grn);
        
        await context.MaterialSamplings.AddAsync(request);
        await context.SaveChangesAsync();
        
        return request.Id;
    }

    public async Task<Result<MaterialSamplingDto>> GetMaterialSamplingByMaterialId(Guid id)
    {
        var materialSampling =  await context.MaterialSamplings.FirstOrDefaultAsync(ps => ps.Id == id);

        return materialSampling == null ? 
            Error.Validation("MaterialSampling.NotFound", "Material Sampling not found") 
            : Result.Success(mapper.Map<MaterialSamplingDto>(materialSampling));
    }
}