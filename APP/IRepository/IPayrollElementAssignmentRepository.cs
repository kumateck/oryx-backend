using APP.Utils;
using DOMAIN.Entities.PayrollElementAssignments;
using SHARED;

namespace APP.IRepository;

public interface IPayrollElementAssignmentRepository
{
    Task<Result<Guid>> CreatePayrollElementAssignment(CreatePayrollElementAssignment request);
    Task<Result<Paginateable<IEnumerable<PayrollElementAssignmentDto>>>> GetPayrollElementAssignments(int page, int pageSize, Guid? employeeId = null,
        Guid? payrollElementId = null, PayrollElementAssignmentStatus? status = null);
    Task<Result<PayrollElementAssignmentDto>> GetPayrollElementAssignment(Guid id);
    Task<Result> UpdatePayrollElementAssignment(Guid id, CreatePayrollElementAssignment request);
    Task<Result> DeletePayrollElementAssignment(Guid id, Guid userId);
}