using APP.Utils;
using DOMAIN.Entities.PayrollRetroAdjustments;
using SHARED;

namespace APP.IRepository;

public interface IPayrollRetroAdjustmentRepository
{
    Task<Result<Guid>> CreatePayrollRetroAdjustment(CreatePayrollRetroAdjustmentRequest request);
    Task<Result<Paginateable<IEnumerable<PayrollRetroAdjustmentDto>>>> GetRetroAdjustments(int page, int pageSize, Guid? payrollCompanyId = null, Guid? employeeId = null, PayrollRetroAdjustmentStatus? status = null);
    Task<Result<PayrollRetroAdjustmentDto>> GetRetroAdjustment(Guid id);
    Task<Result> MaterializeRetroAdjustment(Guid id, MaterializeRetroAdjustmentRequest request, Guid userId);
    Task<Result> DeleteRetroAdjustment(Guid id, Guid userId);
}