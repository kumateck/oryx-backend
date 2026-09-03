using APP.IRepository;
using APP.Services;
using AutoMapper;
using INFRASTRUCTURE.Context;

namespace APP.Repository;

public partial class PayrollRepository(
    ApplicationDbContext context,
    IMapper mapper,
    IApprovalRepository approvalRepository,
    IPayrollCalculationService calculationService
) : IPayrollRepository;
