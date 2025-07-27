using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using APP.Extensions;
using APP.IRepository;
using APP.Services.Email;
using APP.Services.Storage;
using APP.Utils;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using DOMAIN.Entities.Auth;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.LeaveRequests;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SHARED;
using SHARED.Requests;


namespace APP.Repository;

public class EmployeeRepository(ApplicationDbContext context,
    ILogger<EmployeeRepository> logger, IEmailService emailService, IMapper mapper,
    IConfiguration configuration, UserManager<User> userManager, IBlobStorageService blobStorage) : IEmployeeRepository
{

    public async Task<Result> OnboardEmployees(OnboardEmployeeDto employeeDtos)
    {
        const int maxRetries = 3;

        const string templatePath = "wwwroot/email/RegistrationEmail.html";
        
        if (!File.Exists(templatePath))
            throw new FileNotFoundException("Email template not found", templatePath);

        var emailTemplate = await File.ReadAllTextAsync(templatePath);
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtKey = configuration["JwtSettings:Key"] ?? "";
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
    
    public async Task<Result> UploadAvatar(UploadFileRequest request, Guid employeeId)
    {
        var avatar = request.File.ConvertFromBase64();
        var employee = await context.Employees.FirstOrDefaultAsync(e => e.Id == employeeId);
        if (employee == null) return Error.NotFound("Employee.NotFound", "Employee Not Found");
        var reference = $"{employeeId}.{avatar.FileName.Split(".").Last()}";

        var result = await blobStorage.UploadBlobAsync("avatar", avatar, reference, employee.Avatar);
        if (result.IsSuccess)
        {
            employee.Avatar = reference;
            context.Employees.Update(employee);
            await context.SaveChangesAsync();
        }

        return result;
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
        var employee = await context.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == employeeUserDto.EmployeeId);

        if (employee is null)
            return Error.NotFound("Employee.NotFound", "Employee not found");

        var existingUser = await context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == employee.Email);

        var role = await context.Roles.FirstOrDefaultAsync(r => r.Id == employeeUserDto.RoleId);
        
        if (role is null) return Error.NotFound("Role.NotFound", "Role not found");

        if (existingUser is not null && !existingUser.DeletedAt.HasValue)
        {
            return Error.Conflict("User.Exists", "A user with this email already exists.");
        }

        // Restore soft-deleted user
        if (existingUser?.DeletedAt != null)
        {
            existingUser.DeletedAt = null;
            existingUser.LastDeletedById = null;
            context.Users.Update(existingUser);

            var existingRoles = await userManager.GetRolesAsync(existingUser);
            await userManager.RemoveFromRolesAsync(existingUser, existingRoles);
            await userManager.AddToRoleAsync(existingUser, role.DisplayName);
            await context.SaveChangesAsync();

            logger.LogInformation("Restored deleted user {UserId} and assigned role {Role}", existingUser.Id, role);
            return Result.Success(existingUser.Id);
        }

        // Create new user
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            logger.LogInformation("Creating new user for email: {Email}", employee.Email);

            var newUser = mapper.Map<User>(employee);
            newUser.Email = newUser.UserName = employee.Email;

            var createResult = await userManager.CreateAsync(newUser, password:"Pass123$1");
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                logger.LogError("Failed to create user {Email}: {Errors}", newUser.Email, errors);
                return Error.Failure("User.CreationFailed", errors);
            }

            await userManager.AddToRoleAsync(newUser, role.DisplayName);
            logger.LogInformation("Assigned role {Role} to user {UserId}", role, newUser.Id);

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            logger.LogInformation("User {UserId} successfully created and saved", newUser.Id);

            // Password setup email
            const string templatePath = "wwwroot/email/PasswordSetup.html";
            if (!File.Exists(templatePath))
            {
                logger.LogError("Password setup email template not found at: {Path}", templatePath);
                return Error.Failure("Template.Missing", "Email template not found.");
            }

            var emailTemplate = await File.ReadAllTextAsync(templatePath);
            var key = Guid.NewGuid().ToString();
            var partialUrl = Environment.GetEnvironmentVariable("ClientBaseUrl");
            var token = await userManager.GeneratePasswordResetTokenAsync(newUser);

            await context.PasswordResets.AddAsync(new PasswordReset
            {
                UserId = newUser.Id,
                Token = token,
                KeyName = key,
                CreatedAt = DateTime.UtcNow
            });
            
            await context.SaveChangesAsync();

            var verificationLink = $"{partialUrl}/reset-password?key={key}";
            var emailBody = emailTemplate
                .Replace("{Name}", $"{employee.FirstName} {employee.LastName}")
                .Replace("{Email}", employee.Email)
                .Replace("{VerificationLink}", verificationLink);

            // Retry logic for email
            const int maxRetries = 3;
            var sent = false;
            for (int attempt = 1; attempt <= maxRetries && !sent; attempt++)
            {
                try
                {
                    emailService.SendMail(newUser.Email, "Password Setup", emailBody, []);
                    sent = true;
                    logger.LogInformation("Password setup email sent to {Email}", newUser.Email);
                }
                catch (Exception ex)
                {
                    logger.LogWarning("Attempt {Attempt}: Failed to send email to {Email}: {Message}", attempt, newUser.Email, ex.Message);
                    if (attempt == maxRetries)
                    {
                        logger.LogError("Giving up after {MaxAttempts} attempts to send email to {Email}", maxRetries, newUser.Email);
                    }
                }
            }

            return Result.Success(newUser.Id);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Failed to create employee user");
            return Error.Failure("User.CreationFailed", "An unexpected error occurred while creating the user.");
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
        string searchQuery, string designation, string department, EmployeeStatus? activeStatus)
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

        if (activeStatus.HasValue)
        {
            query = query.Where(e => e.Status == activeStatus);
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
        employee.Status = EmployeeStatus.Active;

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

    public async Task<Result> ChangeEmployeeType(Guid id, EmployeeType employeeType)
    {
        var employee = await context.Employees.FirstOrDefaultAsync(e => e.Id == id);
        if (employee == null) return Error.NotFound("Employee.NotFound", "Employee not found");
        
        employee.Type = employeeType;
        context.Employees.Update(employee);
        await context.SaveChangesAsync();
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
