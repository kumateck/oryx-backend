using DOMAIN.Entities.Payroll;

namespace APP.Services;

public interface IPayrollCalculationService
{
    Task<Payslip> CalculatePayslip(Guid employeeId, DateTime periodStart, DateTime periodEnd);
}
