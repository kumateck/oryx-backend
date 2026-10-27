using APP.Mapper;
using APP.Repository;
using APP.Services.Employees;
using AutoMapper;
using DOMAIN.Entities.Countries;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.Performance;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public class HrOwnershipAndOnboardingTests
{
    private static readonly Guid Actor = Guid.NewGuid();
    private static ApplicationDbContext Context() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new CurrentUser());
    private static IMapper Mapper() => new MapperConfiguration(c => c.AddProfile<OryxMapper>()).CreateMapper();
    private static EmployeeRepository Employees(ApplicationDbContext c) =>
        new(c, Microsoft.Extensions.Logging.Abstractions.NullLogger<EmployeeRepository>.Instance, null!, Mapper(), null!, null!, null!, null!, null!);

    private static async Task<(string token, Country country)> Invite(ApplicationDbContext c, EmployeeType type = EmployeeType.Permanent, DateTime? expires = null)
    {
        var country = new Country { Id = Guid.NewGuid(), Name = "Ghana", Nationality = "Ghanaian" };
        var token = OnboardingTokens.Create();
        c.Add(country);
        c.EmployeeInvitations.Add(new EmployeeInvitation
        {
            Id = Guid.NewGuid(), Email = "kofi@example.com", Name = "Kofi", EmployeeType = type, TokenHash = OnboardingTokens.Hash(token),
            ExpiresAt = expires ?? DateTime.UtcNow.AddDays(7)
        });
        await c.SaveChangesAsync();
        return (token, country);
    }

    private static CreateEmployeeRequest Form(Country country) => new()
    {
        FirstName = "Kofi", LastName = "Mensah", Email = "attacker@example.com", Type = EmployeeType.Casual, Nationality = country.Id.ToString(), PhoneNumber = "0240000000", StaffNumber = "ST-CHOSEN"
    };

    [Fact]
    public async Task TheInvitationDecidesTheEmailAndTypeAndWorksOnce()
    {
        await using var c = Context(); var (token, country) = await Invite(c);
        var repository = Employees(c);
        Assert.Equal("kofi@example.com", (await repository.GetOnboardingInvitation(token)).Value.Email);
        var created = await repository.CompleteOnboarding(new CompleteOnboardingRequest { Token = token, Employee = Form(country) });
        Assert.True(created.IsSuccess, created.IsFailure ? created.Error.Description : null);
        var employee = await c.Employees.SingleAsync();
        Assert.Equal("kofi@example.com", employee.Email);
        Assert.Equal(EmployeeType.Permanent, employee.Type);
        Assert.Null(employee.StaffNumber);
        Assert.Equal("Onboarding.Used", (await repository.CompleteOnboarding(new CompleteOnboardingRequest { Token = token, Employee = Form(country) })).Error.Code);
        Assert.Equal("Onboarding.Used", (await repository.GetOnboardingInvitation(token)).Error.Code);
        Assert.Single(await c.Employees.ToListAsync());
    }

    [Fact]
    public async Task UnknownExpiredOrMissingTokensOpenNothing()
    {
        await using var c = Context(); var (token, country) = await Invite(c, expires: DateTime.UtcNow.AddMinutes(-1));
        var repository = Employees(c);
        Assert.Equal("Onboarding.Expired", (await repository.CompleteOnboarding(new CompleteOnboardingRequest { Token = token, Employee = Form(country) })).Error.Code);
        Assert.Equal("Onboarding.Invalid", (await repository.GetOnboardingInvitation(OnboardingTokens.Create())).Error.Code);
        Assert.Equal("Onboarding.Token", (await repository.GetOnboardingInvitation("")).Error.Code);
        Assert.Empty(await c.Employees.ToListAsync());
        // Only the hash is stored, never the token itself.
        Assert.DoesNotContain(await c.EmployeeInvitations.Select(i => i.TokenHash).ToListAsync(), h => h == token);
    }

    [Fact]
    public async Task APhotoCanOnlyBeAddedRightAfterSubmitting()
    {
        await using var c = Context(); var (token, _) = await Invite(c);
        var repository = Employees(c);
        Assert.Equal("Onboarding.Photo", (await repository.UploadOnboardingAvatar(new OnboardingAvatarRequest { Token = token, File = "data:image/png;base64,AA==" })).Error.Code);
        var invitation = await c.EmployeeInvitations.SingleAsync();
        invitation.UsedAt = DateTime.UtcNow.AddHours(-3); invitation.EmployeeId = Guid.NewGuid(); await c.SaveChangesAsync();
        Assert.Equal("Onboarding.Photo", (await repository.UploadOnboardingAvatar(new OnboardingAvatarRequest { Token = token, File = "data:image/png;base64,AA==" })).Error.Code);
    }

    [Fact]
    public void TemporaryPasswordsAreRandomAndStrong()
    {
        var first = OnboardingTokens.TemporaryPassword(); var second = OnboardingTokens.TemporaryPassword();
        Assert.NotEqual(first, second); Assert.NotEqual("Pass123$1", first); Assert.True(first.Length >= 30);
    }

    private static PerformanceRepository Performance(ApplicationDbContext c) => new(c, Mapper(), null!);

    private static async Task<(Employee me, Employee manager, PerformanceCycle cycle)> People(ApplicationDbContext c, PerformanceCycleStatus status = PerformanceCycleStatus.Active)
    {
        var me = new Employee { Id = Guid.NewGuid(), FirstName = "Ama", Email = "ama@example.com", UserId = Actor };
        var manager = new Employee { Id = Guid.NewGuid(), FirstName = "Kwame", Email = "kwame@example.com", UserId = Guid.NewGuid() };
        var cycle = new PerformanceCycle { Id = Guid.NewGuid(), Name = "2026", PeriodStart = new DateTime(2026, 1, 1), PeriodEnd = new DateTime(2026, 12, 31), Status = status };
        c.AddRange(me, manager, cycle); await c.SaveChangesAsync();
        return (me, manager, cycle);
    }

    [Fact]
    public async Task TheAccountLinkIdentifiesTheEmployee()
    {
        await using var c = Context(); var (me, _, _) = await People(c);
        Assert.Equal(me.Id, await Performance(c).EmployeeOf(Actor));
        Assert.Null(await Performance(c).EmployeeOf(Guid.NewGuid()));
    }

    [Fact]
    public async Task GoalsStayWithinTheCycleWeightAndTitleRules()
    {
        await using var c = Context(); var (me, _, cycle) = await People(c);
        var repository = Performance(c);
        CreateGoalRequest Goal(string title, int weight, DateTime? target = null) => new() { EmployeeId = me.Id, CycleId = cycle.Id, Title = title, Weight = weight, TargetDate = target ?? new DateTime(2026, 6, 30) };
        Assert.True((await repository.CreateGoal(Goal("Reduce deviations", 60), Actor)).IsSuccess);
        Assert.Equal("Goal.Weight", (await repository.CreateGoal(Goal("Train two analysts", 50), Actor)).Error.Code);
        Assert.Equal("Goal.Duplicate", (await repository.CreateGoal(Goal("reduce deviations", 10), Actor)).Error.Code);
        Assert.Equal("Goal.TargetDate", (await repository.CreateGoal(Goal("Audit readiness", 10, new DateTime(2027, 1, 5)), Actor)).Error.Code);
        cycle.Status = PerformanceCycleStatus.Closed; await c.SaveChangesAsync();
        Assert.Equal("Goal.CycleClosed", (await repository.CreateGoal(Goal("Audit readiness", 10), Actor)).Error.Code);
    }

    [Fact]
    public async Task OnlyTheEmployeeSelfAssessesAndOnlyTheirGoalsAreRated()
    {
        await using var c = Context(); var (me, manager, cycle) = await People(c);
        var mine = new Goal { Id = Guid.NewGuid(), EmployeeId = me.Id, CycleId = cycle.Id, Title = "Mine", Weight = 50, TargetDate = new DateTime(2026, 6, 1) };
        var theirs = new Goal { Id = Guid.NewGuid(), EmployeeId = manager.Id, CycleId = cycle.Id, Title = "Theirs", Weight = 50, TargetDate = new DateTime(2026, 6, 1) };
        var review = new PerformanceReview { Id = Guid.NewGuid(), EmployeeId = me.Id, CycleId = cycle.Id, ReviewerId = manager.Id, Status = PerformanceReviewStatus.SelfAssessment };
        c.AddRange(mine, theirs, review); await c.SaveChangesAsync();
        var repository = Performance(c);
        SubmitSelfAssessmentRequest Ratings(params Guid[] goals) => new() { Comments = "Done", GoalRatings = goals.Select(g => new GoalRatingRequest { GoalId = g, AchievementPercentage = 80 }).ToList() };
        Assert.Equal("PerformanceReview.NotYours", (await repository.SubmitSelfAssessment(review.Id, Ratings(mine.Id), manager.Id)).Error.Code);
        Assert.Equal("Review.Ratings", (await repository.SubmitSelfAssessment(review.Id, Ratings(theirs.Id), me.Id)).Error.Code);
        Assert.Equal("Review.Ratings", (await repository.SubmitSelfAssessment(review.Id, Ratings(mine.Id, mine.Id), me.Id)).Error.Code);
        Assert.True((await repository.SubmitSelfAssessment(review.Id, Ratings(mine.Id), me.Id)).IsSuccess);
        Assert.Equal(PerformanceReviewStatus.ManagerReview, (await c.PerformanceReviews.SingleAsync()).Status);
    }

    [Fact]
    public async Task OnlyTheAssignedReviewerSubmitsTheManagerReview()
    {
        await using var c = Context(); var (me, manager, cycle) = await People(c);
        var review = new PerformanceReview { Id = Guid.NewGuid(), EmployeeId = me.Id, CycleId = cycle.Id, ReviewerId = manager.Id, Status = PerformanceReviewStatus.ManagerReview };
        c.Add(review); await c.SaveChangesAsync();
        var request = new SubmitManagerReviewRequest { Comments = "Good", OverallRating = default };
        Assert.Equal("PerformanceReview.NotReviewer", (await Performance(c).SubmitManagerReview(review.Id, request, Actor, me.Id)).Error.Code);
    }

    [Fact]
    public async Task OnlyTheAssignedReviewerMayReadTheReviewedGoals()
    {
        await using var c = Context(); var (me, manager, cycle) = await People(c);
        c.Add(new PerformanceReview { Id = Guid.NewGuid(), EmployeeId = me.Id, CycleId = cycle.Id, ReviewerId = manager.Id, Status = PerformanceReviewStatus.ManagerReview });
        await c.SaveChangesAsync();
        var repository = Performance(c);
        Assert.True(await repository.Reviews(manager.Id, me.Id, cycle.Id));
        Assert.False(await repository.Reviews(me.Id, manager.Id, cycle.Id));
        Assert.False(await repository.Reviews(manager.Id, me.Id, Guid.NewGuid()));
    }

    private sealed class CurrentUser : ICurrentUserService
    {
        public Guid? UserId => Actor; public Guid? DepartmentId => null; public string DepartmentType => string.Empty;
    }
}
