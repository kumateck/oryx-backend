using APP.Repository;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SHARED.Services.Identity;

namespace APP.Tests.Repository;

internal static class SupplierRelationshipTestContext
{
    public static ApplicationDbContext Create()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new ApplicationDbContext(options, new SupplierRelationshipCurrentUser());
    }

    public static SupplierRelationshipRepository CreateRepository(ApplicationDbContext context)
        => new(context, NullLogger<SupplierRelationshipRepository>.Instance,
            new ApprovalRepository(context, null, null, null,
                NullLogger<ApprovalRepository>.Instance, null, null));

    private sealed class SupplierRelationshipCurrentUser : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }
}
