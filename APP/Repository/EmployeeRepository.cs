using System.Data.Entity;
using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Employees;
using INFRASTRUCTURE.Context;
using SHARED;

namespace APP.Repository;

public class EmployeeRepository(ApplicationDbContext context, IMapper mapper) : IEmployeeRepository
{
    public async Task<Result<Guid?>> CreateEmployee(CreateEmployeeRequest request, Guid userId)
    {
        var employee = mapper.Map<Employee>(request);
        employee.CreatedById = userId;

        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        return employee.Id;
    }

    public async Task<Result<EmployeeDto>> GetEmployee(Guid id)
    {
        var employee = await context.Employees
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
        {
            return Error.NotFound("Employee.NotFound", "Employee not found");
        }

        var employeeDto = mapper.Map<EmployeeDto>(employee);
        return Result.Success(employeeDto);
    }
    
    public async Task<Result<Paginateable<IEnumerable<EmployeeDto>>>> GetEmployees(int page, int pageSize, string searchQuery)
    {
        var query = context.Employees.AsQueryable();
        
        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.Email);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query, 
            page, 
            pageSize, 
            mapper.Map<EmployeeDto>
        );
    }

    public async Task<Result> UpdateEmployee(Guid id, CreateEmployeeRequest request, Guid userId)
    {
        var employee = await context.Employees
            .Include(e => e.Siblings)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
        {
            return Error.NotFound("Employee.NotFound", "Employee not found");
        }

        mapper.Map(request, employee);
        employee.UpdatedAt = DateTime.Now;
        employee.LastUpdatedById = userId;
        context.Employees.Update(employee);
        await context.SaveChangesAsync();

        return Result.Success();
    }
}
