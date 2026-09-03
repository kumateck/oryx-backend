using APP.IRepository;
using AutoMapper;
using INFRASTRUCTURE.Context;

namespace APP.Repository;

public partial class PerformanceRepository(
    ApplicationDbContext context,
    IMapper mapper,
    IApprovalRepository approvalRepository
) : IPerformanceRepository;
