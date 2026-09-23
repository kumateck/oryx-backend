using APP.IRepository;
using APP.Mapper;
using APP.Repository;
using APP.Repository.QcWorksheets;
using APP.Services.QcWorksheets;
using AutoMapper;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.QcWorksheets;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SHARED.Services.Identity;

namespace APP.Tests.QcWorksheets;

internal sealed class NoCurrentUser : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

/// <summary>
/// Resolves only what the QC approval path asks of it, so the real
/// <see cref="ApprovalRepository"/> can be exercised without standing up the whole app.
/// </summary>
internal sealed class StubServiceProvider(IQcReauthContext reauthContext) : IServiceProvider
{
    public object GetService(Type serviceType) =>
        serviceType == typeof(IQcReauthContext) ? reauthContext : null;
}

/// <summary>
/// Shared harness for the QC worksheet acceptance tests. Everything runs against the real
/// repositories, the real <see cref="ApprovalRepository"/> and the real
/// <see cref="QcSignatureService"/> — including genuine password hashing — so the tests
/// prove the production path rather than a parallel one.
/// </summary>
internal sealed class QcWorksheetTestContext : IDisposable
{
    internal const string CorrectPassword = "Correct-Horse-9!";

    internal ApplicationDbContext Db { get; }
    internal IMapper Mapper { get; }
    internal QcReauthContext Reauth { get; }
    internal UserManager<User> UserManager { get; }
    internal ApprovalRepository ApprovalRepository { get; }
    internal QcSignatureService SignatureService { get; }
    internal StandardTestProcedureRepository Stps { get; }
    internal WorksheetTemplateRepository Templates { get; }
    internal QcApprovalRepository Approvals { get; }

    internal User Approver { get; private set; }
    internal Role ApproverRole { get; private set; }

    internal QcWorksheetTestContext()
    {
        Db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            new NoCurrentUser());

        Mapper = CreateMapper();
        Reauth = new QcReauthContext();
        UserManager = CreateUserManager(Db);

        ApprovalRepository = new ApprovalRepository(
            Db,
            Mapper,
            UserManager,
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<ApprovalRepository>.Instance,
            new StubServiceProvider(Reauth),
            null!);

        SignatureService = new QcSignatureService(Db, UserManager, Reauth, ApprovalRepository);

        Stps = new StandardTestProcedureRepository(
            Db, Mapper, SignatureService, Reauth, ApprovalRepository, new StpDocxImportService());

        Templates = new WorksheetTemplateRepository(
            Db, Mapper, SignatureService, Reauth, ApprovalRepository);

        Approvals = new QcApprovalRepository(Db, Mapper);
    }

    /// <summary>Creates the approver and a single-stage approval chain for the given model type.</summary>
    internal async Task<Guid> SeedApprovalChain(string modelType)
    {
        Approver ??= await SeedUser();
        ApproverRole ??= await SeedRole();

        var approval = new Approval
        {
            Id = Guid.NewGuid(),
            ItemType = modelType,
            CreatedAt = DateTime.UtcNow,
            ApprovalStages = []
        };

        Db.Approvals.Add(approval);
        Db.ApprovalStages.Add(new ApprovalStage
        {
            Id = Guid.NewGuid(),
            ApprovalId = approval.Id,
            Order = 1,
            Required = true,
            UserId = Approver.Id
        });

        await Db.SaveChangesAsync();
        return approval.Id;
    }

    internal async Task<User> SeedUser()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "qc.approver",
            NormalizedUserName = "QC.APPROVER",
            Email = "qc.approver@example.test",
            FirstName = "Qc",
            LastName = "Approver",
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, CorrectPassword);

        Db.Users.Add(user);
        await Db.SaveChangesAsync();
        return user;
    }

    private async Task<Role> SeedRole()
    {
        var role = new Role { Id = Guid.NewGuid(), Name = "QC Manager", NormalizedName = "QC MANAGER" };
        Db.Roles.Add(role);
        await Db.SaveChangesAsync();
        return role;
    }

    private static IMapper CreateMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => { }, typeof(OryxMapper));
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    private static UserManager<User> CreateUserManager(ApplicationDbContext context) =>
        new(
            new UserStore<User, Role, ApplicationDbContext, Guid>(context),
            Options.Create(new IdentityOptions()),
            new PasswordHasher<User>(),
            [],
            [],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null,
            NullLogger<UserManager<User>>.Instance);

    public void Dispose()
    {
        UserManager.Dispose();
        Db.Dispose();
    }
}
