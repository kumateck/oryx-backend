using SHARED;

namespace APP.IRepository;

public interface IRndDevelopmentReportRepository
{
    Task<Result<byte[]>> GenerateDevelopmentReportPdf(Guid rndProjectId);
}
