using System.Text;
using APP.Utils;
using DOMAIN.Entities.AttendanceRecords;
using DOMAIN.Entities.Payroll;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using SHARED;

namespace APP.Repository;

public partial class PayrollRepository
{
    public async Task<Result<Paginateable<IEnumerable<PayslipDto>>>> GetPayslipsForRun(
        Guid payrollRunId, int page, int pageSize)
    {
        var query = context.Payslips
            .Include(p => p.PayrollRun)
            .Include(p => p.Employee).ThenInclude(e => e.Department)
            .Include(p => p.Employee).ThenInclude(e => e.Designation)
            .Include(p => p.LineItems)
            .Where(p => p.PayrollRunId == payrollRunId)
            .OrderBy(p => p.Employee.FirstName)
            .AsQueryable();

        return await PaginationHelper.GetPaginatedResultAsync(
            query, page, pageSize, mapper.Map<PayslipDto>);
    }

    public async Task<Result<PayslipDto>> GetPayslip(Guid id)
    {
        var payslip = await context.Payslips
            .Include(p => p.PayrollRun)
            .Include(p => p.Employee).ThenInclude(e => e.Department)
            .Include(p => p.Employee).ThenInclude(e => e.Designation)
            .Include(p => p.LineItems)
            .FirstOrDefaultAsync(p => p.Id == id);

        return payslip is null
            ? Error.NotFound("Payslip.NotFound", "Payslip not found")
            : mapper.Map<PayslipDto>(payslip);
    }

    public async Task<Result<Paginateable<IEnumerable<PayslipDto>>>> GetEmployeePayslips(
        Guid employeeId, int page, int pageSize)
    {
        var query = context.Payslips
            .Include(p => p.PayrollRun)
            .Include(p => p.Employee).ThenInclude(e => e.Department)
            .Include(p => p.Employee).ThenInclude(e => e.Designation)
            .Include(p => p.LineItems)
            .Where(p => p.EmployeeId == employeeId && p.PayrollRun.Status == PayrollRunStatus.Processed)
            .OrderByDescending(p => p.PayrollRun.PeriodStart)
            .AsQueryable();

        return await PaginationHelper.GetPaginatedResultAsync(
            query, page, pageSize, mapper.Map<PayslipDto>);
    }

    /// <summary>
    /// A bulk bank-transfer advice (staff number, bank, branch, account, net pay) for a
    /// processed payroll run - the standard hand-off a bank needs to execute salary payments.
    /// </summary>
    public async Task<Result<FileExportResult>> ExportBankAdvice(Guid payrollRunId, FileFormat format)
    {
        var payrollRun = await context.PayrollRuns.FirstOrDefaultAsync(r => r.Id == payrollRunId);
        if (payrollRun is null)
        {
            return Error.NotFound("PayrollRun.NotFound", "Payroll run not found");
        }

        var payslips = await context.Payslips
            .Include(p => p.Employee)
            .Where(p => p.PayrollRunId == payrollRunId)
            .OrderBy(p => p.Employee.FirstName)
            .ToListAsync();

        var employeeIds = payslips.Select(p => p.EmployeeId).ToList();
        var compensations = await context.EmployeeCompensations
            .Where(c => employeeIds.Contains(c.EmployeeId) && c.EffectiveTo == null)
            .ToListAsync();

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

        if (format.FileType == "csv")
        {
            var sb = new StringBuilder();
            sb.AppendLine("Staff Number,Employee Name,Bank Name,Bank Branch,Account Number,Net Pay");

            foreach (var payslip in payslips)
            {
                var compensation = compensations.FirstOrDefault(c => c.EmployeeId == payslip.EmployeeId);
                sb.AppendLine(
                    $"{payslip.Employee.StaffNumber},{payslip.Employee.FirstName} {payslip.Employee.LastName}," +
                    $"{compensation?.BankName},{compensation?.BankBranch},{compensation?.BankAccountNumber}," +
                    $"{payslip.NetPay}");
            }

            return new FileExportResult
            {
                FileBytes = Encoding.UTF8.GetBytes(sb.ToString()),
                ContentType = "text/csv",
                FileName = $"BankAdvice_{timestamp}.csv"
            };
        }

        ExcelPackage.License.SetNonCommercialPersonal("Oryx");
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Bank Advice");
        string[] headers = ["Staff Number", "Employee Name", "Bank Name", "Bank Branch", "Account Number", "Net Pay"];
        for (var i = 0; i < headers.Length; i++)
        {
            sheet.Cells[1, i + 1].Value = headers[i];
        }

        var row = 2;
        foreach (var payslip in payslips)
        {
            var compensation = compensations.FirstOrDefault(c => c.EmployeeId == payslip.EmployeeId);
            sheet.Cells[row, 1].Value = payslip.Employee.StaffNumber;
            sheet.Cells[row, 2].Value = $"{payslip.Employee.FirstName} {payslip.Employee.LastName}";
            sheet.Cells[row, 3].Value = compensation?.BankName;
            sheet.Cells[row, 4].Value = compensation?.BankBranch;
            sheet.Cells[row, 5].Value = compensation?.BankAccountNumber;
            sheet.Cells[row, 6].Value = payslip.NetPay;
            row++;
        }

        return new FileExportResult
        {
            FileBytes = package.GetAsByteArray(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            FileName = $"BankAdvice_{timestamp}.xlsx"
        };
    }

    /// <summary>
    /// The full payroll register for a run - one row per employee with the statutory
    /// breakdown, for audit and GRA/SSNIT filing support.
    /// </summary>
    public async Task<Result<FileExportResult>> ExportPayrollRegister(Guid payrollRunId, FileFormat format)
    {
        var payrollRun = await context.PayrollRuns.FirstOrDefaultAsync(r => r.Id == payrollRunId);
        if (payrollRun is null)
        {
            return Error.NotFound("PayrollRun.NotFound", "Payroll run not found");
        }

        var payslips = await context.Payslips
            .Include(p => p.Employee).ThenInclude(e => e.Department)
            .Where(p => p.PayrollRunId == payrollRunId)
            .OrderBy(p => p.Employee.FirstName)
            .ToListAsync();

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        string[] headers =
        [
            "Staff Number", "Employee Name", "Department", "Basic Salary", "Allowances", "Additions",
            "Gross Pay", "SSNIT (Employee)", "SSNIT (Employer)", "Tier 2", "PAYE Tax", "Other Deductions", "Net Pay"
        ];

        if (format.FileType == "csv")
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(',', headers));

            foreach (var p in payslips)
            {
                sb.AppendLine($"{p.Employee.StaffNumber},{p.Employee.FirstName} {p.Employee.LastName}," +
                              $"{p.Employee.Department?.Name},{p.BasicSalary},{p.TotalAllowances},{p.TotalAdditions}," +
                              $"{p.GrossPay},{p.SsnitEmployeeContribution},{p.SsnitEmployerContribution}," +
                              $"{p.Tier2Contribution},{p.PayeTax},{p.TotalDeductions - p.SsnitEmployeeContribution - p.PayeTax},{p.NetPay}");
            }

            sb.AppendLine();
            sb.AppendLine("TOTALS,,," +
                          $"{payslips.Sum(p => p.BasicSalary)},{payslips.Sum(p => p.TotalAllowances)},{payslips.Sum(p => p.TotalAdditions)}," +
                          $"{payslips.Sum(p => p.GrossPay)},{payslips.Sum(p => p.SsnitEmployeeContribution)},{payslips.Sum(p => p.SsnitEmployerContribution)}," +
                          $"{payslips.Sum(p => p.Tier2Contribution)},{payslips.Sum(p => p.PayeTax)}," +
                          $"{payslips.Sum(p => p.TotalDeductions - p.SsnitEmployeeContribution - p.PayeTax)},{payslips.Sum(p => p.NetPay)}");

            return new FileExportResult
            {
                FileBytes = Encoding.UTF8.GetBytes(sb.ToString()),
                ContentType = "text/csv",
                FileName = $"PayrollRegister_{timestamp}.csv"
            };
        }

        ExcelPackage.License.SetNonCommercialPersonal("Oryx");
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Payroll Register");
        for (var i = 0; i < headers.Length; i++)
        {
            sheet.Cells[1, i + 1].Value = headers[i];
        }

        var row = 2;
        foreach (var p in payslips)
        {
            sheet.Cells[row, 1].Value = p.Employee.StaffNumber;
            sheet.Cells[row, 2].Value = $"{p.Employee.FirstName} {p.Employee.LastName}";
            sheet.Cells[row, 3].Value = p.Employee.Department?.Name;
            sheet.Cells[row, 4].Value = p.BasicSalary;
            sheet.Cells[row, 5].Value = p.TotalAllowances;
            sheet.Cells[row, 6].Value = p.TotalAdditions;
            sheet.Cells[row, 7].Value = p.GrossPay;
            sheet.Cells[row, 8].Value = p.SsnitEmployeeContribution;
            sheet.Cells[row, 9].Value = p.SsnitEmployerContribution;
            sheet.Cells[row, 10].Value = p.Tier2Contribution;
            sheet.Cells[row, 11].Value = p.PayeTax;
            sheet.Cells[row, 12].Value = p.TotalDeductions - p.SsnitEmployeeContribution - p.PayeTax;
            sheet.Cells[row, 13].Value = p.NetPay;
            row++;
        }

        return new FileExportResult
        {
            FileBytes = package.GetAsByteArray(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            FileName = $"PayrollRegister_{timestamp}.xlsx"
        };
    }
}
