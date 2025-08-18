using APP.IRepository;
using AutoMapper;
using DOMAIN.Entities.JobRequests;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class JobRequestRepository(ApplicationDbContext context, IMapper mapper, UserManager<User> userManager) : IJobRequestRepository
{
    public async Task<Result<Guid>> CreateJobRequest(CreateJobRequest request)
    {
        var department = await context.Departments.AnyAsync(d => d.Id == request.DepartmentId);
        if (!department) return Error.Validation("Department.Invalid", "Invalid department");

        var issuer = await userManager.FindByIdAsync(request.IssuedById.ToString());
        if (issuer is null) return Error.Validation("User.Invalid", "User Invalid");
        
        var equipment = await context.Equipments.AnyAsync(e => e.Id == request.EquipmentId);
        if (!equipment) return Error.Validation("Equipment.Invalid", "Invalid equipment");
        
        var jobRequest = mapper.Map<JobRequest>(request);
        await context.JobRequests.AddAsync(jobRequest);
        await context.SaveChangesAsync();
        return jobRequest.Id;
    }

    public async Task<Result<List<JobRequestDto>>> GetJobRequests()
    {
        var jobRequests = await context.JobRequests.ToListAsync();
        return mapper.Map<List<JobRequestDto>>(jobRequests);
    }
}