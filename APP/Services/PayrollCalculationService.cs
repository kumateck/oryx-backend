using DOMAIN.Entities.Payroll;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Services;

/// <summary>
/// Computes a single employee's Payslip for a payroll period against the effective-dated
/// PAYE/SSNIT rate tables. PAYE bands are annual, so taxable pay is annualised before the
/// bands are applied and the resulting tax is divided back down to the period. GRA tax
/// reliefs are entered by HR as annual amounts (see EmployeeTaxRelief) rather than derived,
/// since several reliefs are conditional (age, dependents, disability) in ways this system
/// does not verify. The GRA rule taxing bonus up to 15% of basic salary at a flat 5% is not
/// modelled - bonus/commission additions are taxed at the employee's normal marginal rate.
/// </summary>
public class PayrollCalculationService(ApplicationDbContext context) : IPayrollCalculationService
{
    public async Task<Payslip> CalculatePayslip(Guid employeeId, DateTime periodStart, DateTime periodEnd)
    {
        var compensation = await context.EmployeeCompensations
            .Include(c => c.Allowances)
            .Where(c => c.EmployeeId == employeeId
                        && c.EffectiveFrom <= periodEnd
                        && (c.EffectiveTo == null || c.EffectiveTo > periodStart))
            .OrderByDescending(c => c.EffectiveFrom)
            .FirstOrDefaultAsync();

        if (compensation is null)
        {
            throw new InvalidOperationException($"Employee {employeeId} has no compensation record covering this period.");
        }

        var ssnitRate = await context.SsnitRates
            .Where(r => r.EffectiveFrom <= periodEnd && (r.EffectiveTo == null || r.EffectiveTo > periodStart))
            .OrderByDescending(r => r.EffectiveFrom)
            .FirstOrDefaultAsync();

        if (ssnitRate is null)
        {
            throw new InvalidOperationException("No SSNIT rate is configured for this period.");
        }

        var taxBands = await context.PayeTaxBands
            .Where(b => b.EffectiveFrom <= periodEnd && (b.EffectiveTo == null || b.EffectiveTo > periodStart))
            .OrderBy(b => b.LowerBound)
            .ToListAsync();

        if (taxBands.Count == 0)
        {
            throw new InvalidOperationException("No PAYE tax bands are configured for this period.");
        }

        var deductions = await context.PayrollDeductions
            .Where(d => d.EmployeeId == employeeId
                        && d.EffectiveFrom <= periodEnd
                        && (d.EffectiveTo == null || d.EffectiveTo > periodStart))
            .ToListAsync();

        var additions = await context.PayrollAdditions
            .Where(a => a.EmployeeId == employeeId
                        && a.EffectiveFrom <= periodEnd
                        && (a.EffectiveTo == null || a.EffectiveTo > periodStart))
            .ToListAsync();

        var reliefs = await context.EmployeeTaxReliefs
            .Where(r => r.EmployeeId == employeeId
                        && r.EffectiveFrom <= periodEnd
                        && (r.EffectiveTo == null || r.EffectiveTo > periodStart))
            .ToListAsync();

        var totalAllowances = compensation.Allowances.Sum(a => a.Amount);
        var taxableAllowances = compensation.Allowances.Where(a => a.IsTaxable).Sum(a => a.Amount);
        var totalAdditions = additions.Sum(a => a.Amount);
        var taxableAdditions = additions.Where(a => a.IsTaxable).Sum(a => a.Amount);
        var grossPay = compensation.BasicSalary + totalAllowances + totalAdditions;

        var ssnitableEarnings = compensation.BasicSalary + taxableAllowances + taxableAdditions;
        var ssnitBase = Math.Min(ssnitableEarnings, ssnitRate.InsurableEarningsCeiling);
        var ssnitEmployeeContribution = Math.Round(ssnitBase * ssnitRate.EmployeeRate / 100m, 2);
        var ssnitEmployerContribution = Math.Round(ssnitBase * ssnitRate.EmployerRate / 100m, 2);
        var tier2Contribution = Math.Round(ssnitBase * ssnitRate.Tier2Rate / 100m, 2);

        var monthlyReliefs = Math.Round(reliefs.Sum(r => r.AnnualAmount) / 12, 2);
        var monthlyTaxablePay = ssnitableEarnings - ssnitEmployeeContribution - monthlyReliefs;
        var monthlyTaxablePayFloor = Math.Max(monthlyTaxablePay, 0);
        var annualTaxablePay = monthlyTaxablePayFloor * 12;
        var annualTax = CalculateGraduatedTax(annualTaxablePay, taxBands);
        var payeTax = Math.Round(annualTax / 12, 2);

        var otherDeductionsTotal = deductions.Sum(d => d.Amount);
        var totalDeductions = ssnitEmployeeContribution + payeTax + otherDeductionsTotal;
        var netPay = grossPay - totalDeductions;

        var lineItems = new List<PayslipLineItem>();
        lineItems.AddRange(compensation.Allowances.Select(a => new PayslipLineItem
        {
            Description = $"{a.Type} Allowance" + (string.IsNullOrWhiteSpace(a.Description) ? "" : $" ({a.Description})"),
            Type = PayslipLineItemType.Allowance,
            Amount = a.Amount
        }));
        lineItems.AddRange(additions.Select(a => new PayslipLineItem
        {
            Description = string.IsNullOrWhiteSpace(a.Description) ? a.Type.ToString() : a.Description,
            Type = PayslipLineItemType.Addition,
            Amount = a.Amount
        }));
        lineItems.Add(new PayslipLineItem
        {
            Description = $"SSNIT ({ssnitRate.EmployeeRate}%)",
            Type = PayslipLineItemType.StatutoryDeduction,
            Amount = ssnitEmployeeContribution
        });
        if (monthlyReliefs > 0)
        {
            lineItems.Add(new PayslipLineItem
            {
                Description = "Tax Reliefs (" + string.Join(", ", reliefs.Select(r => r.Type.ToString())) + ")",
                Type = PayslipLineItemType.TaxRelief,
                Amount = monthlyReliefs
            });
        }
        lineItems.Add(new PayslipLineItem
        {
            Description = "PAYE Income Tax",
            Type = PayslipLineItemType.Tax,
            Amount = payeTax
        });
        lineItems.AddRange(deductions.Select(d => new PayslipLineItem
        {
            Description = string.IsNullOrWhiteSpace(d.Description) ? d.Type.ToString() : d.Description,
            Type = PayslipLineItemType.OtherDeduction,
            Amount = d.Amount
        }));

        return new Payslip
        {
            EmployeeId = employeeId,
            BasicSalary = compensation.BasicSalary,
            TotalAllowances = totalAllowances,
            TotalAdditions = totalAdditions,
            GrossPay = grossPay,
            TotalReliefs = monthlyReliefs,
            TaxableIncome = monthlyTaxablePayFloor,
            SsnitEmployeeContribution = ssnitEmployeeContribution,
            SsnitEmployerContribution = ssnitEmployerContribution,
            Tier2Contribution = tier2Contribution,
            PayeTax = payeTax,
            TotalDeductions = totalDeductions,
            NetPay = netPay,
            LineItems = lineItems
        };
    }

    private static decimal CalculateGraduatedTax(decimal annualTaxablePay, List<PayeTaxBand> bands)
    {
        var remaining = annualTaxablePay;
        decimal tax = 0;

        foreach (var band in bands.OrderBy(b => b.LowerBound))
        {
            if (remaining <= 0) break;

            var bandWidth = band.UpperBound.HasValue
                ? band.UpperBound.Value - band.LowerBound
                : remaining;

            var amountInBand = Math.Min(remaining, bandWidth);
            tax += amountInBand * band.Rate / 100m;
            remaining -= amountInBand;
        }

        return Math.Max(tax, 0);
    }
}
