using System.ComponentModel.DataAnnotations;
using APP.Services.Employees;
using DOMAIN.Entities.Employees;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SHARED;
using SHARED.Requests;

namespace APP.Repository;

/// <summary>
/// Invited onboarding. HR sends invitations; each carries a random single-use token whose hash is stored. The public form
/// reads and submits through that token, the email and employee type come from the invitation, and a submitted form can add
/// its photograph only shortly afterwards.
/// </summary>
public partial class EmployeeRepository
{
    private static readonly TimeSpan InvitationLifetime = TimeSpan.FromDays(7);
    private static readonly TimeSpan PhotoWindow = TimeSpan.FromHours(2);

    public async Task<Result<List<EmployeeInvitationResult>>> OnboardEmployees(OnboardEmployeeDto request, Guid actor)
    {
        var invites = (request?.EmailList ?? []).Where(i => !string.IsNullOrWhiteSpace(i?.Email)).ToList();
        if (invites.Count == 0) return Error.Validation("Onboarding.Empty", "Add at least one person to invite.");
        if (invites.Count > 200) return Error.Validation("Onboarding.TooMany", "Invite at most 200 people at a time.");
        const string templatePath = "wwwroot/email/RegistrationEmail.html";
        if (!File.Exists(templatePath)) return Error.Failure("Template.Missing", "The invitation email template is missing on the server.");
        var template = await File.ReadAllTextAsync(templatePath);
        var baseUrl = (configuration["Onboarding:BaseUrl"] ?? httpContextAccessor.HttpContext?.Request.Headers.Origin.ToString())?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl)) return Error.Validation("Onboarding.Address", "The server does not know the address of the onboarding form.");

        var results = new List<EmployeeInvitationResult>();
        var email = new EmailAddressAttribute();
        foreach (var invite in invites.GroupBy(i => i.Email.Trim().ToLowerInvariant()).Select(g => g.First()))
        {
            var address = invite.Email.Trim();
            if (!email.IsValid(address) || !Enum.IsDefined(invite.EmployeeType))
            {
                results.Add(new EmployeeInvitationResult { Email = address, Problem = "Not a valid email address or employee type." });
                continue;
            }
            var lower = address.ToLowerInvariant();
            if (await context.Employees.AnyAsync(e => e.Email != null && e.Email.ToLower() == lower))
            {
                results.Add(new EmployeeInvitationResult { Email = address, Problem = "Someone with this email is already an employee." });
                continue;
            }
            // A new invitation replaces any earlier one still waiting for this address.
            var now = DateTime.UtcNow;
            foreach (var earlier in await context.EmployeeInvitations.Where(i => i.Email.ToLower() == lower && i.UsedAt == null && i.RevokedAt == null).ToListAsync())
                earlier.RevokedAt = now;
            var token = OnboardingTokens.Create();
            context.EmployeeInvitations.Add(new EmployeeInvitation
            {
                Id = Guid.NewGuid(), Email = address, Name = invite.Name?.Trim(), EmployeeType = invite.EmployeeType,
                TokenHash = OnboardingTokens.Hash(token), ExpiresAt = now.Add(InvitationLifetime), CreatedById = actor
            });
            await context.SaveChangesAsync();
            var link = $"{baseUrl}/onboarding/{(int)invite.EmployeeType}?token={Uri.EscapeDataString(token)}";
            var body = template.Replace("{Email}", address).Replace("{VerificationLink}", link);
            var sent = false;
            for (var attempt = 1; attempt <= 3 && !sent; attempt++)
            {
                try { emailService.SendMail(invite.Name, address, "Welcome to the team", body, []); sent = true; }
                catch (Exception ex) { logger.LogWarning("Invitation attempt {Attempt} to {Email} failed: {Message}", attempt, address, ex.Message); }
            }
            results.Add(new EmployeeInvitationResult { Email = address, Sent = sent, Problem = sent ? null : "The invitation was saved but the email could not be sent." });
        }
        return results;
    }

    public async Task<Result<OnboardingInvitationDto>> GetOnboardingInvitation(string token)
    {
        var invitation = await OpenInvitation(token);
        if (invitation.IsFailure) return invitation.Error;
        var value = invitation.Value;
        return new OnboardingInvitationDto { Email = value.Email, Name = value.Name, EmployeeType = value.EmployeeType, ExpiresAt = value.ExpiresAt };
    }

    public async Task<Result<Guid>> CompleteOnboarding(CompleteOnboardingRequest request)
    {
        if (request?.Employee == null) return Error.Validation("Onboarding.Form", "The form is empty.");
        var invitation = await OpenInvitation(request.Token);
        if (invitation.IsFailure) return invitation.Error;
        var invite = invitation.Value;
        // The invitation decides who this is and on what terms; the form cannot change either.
        request.Employee.Email = invite.Email;
        request.Employee.Type = invite.EmployeeType;
        request.Employee.Avatar = null;
        // HR assigns the staff number; a new hire cannot choose it.
        request.Employee.StaffNumber = null;
        var created = await CreateEmployee(request.Employee);
        if (created.IsFailure) return created.Error;
        invite.UsedAt = DateTime.UtcNow;
        invite.EmployeeId = created.Value;
        await context.SaveChangesAsync();
        return created.Value;
    }

    public async Task<Result> UploadOnboardingAvatar(OnboardingAvatarRequest request)
    {
        var hash = OnboardingTokens.Hash(request?.Token);
        var invite = await context.EmployeeInvitations.AsNoTracking().FirstOrDefaultAsync(i => i.TokenHash == hash);
        if (invite?.UsedAt == null || invite.EmployeeId == null || invite.UsedAt.Value.Add(PhotoWindow) < DateTime.UtcNow)
            return Error.Validation("Onboarding.Photo", "The photograph can only be added just after the form is submitted.");
        var employee = await context.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == invite.EmployeeId);
        if (employee == null) return Error.NotFound("Employee.NotFound", "Employee Not Found");
        if (!string.IsNullOrEmpty(employee.Avatar)) return Error.Validation("Onboarding.Photo", "A photograph has already been added.");
        return await UploadAvatar(new UploadFileRequest { File = request.File }, employee.Id);
    }

    public async Task<Result<EmployeeDto>> GetEmployeeForUser(Guid userId)
    {
        var id = await context.Employees.Where(e => e.UserId == userId).Select(e => (Guid?)e.Id).FirstOrDefaultAsync();
        return id == null ? Error.NotFound("Employee.NotLinked", "Your account is not linked to an employee record.") : await GetEmployee(id.Value);
    }

    private async Task<Result<EmployeeInvitation>> OpenInvitation(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return Error.Validation("Onboarding.Token", "This invitation link is incomplete.");
        var hash = OnboardingTokens.Hash(token);
        var invite = await context.EmployeeInvitations.FirstOrDefaultAsync(i => i.TokenHash == hash);
        if (invite == null || invite.RevokedAt != null) return Error.NotFound("Onboarding.Invalid", "This invitation link is not valid. Ask HR for a new one.");
        if (invite.UsedAt != null) return Error.Validation("Onboarding.Used", "This invitation has already been used.");
        if (invite.ExpiresAt < DateTime.UtcNow) return Error.Validation("Onboarding.Expired", "This invitation has expired. Ask HR for a new one.");
        return invite;
    }
}
