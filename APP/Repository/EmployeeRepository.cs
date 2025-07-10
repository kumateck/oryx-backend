using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using APP.Extensions;
using APP.IRepository;
using APP.Services.Email;
using APP.Utils;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using DOMAIN.Entities.Auth;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.LeaveRequests;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SHARED;


namespace APP.Repository;

public class EmployeeRepository(ApplicationDbContext context,
    ILogger<EmployeeRepository> logger, IEmailService emailService, IMapper mapper,
    IConfiguration configuration, UserManager<User> userManager) : IEmployeeRepository
{

    public async Task<Result> OnboardEmployees(OnboardEmployeeDto employeeDtos)
    {
        const int maxRetries = 3;

        const string templatePath = "wwwroot/email/RegistrationEmail.html";
        
        if (!File.Exists(templatePath))
            throw new FileNotFoundException("Email template not found", templatePath);

        var emailTemplate = await File.ReadAllTextAsync(templatePath);
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtKey = configuration["JwtSettings:Key"];
        var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
      
        foreach (var employee in employeeDtos.EmailList)
        {
            try
            {

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity([
                        new Claim("type", employee.EmployeeType.ToString())
                    ]),
                    Expires = DateTime.UtcNow.AddDays(7),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var jwt = tokenHandler.WriteToken(token);
                
                var partialUrl = Environment.GetEnvironmentVariable("CLIENT_BASE_URL");

                var verificationLink = employee.EmployeeType == EmployeeType.Casual
                    ? $"{partialUrl}/onboarding/0?token={jwt}"
                    : $"{partialUrl}/onboarding/1?token={jwt}";
                
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

    public async Task<Result<Guid>> CreateEmployee(CreateEmployeeRequest request)
    {
        var existingEmployee = await context.Employees
            .FirstOrDefaultAsync(e => e.Email == request.Email);

        if (existingEmployee != null)
        {
            logger.LogWarning("Employee already exists with email: {Email}", request.Email);
            return Error.Validation("Employee.Exists", "Employee already exists.");
        }

        var country = await context.Countries
            .FirstOrDefaultAsync(c => c.Id == Guid.Parse(request.Nationality));

        if (country != null)
        {
            request.Nationality = country.Name;
        }
        else
        {
            logger.LogWarning("Nationality ID {Id} not found in countries table.", request.Nationality);
            return Error.Validation("Nationality.Invalid", "Invalid Nationality.");
        }

        var employee = mapper.Map<Employee>(request);
        
        await context.Employees.AddAsync(employee);

        await context.SaveChangesAsync();

        return employee.Id;
    }

    public async Task<Result> CreateEmployeeUser(EmployeeUserDto employeeUserDto)
    {
        var employee = await context.Employees.Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == employeeUserDto.EmployeeId);

        if (employee == null)
            return Error.NotFound("Employee.NotFound", "Employee not found");

        var existingUser = await context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == employee.Email);

        if (existingUser is not null && !existingUser.DeletedAt.HasValue)
            return Error.Conflict("User.Exists", "User already exists");

        if (existingUser?.DeletedAt != null)
        {
            existingUser.DeletedAt = null;
            existingUser.LastDeletedById = null;
            context.Users.Update(existingUser);

            var roles = await userManager.GetRolesAsync(existingUser);
            await userManager.RemoveFromRolesAsync(existingUser, roles);

            var newRole = await context.Roles.FirstOrDefaultAsync(r => r.Id == employeeUserDto.RoleId && r.DeletedAt == null);
            if (newRole is null) return RoleErrors.NotFound(employeeUserDto.RoleId);

            if (string.IsNullOrEmpty(newRole.Name)) return RoleErrors.InvalidRoleName(newRole.Name);

            await userManager.AddToRoleAsync(existingUser, newRole.Name);
            await context.SaveChangesAsync();
            return Result.Success();
        }

        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var newUser = mapper.Map<User>(employee);
            newUser.Email = employee.Email;
            newUser.UserName = employee.Email;
            newUser.FirstName = employee.FirstName;
            newUser.LastName = employee.LastName;
            newUser.DepartmentId = employee.DepartmentId;

            await userManager.CreateAsync(newUser);
            await context.SaveChangesAsync();

            var role = await context.Roles.FirstOrDefaultAsync(r => r.Id == employeeUserDto.RoleId && r.DeletedAt == null);

            if (role == null)
            {
                return Error.NotFound("Role.NotFound", "Role not found");
            }
            
            await userManager.AddToRoleAsync(newUser, role.Name ?? "");
            await context.SaveChangesAsync();

            await transaction.CommitAsync();
            
            const string templatePath = "wwwroot/email/PasswordSetup.html";
            if (!File.Exists(templatePath))
                throw new FileNotFoundException("Email template not found", templatePath);

            var emailTemplate = await File.ReadAllTextAsync(templatePath);
            
            var key = Guid.NewGuid().ToString();
            
            var partialUrl = Environment.GetEnvironmentVariable("CLIENT_BASE_URL");
            
            var token = await userManager.GeneratePasswordResetTokenAsync(newUser);

            await context.PasswordResets.AddAsync(new PasswordReset
            {
                UserId = newUser.Id,
                Token = token,
                KeyName = key,
                CreatedAt = DateTime.Now
            });

            var verificationLink = $"{partialUrl}/reset-password?key={key}";

            var emailBody = emailTemplate
                .Replace("{Name}", $"{employee.FirstName} {employee.LastName}")
                .Replace("{Email}", employee.Email)
                .Replace("{VerificationLink}", verificationLink);

            const int maxRetries = 3;
            var attempts = 0;
            var sent = false;

            while (attempts < maxRetries && !sent)
            {
                try
                {
                    emailService.SendMail(newUser.Email, "Password Setup", emailBody, []);
                    logger.LogInformation($"Password setup email sent to {newUser.Email}");
                    sent = true;
                }
                catch (Exception ex)
                {
                    attempts++;
                    logger.LogWarning($"Failed attempt {attempts} for {newUser.Email}: {ex.Message}");

                    if (attempts == maxRetries)
                        logger.LogError($"Giving up on {newUser.Email} after {maxRetries} attempts.");
                }
            }

            return Result.Success(newUser.Id);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Failed to create user and assign role.");
            return Result.Failure(Error.Failure("User.CreationFailed", "An error occurred while creating the user."));
        }
    }

    public async Task<Result<IEnumerable<EmployeeDto>>> GetEmployeesByDepartment(Guid departmentId)
    {
        var employees = await context.Employees
            .Include(e => e.Department)
            .Include(e => e.Designation)
            .Where(e => e.DepartmentId == departmentId)
            .ToListAsync();

        var employeeDtos = employees.Select(e => mapper.Map<EmployeeDto>(e, 
            opts => { opts.Items[AppConstants.ModelType] = nameof(Employee); }));

        return Result.Success(employeeDtos); 
    }

    public async Task<Result<IEnumerable<MinimalEmployeeInfoDto>>> GetAvailableEmployeesByDepartment(Guid shiftScheduleId, DateTime date)
    {
        var isHoliday = await context.Holidays
            .AnyAsync(h => h.Date.Date == date.Date);

        if (isHoliday)
        {
            return Result.Success(Enumerable.Empty<MinimalEmployeeInfoDto>());
        }
        
        var shiftSchedule = await context.ShiftSchedules.FirstOrDefaultAsync(ss => ss.Id == shiftScheduleId);

        if (shiftSchedule == null)
        {
            return Error.NotFound("ShiftSchedule.NotFound", "ShiftSchedule not found");
        }

        // Get IDs of employees unavailable due to leave
        var leaveEmployeeIdsQuery = context.LeaveRequests
            .Where(l =>
                l.LeaveStatus == LeaveStatus.Approved &&
                l.StartDate.Date <= date.Date &&
                l.EndDate.Date >= date.Date)
            .Select(l => l.EmployeeId);

        // Get IDs of employees with assigned shift
        var scheduledEmployeeIdsQuery = context.ShiftAssignments
            .Where(s => s.ShiftSchedules.StartDate <= date.Date
                        && s.ShiftSchedules.EndDate >= date.Date)
            .Select(s => s.EmployeeId);

        // Combine both into one set
        var unavailableEmployeeIds = await leaveEmployeeIdsQuery
            .Union(scheduledEmployeeIdsQuery)
            .Distinct()
            .ToListAsync();

        // Fetch available employees directly from DB, filter and project to DTO
        var availableEmployees = await context.Employees
            .Where(e => !unavailableEmployeeIds.Contains(e.Id) && e.DepartmentId == shiftSchedule.DepartmentId)
            .ProjectTo<MinimalEmployeeInfoDto>(mapper.ConfigurationProvider)
            .ToListAsync();

        return Result.Success(availableEmployees.AsEnumerable());
    }

    public async Task<Result<EmployeeDto>> GetEmployee(Guid id)
    {
        var employee = await context.Employees
            .Include(e=> e.Department)
            .Include(e=> e.Designation)
            .FirstOrDefaultAsync(e => e.Id == id);
    
        return employee is null ?
            Error.NotFound("Employee.NotFound", "Employee not found") :
            mapper.Map<EmployeeDto>(employee, 
                opts => { opts.Items[AppConstants.ModelType] = nameof(Employee);});
    }
    
    public async Task<Result<Paginateable<IEnumerable<EmployeeDto>>>> GetEmployees(int page, int pageSize,
        string searchQuery, string designation, string department)
    {
        var query = context.Employees
            .Include(e => e.Department)
            .Include(e => e.Designation)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery,
                q => q.FirstName,
                q=> q.LastName, q => q.Email, 
                q => q.FirstName + " " +q.LastName,
                q => q.StaffNumber,
                q => q.PhoneNumber,
                q => q.GhanaCardNumber);
        }
        
        if (!string.IsNullOrWhiteSpace(designation))
        {
            query = query.WhereSearch(designation, q => q.Designation.Name);
        }

        if (!string.IsNullOrWhiteSpace(department))
        {
            query = query.WhereSearch(department, q => q.Department.Name);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query, 
            page, 
            pageSize, 
            mapper.Map<EmployeeDto>
        );
    }

    public async Task<Result> UpdateEmployee(Guid id, UpdateEmployeeRequest request)
    {
        var employee = await context.Employees
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
        {
            return Error.NotFound("Employee.NotFound", "Employee not found");
        }

        if (request.ActiveStatus is EmployeeActiveStatus.Suspension)
        {
            if (!request.SuspensionStartDate.HasValue || !request.SuspensionEndDate.HasValue)
            {
                return Error.Validation("Employee.Status", "Employee suspension requires start and end dates");
            }
        }

        mapper.Map(request, employee);

        context.Employees.Update(employee);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> AssignEmployee(Guid id, AssignEmployeeDto employeeDto)
    {
        var employee = await context.Employees
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
        {
            return Error.NotFound("Employee.NotFound", "Employee not found");
        }
        
        var designation = await context.Designations.FirstOrDefaultAsync(d => d.Id == employeeDto.DesignationId);

        if (designation == null)
        {
            return Error.NotFound("Designation.NotFound", "Designation not found");
        }
        
        var department = await context.Departments.FirstOrDefaultAsync(d => d.Id == employeeDto.DepartmentId);

        if (department == null)
        {
            return Error.NotFound("Department.NotFound", "Department not found");
        }
        
        mapper.Map(employeeDto, employee);
        employee.DepartmentId = employeeDto.DepartmentId;
        employee.DesignationId = employeeDto.DesignationId;
        employee.AnnualLeaveDays = designation.MaximumLeaveDays;

        context.Employees.Update(employee);
        await context.SaveChangesAsync();

        const string templatePath = "wwwroot/email/EmployeeAcceptance.html";
        Console.WriteLine(templatePath);
        if (!File.Exists(templatePath))
            throw new FileNotFoundException("Email template not found", templatePath);

        var emailTemplate = await File.ReadAllTextAsync(templatePath);

        var body = emailTemplate
            .Replace("{Name}", $"{employee.FirstName} {employee.LastName}")
            .Replace("{Email}", employee.Email)
            .Replace("{DesignationName}", designation.Name)
            .Replace("{DepartmentName}", department.Name);

        const int maxRetries = 3;
        var attempts = 0;
        var sent = false;

        while (attempts < maxRetries && !sent)
        {
            try
            {
                emailService.SendMail(employee.Email, "Welcome to the Company", body, []);
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

        return Result.Success();
    }

    public async Task<Result> DeleteEmployee(Guid id, Guid userId)
    {
        var employee = await context.Employees.FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
        {
            return Error.NotFound("Employee.NotFound", "Employee not found");
        }
        employee.DeletedAt = DateTime.UtcNow;
        employee.LastDeletedById = userId;
        
        context.Employees.Update(employee);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}
