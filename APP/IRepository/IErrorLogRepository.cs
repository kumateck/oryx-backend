using APP.Utils;
using DOMAIN.Entities.ErrorLogs;
using SHARED;

namespace APP.IRepository;

public interface IErrorLogRepository
{
    Task<Paginateable<IEnumerable<ErrorLogListDto>>> GetErrorLogs(ErrorLogFilter filter);
    Task<Result<ErrorLogDto>> GetErrorLog(Guid id);
    Task<List<ErrorLogSummaryDto>> GetErrorSummary(ErrorLogFilter filter);
}
