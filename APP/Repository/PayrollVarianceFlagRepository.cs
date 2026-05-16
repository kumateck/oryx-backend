using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.PayrollVarianceFlags;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class PayrollVarianceFlagRepository(ApplicationDbContext context, IMapper mapper) : IPayrollVarianceFlagRepository
    {
        public async Task<Result<Paginateable<IEnumerable<PayrollVarianceFlagDto>>>> GetVarianceFlags(Guid payrollRunId,
            int page, int pageSize, string searchQuery, PayrollVarianceSeverity? severity = null,
            PayrollVarianceDisposition? disposition = null)
        {
            var query = context.PayrollVarianceFlags
                .Where(v => v.PayrollRunId == payrollRunId);

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                query = query.WhereSearch(searchQuery, q => q.PayrollRun.Code);
            }

            if (severity.HasValue)
                query = query.Where(v => v.Severity == severity.Value);

            if (disposition.HasValue)
                query = query.Where(v => v.Disposition == disposition.Value);

            return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, mapper.Map<PayrollVarianceFlagDto>);
        }

        public async Task<Result<PayrollVarianceFlagDto>> GetVarianceFlag(Guid id)
        {
            var flag = await context.PayrollVarianceFlags.FirstOrDefaultAsync(v => v.Id == id);
            return flag is null
                ? Error.NotFound("PayrollVarianceFlag.NotFound", "Variance flag not found")
                : Result.Success(mapper.Map<PayrollVarianceFlagDto>(flag));
        }

        public async Task<Result> ReviewVarianceFlag(Guid id, ReviewVarianceFlagRequest request, Guid userId)
        {
            var flag = await context.PayrollVarianceFlags.FirstOrDefaultAsync(v => v.Id == id);
            if (flag is null)
                return Error.NotFound("PayrollVarianceFlag.NotFound", "Variance flag not found");

            flag.Disposition = request.Disposition;
            flag.ReviewerComment = request.ReviewerComment;
            flag.ReviewerId = userId;
            flag.ReviewedAt = DateTime.UtcNow;
            
            await context.SaveChangesAsync();

            return Result.Success();
        }
}