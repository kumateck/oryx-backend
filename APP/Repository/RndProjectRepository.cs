using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.RndProjects;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class RndProjectRepository(
    ApplicationDbContext context,
    IMapper mapper,
    IApprovalRepository approvalRepository
) : IRndProjectRepository
{
    private const string DepartmentName = "Research & Development";

    private static Result<RndProject> ValidateStatus(RndProject project, params RndProjectStatus[] allowed)
    {
        if (!allowed.Contains(project.Status))
            return Error.Validation(
                "RndProject.InvalidStatus",
                $"This action is not allowed while the project is {project.Status}."
            );

        return project;
    }

    private async Task<Result> ValidateReferences(Guid? productId, Guid? qtppFormId, Guid? attachmentId)
    {
        if (productId.HasValue && !await context.Products.AnyAsync(p => p.Id == productId.Value))
            return Error.NotFound("RndProject.ProductNotFound", "Product not found.");

        if (qtppFormId.HasValue && !await context.Forms.AnyAsync(f => f.Id == qtppFormId.Value))
            return Error.NotFound("RndProject.FormNotFound", "QTPP form not found.");

        if (attachmentId.HasValue && !await context.Attachments.AnyAsync(a => a.Id == attachmentId.Value))
            return Error.NotFound("Attachment.NotFound", "Attachment not found.");

        return Result.Success();
    }

    public async Task<Result<Guid>> CreateProject(CreateRndProjectRequest request, Guid userId)
    {
        var validation = await ValidateReferences(request.ProductId, request.QtppFormId, request.AttachmentId);
        if (validation.IsFailure)
            return Result.Failure<Guid>(validation.Errors);

        var department = await context.Departments.FirstOrDefaultAsync(d => d.Name == DepartmentName);
        if (department is null)
            return Error.NotFound(
                "RndProject.DepartmentNotFound",
                "The Research & Development department has not been seeded."
            );

        var year = DateTime.UtcNow.Year;
        var sequence = await context.RndProjects.CountAsync(p => p.CreatedAt.Year == year) + 1;

        var project = mapper.Map<RndProject>(request);
        project.Code = $"RND-{year}-{sequence:D4}";
        project.DepartmentId = department.Id;
        project.RequestedById = userId;
        project.Status = RndProjectStatus.Intake;
        project.Approved = false;
        project.CreatedById = userId;

        await context.RndProjects.AddAsync(project);
        await context.SaveChangesAsync();

        await approvalRepository.CreateInitialApprovalsAsync(nameof(RndProject), project.Id);

        return project.Id;
    }

    public async Task<Result> UpdateProject(Guid id, UpdateRndProjectRequest request, Guid userId)
    {
        var project = await context.RndProjects.FirstOrDefaultAsync(p => p.Id == id);
        if (project is null)
            return Error.NotFound("RndProject.NotFound", "R&D project not found.");

        var statusCheck = ValidateStatus(project, RndProjectStatus.Intake);
        if (statusCheck.IsFailure)
            return statusCheck.Error;

        var validation = await ValidateReferences(request.ProductId, request.QtppFormId, request.AttachmentId);
        if (validation.IsFailure)
            return validation;

        project.ProductId = request.ProductId;
        project.Title = request.Title;
        project.Objective = request.Objective;
        project.TargetLaunchDate = request.TargetLaunchDate;
        project.QtppFormId = request.QtppFormId;
        project.AttachmentId = request.AttachmentId;
        project.LastUpdatedById = userId;

        context.RndProjects.Update(project);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> UpdateStatus(Guid id, UpdateRndProjectStatusRequest request, Guid userId)
    {
        var project = await context.RndProjects.FirstOrDefaultAsync(p => p.Id == id);
        if (project is null)
            return Error.NotFound("RndProject.NotFound", "R&D project not found.");

        var approvalGate = project.EnsureApprovedForProgression("R&D project");
        if (approvalGate.IsFailure)
            return approvalGate.Error;

        if (project.Status is RndProjectStatus.Completed or RndProjectStatus.Cancelled)
            return Error.Validation(
                "RndProject.InvalidStatus",
                $"This project is already {project.Status} and cannot change status further."
            );

        project.Status = request.Status;
        project.LastUpdatedById = userId;

        context.RndProjects.Update(project);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteProject(Guid id, Guid userId)
    {
        var project = await context.RndProjects.FirstOrDefaultAsync(p => p.Id == id);
        if (project is null)
            return Error.NotFound("RndProject.NotFound", "R&D project not found.");

        var statusCheck = ValidateStatus(project, RndProjectStatus.Intake);
        if (statusCheck.IsFailure)
            return statusCheck.Error;

        project.DeletedAt = DateTime.UtcNow;
        project.LastDeletedById = userId;

        context.RndProjects.Update(project);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    private IQueryable<RndProject> ProjectDetailQuery() =>
        context
            .RndProjects.AsSplitQuery()
            .Where(p => !p.DeletedAt.HasValue)
            .Include(p => p.Product)
            .Include(p => p.Department)
            .Include(p => p.RequestedBy);

    public async Task<Result<RndProjectDto>> GetProject(Guid id)
    {
        var project = await ProjectDetailQuery().FirstOrDefaultAsync(p => p.Id == id);
        return project is null
            ? Error.NotFound("RndProject.NotFound", "R&D project not found.")
            : mapper.Map<RndProjectDto>(project);
    }

    public async Task<Result<Paginateable<IEnumerable<RndProjectDto>>>> GetProjects(
        int page,
        int pageSize,
        string searchQuery,
        RndProjectStatus? status
    )
    {
        var query = ProjectDetailQuery().AsQueryable();

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(searchQuery))
            query = query.WhereSearch(searchQuery, p => p.Code, p => p.Title);

        query = query.OrderByDescending(p => p.CreatedAt);

        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, mapper.Map<RndProjectDto>);
    }
}
