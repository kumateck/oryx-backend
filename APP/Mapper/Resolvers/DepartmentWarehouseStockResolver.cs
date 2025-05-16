/*using APP.IRepository;
using AutoMapper;
using DOMAIN.Entities.Materials;
using INFRASTRUCTURE.Context;

namespace APP.Mapper.Resolvers;

public class DepartmentWarehouseStockResolver(IMaterialRepository repo, ApplicationDbContext dbContext) : IValueResolver<MaterialDepartment, MaterialDepartmentWithWarehouseStockDto, decimal>
{
    public decimal Resolve(MaterialDepartment source, MaterialDepartmentWithWarehouseStockDto destination, decimal destMember,
        ResolutionContext context)
    {
        throw new NotImplementedException();
    }
}*/