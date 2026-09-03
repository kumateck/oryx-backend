using APP.Utils;
using DOMAIN.Entities.AttendanceRecords;
using DOMAIN.Entities.Payroll;
using SHARED;

namespace APP.IRepository;

public interface IPayrollRepository
{
    // Compensation
    Task<Result<Guid>> CreateOrUpdateCompensation(CreateEmployeeCompensationRequest request, Guid userId);
    Task<Result<EmployeeCompensationDto>> GetCurrentCompensation(Guid employeeId);
    Task<Result<Paginateable<IEnumerable<EmployeeCompensationDto>>>> GetCompensationHistory(Guid employeeId, int page, int pageSize);

    Task<Result<Guid>> CreatePayrollDeduction(CreatePayrollDeductionRequest request);
    Task<Result<Paginateable<IEnumerable<PayrollDeductionDto>>>> GetPayrollDeductions(Guid? employeeId, int page, int pageSize);
    Task<Result> DeletePayrollDeduction(Guid id, Guid userId);

    Task<Result<Guid>> CreatePayrollAddition(CreatePayrollAdditionRequest request);
    Task<Result<Paginateable<IEnumerable<PayrollAdditionDto>>>> GetPayrollAdditions(Guid? employeeId, int page, int pageSize);
    Task<Result> DeletePayrollAddition(Guid id, Guid userId);

    Task<Result<Guid>> CreateTaxRelief(CreateEmployeeTaxReliefRequest request);
    Task<Result<List<EmployeeTaxReliefDto>>> GetTaxReliefs(Guid employeeId);
    Task<Result> DeleteTaxRelief(Guid id, Guid userId);

    // Configuration
    Task<Result<Guid>> CreatePayGrade(CreatePayGradeRequest request);
    Task<Result<List<PayGradeDto>>> GetPayGrades();
    Task<Result> UpdatePayGrade(Guid id, CreatePayGradeRequest request);
    Task<Result> DeletePayGrade(Guid id, Guid userId);

    Task<Result<Guid>> CreatePayeTaxBand(CreatePayeTaxBandRequest request);
    Task<Result<List<PayeTaxBandDto>>> GetPayeTaxBands();
    Task<Result> DeletePayeTaxBand(Guid id, Guid userId);

    Task<Result<Guid>> CreateSsnitRate(CreateSsnitRateRequest request);
    Task<Result<List<SsnitRateDto>>> GetSsnitRates();
    Task<Result> DeleteSsnitRate(Guid id, Guid userId);

    // Runs
    Task<Result<Guid>> CreatePayrollRun(CreatePayrollRunRequest request, Guid userId);
    Task<Result<Paginateable<IEnumerable<PayrollRunDto>>>> GetPayrollRuns(int page, int pageSize, PayrollRunStatus? status);
    Task<Result<PayrollRunDto>> GetPayrollRun(Guid id);
    Task<Result> SubmitPayrollRunForApproval(Guid id, Guid userId);
    Task<Result> MarkPayrollRunProcessed(Guid id);
    Task<Result> CancelPayrollRun(Guid id, Guid userId);

    // Payslips
    Task<Result<Paginateable<IEnumerable<PayslipDto>>>> GetPayslipsForRun(Guid payrollRunId, int page, int pageSize);
    Task<Result<PayslipDto>> GetPayslip(Guid id);
    Task<Result<Paginateable<IEnumerable<PayslipDto>>>> GetEmployeePayslips(Guid employeeId, int page, int pageSize);

    // Reports / Exports
    Task<Result<FileExportResult>> ExportBankAdvice(Guid payrollRunId, FileFormat format);
    Task<Result<FileExportResult>> ExportPayrollRegister(Guid payrollRunId, FileFormat format);
}
