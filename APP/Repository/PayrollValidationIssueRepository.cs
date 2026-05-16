using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.PayrollValidationIssues;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class PayrollValidationIssueRepository(ApplicationDbContext context, IMapper mapper) : IPayrollValidationIssueRepository
{
    public async Task<Result<Paginateable<IEnumerable<PayrollValidationIssueDto>>>> GetValidationIssues(Guid payrollRunId, int page, int pageSize, string searchQuery,
        PayrollValidationStage? stage = null, PayrollValidationSeverity? severity = null,
        PayrollValidationIssueStatus? status = null)
    {
        var query = context.PayrollValidationIssues.AsQueryable();
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery,p => p.Code);
        }
        if (stage.HasValue) query = query.Where(p => p.Stage == stage.Value);
        if (severity.HasValue) query = query.Where(p => p.Severity == severity.Value);
        if (status.HasValue) query = query.Where(p => p.Status == status.Value);
        
        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, 
            mapper.Map<PayrollValidationIssueDto>);
    }

    public async Task<Result<PayrollValidationIssueDto>> GetValidationIssue(Guid id)
    {
        var issue = await context.PayrollValidationIssues.FirstOrDefaultAsync(p => p.Id == id);
        return issue is null ? Error.NotFound("ValidationIssue.NotFound", "Payroll validation issue not found") 
            : mapper.Map<PayrollValidationIssueDto>(issue);
    }

    public async Task<Result> ResolveValidationIssue(Guid id, ResolveValidationIssueRequest request, Guid userId)
    {
        var issue = await context.PayrollValidationIssues.FirstOrDefaultAsync(p => p.Id == id);
        if (issue is null) return Error.NotFound("ValidationIssue.NotFound", "Payroll validation issue not found");

        if (issue.Status == PayrollValidationIssueStatus.Resolved)
            return Error.Validation("ValidationIssue.AlreadyResolved", "Validation issue is already resolved");

        issue.Status = PayrollValidationIssueStatus.Resolved;
        issue.ResolutionNotes = request.ResolutionNotes;
        issue.ResolvedById = userId;
        issue.ResolvedAt = DateTime.UtcNow;
        
        await context.SaveChangesAsync();
        return Result.Success();
    }
}