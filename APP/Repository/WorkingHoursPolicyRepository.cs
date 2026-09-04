using APP.IRepository;
using AutoMapper;
using DOMAIN.Entities.ShiftAssignments;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class WorkingHoursPolicyRepository(ApplicationDbContext context, IMapper mapper)
    : IWorkingHoursPolicyRepository
{
    public async Task<Result<Guid>> CreatePolicy(CreateWorkingHoursPolicyRequest request)
    {
        if (request.MaxHoursPerDay <= 0 || request.MaxHoursPerWeek <= 0)
        {
            return Error.Validation(
                "WorkingHoursPolicy.Invalid",
                "Max hours per day and per week must be positive.");
        }

        var policy = mapper.Map<WorkingHoursPolicy>(request);

        await context.WorkingHoursPolicies.AddAsync(policy);
        await context.SaveChangesAsync();

        return policy.Id;
    }

    public async Task<Result<List<WorkingHoursPolicyDto>>> GetPolicies()
    {
        var policies = await context.WorkingHoursPolicies
            .OrderByDescending(p => p.EffectiveFrom)
            .ToListAsync();

        return mapper.Map<List<WorkingHoursPolicyDto>>(policies);
    }

    public async Task<Result> DeletePolicy(Guid id, Guid userId)
    {
        var policy = await context.WorkingHoursPolicies.FirstOrDefaultAsync(p => p.Id == id);
        if (policy is null)
        {
            return Error.NotFound("WorkingHoursPolicy.NotFound", "Working hours policy not found");
        }

        policy.LastDeletedById = userId;
        policy.DeletedAt = DateTime.UtcNow;

        context.WorkingHoursPolicies.Update(policy);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}
