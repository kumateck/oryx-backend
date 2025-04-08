using System.Data.Entity;
using APP.Extensions;
using APP.IRepository;
using APP.Services.Email;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Employees;
using INFRASTRUCTURE.Context;
using Microsoft.Extensions.Logging;
using SHARED;
using SHARED.Requests;

namespace APP.Repository;

public class EmployeeRepository(ApplicationDbContext context,
    ILogger<EmployeeRepository> logger, IEmailService emailService, IMapper mapper) : IEmployeeRepository
{

    public async Task<Result> OnboardEmployee(OnboardEmployeeDto employeeDto)
    {
        try
        {
            var existingEmployee = context.Employees.FirstOrDefault(e => e.Email == employeeDto.Email);
            if (existingEmployee != null)
            {
                return Error.Validation("Employee.Email", "Employee already exists");
            }
        
            var pathToFile = Path.GetFullPath(
                Path.Combine("..", "APP", "Services", "Email", "Templates", "RegistrationEmail.html")
            );
            Console.WriteLine(pathToFile);
            if (!File.Exists(pathToFile))
            {
                throw new FileNotFoundException("File not found", pathToFile);
            }

            Console.WriteLine(employeeDto.EmployeeType);

            var emailTemplate = await File.ReadAllTextAsync(pathToFile);
        
            var body = emailTemplate.Replace("{Email}", employeeDto.Email);
        
            emailService.SendMail(employeeDto.Email, "Welcome to the team", body, []);
            logger.LogInformation($"Email sent to {employeeDto.Email}");
            return Result.Success();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error during onboarding");
            
        }

        return Result.Failure(Error.Validation("Employee.Email", "Error during onboarding"));

    }
    
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
