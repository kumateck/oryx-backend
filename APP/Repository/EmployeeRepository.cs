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

public async Task<Result> OnboardEmployees(OnboardEmployeeDto employeeDtos)
{
    const int maxRetries = 3;
    
    var templatePath = Path.GetFullPath(Path.Combine("..", "APP", "Services", "Email", "Templates", "RegistrationEmail.html"));
    if (!File.Exists(templatePath))
        throw new FileNotFoundException("Email template not found", templatePath);

    var emailTemplate = await File.ReadAllTextAsync(templatePath);

    foreach (var employee in employeeDtos.EmailList)
    {
        try
        {
            var existing = await context.Employees.AnyAsync(e => e.Email == employee.Email);
            if (existing)
            {
                logger.LogWarning($"Employee with email {employee.Email} already exists. Skipping.");
                continue;
            }
            
            var verificationLink = employee.EmployeeType == EmployeeType.Casual
                ? "http://164.90.142.68:3005/onboarding?etype=casual"
                : "http://164.90.142.68:3005/onboarding?etype=permanent";
            
            var emailBody = emailTemplate
                .Replace("{Email}", employee.Email)
                .Replace("{VerificationLink}", verificationLink);

            // Send email with retry
            var attempts = 0;
            var sent = false;

            while (attempts < maxRetries && !sent)
            {
                try
                {
                    emailService.SendMail(employee.Email, "Welcome to the team", emailBody, []);
                    logger.LogInformation($"Email sent to {employee.Email}");
                    sent = true;
                }
                catch (Exception ex)
                {
                    attempts++;
                    logger.LogWarning($"Failed attempt {attempts} for {employee.Email}: {ex.Message}");

                    if (attempts == maxRetries)
                        logger.LogError($"Giving up on {employee.Email} after {maxRetries} attempts.");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error onboarding {employee.Email}");
        }
    }

    return Result.Success("Bulk onboarding completed.");
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
